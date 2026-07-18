using System.Diagnostics;
using LayerBase.Core.Event;
using LayerBase.Scope;

namespace LayerBase.Worker;

internal sealed class WorkerJobCoordinator : IDisposable
{
    private readonly ScopeRuntime _owner;
    private readonly WorkerJobScheduler _executor;
    private readonly WorkerJobSchedulerOptions _options;

    private readonly StateSlot[] _states;
    private readonly int[] _free;
    private readonly Stack<CancellationTokenSource> _ctsPool;

    private int _freeCount;
    private int _activeCount;
    private int _runningCount;
    private int _staleCompletionCount;

    private bool _accepting = true;
    private bool _disposed;

    internal WorkerJobCoordinator(
        ScopeRuntime owner,
        WorkerJobScheduler executor,
        WorkerJobSchedulerOptions options)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _states = new StateSlot[options.StateCapacity];
        _free = new int[options.StateCapacity];
        _ctsPool = new Stack<CancellationTokenSource>(options.WorkerItemPoolCapacity);

        for (int i = 0; i < _free.Length; i++)
            _free[i] = _free.Length - 1 - i;

        _freeCount = _free.Length;
    }

    internal int ActiveCount =>
        Volatile.Read(ref _activeCount);

    internal int RunningCount =>
        Volatile.Read(ref _runningCount);

    internal int StaleCompletionCount =>
        Volatile.Read(ref _staleCompletionCount);

    internal bool CanDispose =>
        ActiveCount == 0;

    internal WorkerHandle Run<TJob, TInput, TEvent>(
        in TJob job,
        in TInput input,
        WorkerEventJobOptions options,
        CancellationToken cancellationToken)
        where TJob : struct, IWorkerEventJob<TInput, TEvent>
        where TInput : struct
        where TEvent : struct
    {
        RequireOwnerThreadDebug();

        if (_disposed || !_accepting)
            return WorkerHandle.Invalid;

        if (cancellationToken.IsCancellationRequested)
            return WorkerHandle.Invalid;

        WorkerHandle handle = AllocateHandle();
        if (!handle.IsValid)
            return WorkerHandle.Invalid;

        ref StateSlot slot = ref _states[handle.Index];

        CancellationTokenSource cts = RentCancellationSource();
        slot.Cancellation = cts;

        if (cancellationToken.CanBeCanceled)
        {
            var callbackState = new CancellationCallbackState(
                _owner.Endpoint,
                handle);

            slot.ExternalCancellationRegistration =
                cancellationToken.Register(
                    static state =>
                    {
                        var callback = (CancellationCallbackState)state!;

                        var request = new WorkerCancelRequestedScopeEvent(
                            callback.Handle);

                        _ = callback.Endpoint.Transport.EnqueueEvent(
                            WorkerScopeEventRouteIds.CancelRequested,
                            ScopeEventClass.Critical,
                            in request);
                    },
                    callbackState);
        }

        var item = WorkerExecutionItem<TJob, TInput, TEvent>.Rent(
            _owner.Endpoint,
            handle,
            in job,
            in input,
            options,
            cts.Token);

        if (_executor.TryEnqueue(item))
            return handle;

        item.ReturnWithoutExecution();

        CompleteSlot(handle, WorkerState.Failed);

        return handle;
    }

    internal WorkerState GetState(WorkerHandle handle)
    {
        if (!handle.IsValid ||
            (uint)handle.Index >= (uint)_states.Length)
        {
            return WorkerState.Failed;
        }

        ref StateSlot slot = ref _states[handle.Index];

        if (Volatile.Read(ref slot.Version) != handle.Version)
            return WorkerState.Failed;

        return (WorkerState)Volatile.Read(ref slot.PublicState);
    }

    internal void HandleCancelRequested(WorkerHandle handle)
    {
        RequireOwnerThreadDebug();

        if (!TryGetActiveSlot(handle, out int index))
            return;

        ref StateSlot slot = ref _states[index];

        if (slot.CancelRequested)
            return;

        slot.CancelRequested = true;

        CancellationTokenSource? cts = slot.Cancellation;
        if (cts == null)
            return;

        try
        {
            cts.Cancel(throwOnFirstException: false);
        }
        catch (AggregateException ex)
        {
            _owner.ReportFault(ex, ScopeFaultPhase.WorkerLoop);
        }
    }

    internal void HandleExecutionCompleted(
        in WorkerExecutionCompletedScopeEvent completion,
        PostScheduler? scheduler)
    {
        RequireOwnerThreadDebug();

        WorkerHandle handle = completion.Handle;

        if (!TryGetActiveSlot(handle, out int index))
        {
            Interlocked.Increment(ref _staleCompletionCount);
            return;
        }

        ref StateSlot slot = ref _states[index];

        if (slot.ExecutionStarted)
            Interlocked.Decrement(ref _runningCount);

        bool cancelled =
            slot.CancelRequested ||
            completion.Kind == WorkerExecutionCompletionKind.Cancelled;

        if (cancelled)
        {
            PostFailure(
                scheduler,
                handle,
                WorkerJobFailureKind.Cancelled,
                WorkerJobExceptionInfo.None);

            CompleteSlot(handle, WorkerState.Cancelled);
            return;
        }

        if (completion.Kind == WorkerExecutionCompletionKind.Faulted)
        {
            PostFailure(
                scheduler,
                handle,
                WorkerJobFailureKind.ExecutionFault,
                completion.Error);

            CompleteSlot(handle, WorkerState.Failed);
            return;
        }

        if (scheduler == null || completion.Result == null)
        {
            PostFailure(
                scheduler,
                handle,
                WorkerJobFailureKind.OriginScopeStopped,
                WorkerJobExceptionInfo.None);

            CompleteSlot(handle, WorkerState.Failed);
            return;
        }

        PostResult postResult = completion.Result.PostTo(
            scheduler,
            completion.Options.ResultPostPolicy);

        if (postResult.IsSuccess)
        {
            CompleteSlot(handle, WorkerState.Completed);
            return;
        }

        PostFailure(
            scheduler,
            handle,
            WorkerJobFailureKind.ResultScopeEventRejected,
            WorkerJobExceptionInfo.None);

        CompleteSlot(handle, WorkerState.Failed);
    }

    internal void MarkExecutionStarted(WorkerHandle handle)
    {
        RequireOwnerThreadDebug();

        if (!TryGetActiveSlot(handle, out int index))
            return;

        ref StateSlot slot = ref _states[index];

        if (slot.ExecutionStarted)
            return;

        slot.ExecutionStarted = true;
        Volatile.Write(ref slot.PublicState, (int)WorkerState.Running);

        Interlocked.Increment(ref _runningCount);
    }

    internal void BeginStopOnOwnerThread()
    {
        RequireOwnerThreadDebug();

        if (!_accepting)
            return;

        _accepting = false;

        for (int i = 0; i < _states.Length; i++)
        {
            ref StateSlot slot = ref _states[i];

            if (!slot.InUse)
                continue;

            HandleCancelRequested(new WorkerHandle(i, slot.Version));
        }
    }

    internal void DisposeOnOwnerThread()
    {
        RequireOwnerThreadDebug();

        if (_disposed)
            return;

        if (_activeCount != 0)
        {
            throw new InvalidOperationException(
                "WorkerJobCoordinator cannot be disposed while jobs are physically active.");
        }

        _disposed = true;
        _accepting = false;

        while (_ctsPool.Count > 0)
            _ctsPool.Pop().Dispose();
    }

    public void Dispose()
    {
        DisposeOnOwnerThread();
    }

    private WorkerHandle AllocateHandle()
    {
        if (_freeCount == 0)
            return WorkerHandle.Invalid;

        int index = _free[--_freeCount];

        ref StateSlot slot = ref _states[index];

        int version = slot.Version + 1;
        if (version <= 0)
            version = 1;

        slot.Version = version;
        slot.PublicState = (int)WorkerState.Pending;
        slot.InUse = true;
        slot.CancelRequested = false;
        slot.ExecutionStarted = false;
        slot.Cancellation = null;
        slot.ExternalCancellationRegistration = default;

        Interlocked.Increment(ref _activeCount);

        return new WorkerHandle(index, version);
    }

    private bool TryGetActiveSlot(WorkerHandle handle, out int index)
    {
        index = handle.Index;

        if (!handle.IsValid ||
            (uint)index >= (uint)_states.Length)
        {
            return false;
        }

        ref StateSlot slot = ref _states[index];

        return slot.InUse &&
               slot.Version == handle.Version;
    }

    private void CompleteSlot(WorkerHandle handle, WorkerState terminalState)
    {
        if (!TryGetActiveSlot(handle, out int index))
            return;

        ref StateSlot slot = ref _states[index];

        slot.ExternalCancellationRegistration.Dispose();
        slot.ExternalCancellationRegistration = default;

        CancellationTokenSource? cts = slot.Cancellation;
        slot.Cancellation = null;

        if (cts != null)
            ReturnCancellationSource(cts);

        slot.CancelRequested = false;
        slot.ExecutionStarted = false;
        slot.InUse = false;

        Volatile.Write(ref slot.PublicState, (int)terminalState);

        _free[_freeCount++] = index;

        Interlocked.Decrement(ref _activeCount);
    }

    private CancellationTokenSource RentCancellationSource()
    {
        if (_ctsPool.Count == 0)
            return new CancellationTokenSource();

        return _ctsPool.Pop();
    }

    private void ReturnCancellationSource(CancellationTokenSource cts)
    {
        if (_ctsPool.Count < _options.WorkerItemPoolCapacity)
        {
#if NET
            try
            {
                if (cts.TryReset())
                {
                    _ctsPool.Push(cts);
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
            }
#endif
        }

        cts.Dispose();
    }

    private static void PostFailure(
        PostScheduler? scheduler,
        WorkerHandle handle,
        WorkerJobFailureKind kind,
        WorkerJobExceptionInfo error)
    {
        if (scheduler == null)
            return;

        _ = scheduler.TryPost(
            new WorkerJobFailedEvent(handle, kind, error));
    }

    [Conditional("DEBUG")]
    private void RequireOwnerThreadDebug()
    {
        _owner.RequireOwnerThread();
    }

    private sealed class CancellationCallbackState
    {
        public CancellationCallbackState(
            ScopeEndpoint endpoint,
            WorkerHandle handle)
        {
            Endpoint = endpoint;
            Handle = handle;
        }

        public ScopeEndpoint Endpoint { get; }

        public WorkerHandle Handle { get; }
    }

    private struct StateSlot
    {
        public int Version;
        public int PublicState;
        public bool InUse;
        public bool CancelRequested;
        public bool ExecutionStarted;

        public CancellationTokenSource? Cancellation;

        public CancellationTokenRegistration ExternalCancellationRegistration;
    }
}
