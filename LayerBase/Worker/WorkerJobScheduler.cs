using System.Collections.Concurrent;
using System.Diagnostics;
using LayerBase.Scope;

namespace LayerBase.Worker;

internal readonly struct WorkerJobOrigin
{
    public WorkerJobOrigin(ScopeEndpoint endpoint)
    {
        Endpoint = endpoint;
    }

    public ScopeEndpoint Endpoint { get; }

    public bool CanSubmit => Endpoint.Transport != null && Endpoint.Transport.AcceptsWorkerJobs;
}

internal sealed class WorkerJobScheduler : IDisposable
{
    private readonly WorkerJobSchedulerOptions _options;
    private readonly object _gate = new();
    private readonly Queue<IWorkerJobItem> _queue;
    private readonly ManualResetEventSlim _signal = new(false);
    private readonly Thread[] _threads;
    private readonly StateSlot[] _states;
    private readonly int[] _free;
    private int _freeCount;
    private bool _accepting = true;
    private bool _disposed;
    private static int s_workerItemPoolCapacity = 64;
    private static int s_workerItemPoolTotalCount;
    private static readonly ConcurrentBag<CancellationTokenSource> s_ctsPool = new();

    internal static void SetWorkerItemPoolCapacity(int capacity)
    {
        s_workerItemPoolCapacity = Math.Max(1, capacity);
    }

    internal static int WorkerItemPoolTotalCount =>
        Volatile.Read(ref s_workerItemPoolTotalCount);

    internal static int CtsPoolCount => s_ctsPool.Count;

    internal static CancellationTokenSource RentCtsFromPool()
    {
        if (!s_ctsPool.TryTake(out var cts))
            cts = new CancellationTokenSource();
        return cts;
    }

    internal static void ReturnCtsToPool(CancellationTokenSource cts)
    {
#if NET
        try
        {
            if (cts.TryReset())
            {
                s_ctsPool.Add(cts);
                return;
            }
        }
        catch (ObjectDisposedException)
        {
        }
#endif
        cts.Dispose();
    }

    public WorkerJobScheduler(WorkerJobSchedulerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        SetWorkerItemPoolCapacity(options.WorkerItemPoolCapacity);
        _queue = new Queue<IWorkerJobItem>(options.JobQueueCapacity);
        _threads = new Thread[options.WorkerCount];
        _states = new StateSlot[options.StateCapacity];
        _free = new int[options.StateCapacity];

        for (int i = 0; i < _free.Length; i++)
            _free[i] = _free.Length - 1 - i;

        _freeCount = _free.Length;

        for (int i = 0; i < _threads.Length; i++)
        {
            int workerIndex = i;
            _threads[i] = new Thread(() => WorkerLoop(workerIndex))
            {
                IsBackground = true,
                Name = $"LayerBase Worker {workerIndex}"
            };
            _threads[i].Start();
        }
    }

    public WorkerHandle Run<TJob, TInput, TEvent>(
        in TJob job,
        in TInput input,
        in WorkerJobOrigin origin,
        WorkerEventJobOptions options,
        CancellationToken cancellationToken)
        where TJob : struct, IWorkerEventJob<TInput, TEvent>
        where TInput : struct
        where TEvent : struct
    {
        CancellationTokenSource cts = cancellationToken == default
            ? RentCtsFromPool()
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        WorkerHandle handle;
        IWorkerJobItem item;

        lock (_gate)
        {
            handle = AllocateHandleLocked(cts);
            if (!handle.IsValid)
            {
                ReturnCtsToPool(cts);
                return WorkerHandle.Invalid;
            }

            if (!_accepting || _disposed)
            {
                MarkTerminalLocked(handle, WorkerState.Failed);
                ReturnCtsToPool(cts);
                return handle;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                MarkTerminalLocked(handle, WorkerState.Cancelled);
                ReturnCtsToPool(cts);
                return handle;
            }

            if (_queue.Count >= _options.JobQueueCapacity)
            {
                MarkTerminalLocked(handle, WorkerState.Failed);
                ReturnCtsToPool(cts);
                return handle;
            }

            item = WorkerJobItem<TJob, TInput, TEvent>.Rent(
                this,
                handle,
                in job,
                in input,
                in origin,
                options,
                cts);
            _queue.Enqueue(item);
            _signal.Set();
        }

        return handle;
    }

    public WorkerState GetState(WorkerHandle handle)
    {
        lock (_gate)
        {
            if (!IsKnownHandleLocked(handle))
                return WorkerState.Failed;

            return _states[handle.Index].State;
        }
    }

    public bool Cancel(WorkerHandle handle)
    {
        lock (_gate)
        {
            if (!IsKnownHandleLocked(handle))
                return false;

            ref StateSlot slot = ref _states[handle.Index];
            if (slot.State is WorkerState.Completed or WorkerState.Failed or WorkerState.Cancelled)
                return false;

            slot.Cancellation?.Cancel();
            var cts = DetachCancellationLocked(handle);
            MarkTerminalLocked(handle, WorkerState.Cancelled);
            if (cts != null)
                ReturnCtsToPool(cts);
            return true;
        }
    }

    public void BeginStop()
    {
        List<IWorkerJobItem> pending = new();
        lock (_gate)
        {
            if (!_accepting)
                return;

            _accepting = false;
            while (_queue.Count > 0)
                pending.Add(_queue.Dequeue());

            for (int i = 0; i < _states.Length; i++)
            {
                var cts = _states[i].Cancellation;
                if (cts != null && _states[i].State is WorkerState.Pending or WorkerState.Running)
                    cts.Cancel();
            }

            if (_queue.Count == 0)
                _signal.Reset();
        }

        foreach (var item in pending)
            item.CancelBeforeRun();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        BeginStop();

        lock (_gate)
        {
            _disposed = true;
            _signal.Set();
        }

        var deadline = Stopwatch.GetTimestamp() +
            _options.ShutdownTotalTimeoutMilliseconds * (Stopwatch.Frequency / 1000);
        for (int i = 0; i < _threads.Length; i++)
        {
            if (!_threads[i].IsAlive)
                continue;

            var remainingTicks = deadline - Stopwatch.GetTimestamp();
            if (remainingTicks <= 0)
            {
                _threads[i].IsBackground = true;
                System.Diagnostics.Debug.WriteLine(
                    $"[WorkerJobScheduler] Thread '{_threads[i].Name}' timed out, set to background.");
            }
            else
            {
                var remainingMs = (int)(remainingTicks * 1000 / Stopwatch.Frequency);
                if (!_threads[i].Join(Math.Max(1, remainingMs)))
                {
                    _threads[i].IsBackground = true;
                    System.Diagnostics.Debug.WriteLine(
                        $"[WorkerJobScheduler] Thread '{_threads[i].Name}' timed out, set to background.");
                }
            }
        }

        _signal.Dispose();

        for (int i = 0; i < _states.Length; i++)
        {
            var cts = _states[i].Cancellation;
            _states[i].Cancellation = null;
            cts?.Dispose();
        }
    }

    private WorkerHandle AllocateHandleLocked(CancellationTokenSource cts)
    {
        if (_freeCount <= 0)
            return WorkerHandle.Invalid;

        int index = _free[--_freeCount];
        int version = _states[index].Version + 1;
        if (version <= 0)
            version = 1;

        _states[index].Version = version;
        _states[index].State = WorkerState.Pending;
        _states[index].InUse = true;
        _states[index].Cancellation = cts;
        return new WorkerHandle(index, version);
    }

    private bool TryMarkRunning(WorkerHandle handle)
    {
        lock (_gate)
        {
            if (!IsKnownHandleLocked(handle))
                return false;

            ref StateSlot slot = ref _states[handle.Index];
            if (slot.State != WorkerState.Pending)
                return false;

            slot.State = WorkerState.Running;
            return true;
        }
    }

    private void MarkTerminal(WorkerHandle handle, WorkerState state)
    {
        lock (_gate)
        {
            MarkTerminalLocked(handle, state);
        }
    }

    private void MarkTerminalLocked(WorkerHandle handle, WorkerState state)
    {
        if (!IsKnownHandleLocked(handle))
            return;

        ref var slot = ref _states[handle.Index];
        if (!slot.InUse)
            return;

        slot.State = state;
        slot.InUse = false;
        slot.Cancellation = null;

        _free[_freeCount++] = handle.Index;
    }

    private CancellationTokenSource? DetachCancellationLocked(WorkerHandle handle)
    {
        if (!IsKnownHandleLocked(handle))
            return null;

        ref var slot = ref _states[handle.Index];
        var cts = slot.Cancellation;
        slot.Cancellation = null;
        return cts;
    }

    private bool IsKnownHandleLocked(WorkerHandle handle)
    {
        return handle.IsValid &&
               (uint)handle.Index < (uint)_states.Length &&
               _states[handle.Index].Version == handle.Version;
    }

    private void WorkerLoop(int workerIndex)
    {
        while (true)
        {
            IWorkerJobItem? item = null;

            lock (_gate)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    if (_queue.Count == 0)
                        _signal.Reset();
                }
                else if (_disposed)
                {
                    return;
                }
            }

            if (item != null)
            {
                item.Execute(workerIndex);
                continue;
            }

            _signal.Wait();
        }
    }

    private void PostFailure(
        in WorkerJobOrigin origin,
        WorkerHandle handle,
        WorkerJobFailureKind kind,
        WorkerJobExceptionInfo error)
    {
        var failedEvent = new WorkerEventJobFailedScopeEvent(handle, kind, error);
        origin.Endpoint.Transport.EnqueueEvent(
            WorkerScopeEventRouteIds.Failure,
            ScopeEventClass.Internal,
            in failedEvent);
    }

    private interface IWorkerJobItem
    {
        void Execute(int workerIndex);

        void CancelBeforeRun();
    }

    private sealed class WorkerJobItem<TJob, TInput, TEvent> : IWorkerJobItem
        where TJob : struct, IWorkerEventJob<TInput, TEvent>
        where TInput : struct
        where TEvent : struct
    {
        private static readonly ConcurrentQueue<WorkerJobItem<TJob, TInput, TEvent>> s_pool = new();
        private static int s_poolCount;

        private WorkerJobScheduler _scheduler = null!;
        private WorkerHandle _handle;
        private TJob _job;
        private TInput _input;
        private WorkerJobOrigin _origin;
        private WorkerEventJobOptions _options;
        private CancellationTokenSource _cancellation = null!;

        public static WorkerJobItem<TJob, TInput, TEvent> Rent(
            WorkerJobScheduler scheduler,
            WorkerHandle handle,
            in TJob job,
            in TInput input,
            in WorkerJobOrigin origin,
            WorkerEventJobOptions options,
            CancellationTokenSource cancellation)
        {
            if (s_pool.TryDequeue(out var item))
                Interlocked.Decrement(ref s_poolCount);
            else
                item = new WorkerJobItem<TJob, TInput, TEvent>();

            item._scheduler = scheduler;
            item._handle = handle;
            item._job = job;
            item._input = input;
            item._origin = origin;
            item._options = options;
            item._cancellation = cancellation;
            return item;
        }

        private void ResetFields()
        {
            _scheduler = null!;
            _handle = default;
            _job = default;
            _input = default;
            _origin = default;
            _options = default;
            _cancellation = null!;
        }

        private void Return()
        {
            ResetFields();
            if (Interlocked.Increment(ref s_poolCount) <= s_workerItemPoolCapacity)
            {
                s_pool.Enqueue(this);
            }
            else
            {
                Interlocked.Decrement(ref s_poolCount);
            }
        }

        public void Execute(int workerIndex)
        {
            if (!_scheduler.TryMarkRunning(_handle))
            {
                ReturnCtsToPool(_cancellation);
                Return();
                return;
            }

            if (_cancellation.IsCancellationRequested)
            {
                _scheduler.PostFailure(
                    in _origin,
                    _handle,
                    WorkerJobFailureKind.Cancelled,
                    WorkerJobExceptionInfo.None);
                _scheduler.MarkTerminal(_handle, WorkerState.Cancelled);
                ReturnCtsToPool(_cancellation);
                Return();
                return;
            }

            try
            {
                var context = new WorkerJobContext(workerIndex, _cancellation.Token);
                TEvent result = _job.Execute(in _input, in context);

                if (_cancellation.IsCancellationRequested)
                {
                    _scheduler.PostFailure(
                        in _origin,
                        _handle,
                        WorkerJobFailureKind.Cancelled,
                        WorkerJobExceptionInfo.None);
                    _scheduler.MarkTerminal(_handle, WorkerState.Cancelled);
                    return;
                }

                var resultEvent = new WorkerEventJobResultScopeEvent(
                    _handle,
                    new WorkerEventJobResult<TEvent>(result));
                var postResult = _origin.Endpoint.Transport.EnqueueEvent(
                    WorkerScopeEventRouteIds.Result,
                    ScopeEventClass.Internal,
                    in resultEvent);

                if (postResult.IsAccepted)
                {
                    _scheduler.MarkTerminal(_handle, WorkerState.Completed);
                    return;
                }

                _scheduler.PostFailure(
                    in _origin,
                    _handle,
                    postResult.Status == ScopePostStatus.RuntimeDisposed
                        ? WorkerJobFailureKind.OriginScopeStopped
                        : WorkerJobFailureKind.ResultScopeEventRejected,
                    WorkerJobExceptionInfo.None);
                _scheduler.MarkTerminal(_handle, WorkerState.Failed);
            }
            catch (Exception ex)
            {
                _scheduler.PostFailure(
                    in _origin,
                    _handle,
                    WorkerJobFailureKind.ExecutionFault,
                    WorkerJobExceptionInfo.FromException(ex));
                _scheduler.MarkTerminal(_handle, WorkerState.Failed);
            }
            finally
            {
                ReturnCtsToPool(_cancellation);
                Return();
            }
        }

        public void CancelBeforeRun()
        {
            _cancellation.Cancel();
            _scheduler.PostFailure(
                in _origin,
                _handle,
                WorkerJobFailureKind.Cancelled,
                WorkerJobExceptionInfo.None);
            _scheduler.MarkTerminal(_handle, WorkerState.Cancelled);
            ReturnCtsToPool(_cancellation);
            Return();
        }
    }

    private struct StateSlot
    {
        public int Version;
        public WorkerState State;
        public bool InUse;
        public CancellationTokenSource? Cancellation;
    }
}
