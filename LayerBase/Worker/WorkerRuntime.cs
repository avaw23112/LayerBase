using LayerBase.Core.Event;

namespace LayerBase.Worker;

public sealed class WorkerRuntime : IDisposable
{
    private const int StateCreated = 0;
    private const int StateRunning = 1;
    private const int StateStopping = 2;
    private const int StateDisposed = 3;

    private readonly Queue<IWorkerJobItem> _jobs;
    private readonly Queue<IWorkerEventItem> _events;
    private readonly AutoResetEvent _signal = new(false);
    private readonly Thread[] _threads;
    private readonly object _stateGate = new();
    private readonly object _jobGate = new();
    private readonly object _eventGate = new();
    private readonly WorkerSlot[] _states;
    private readonly WorkerOptions _options;

    private int _lifecycleState;

    internal WorkerRuntime(int workerCount)
        : this(workerCount, WorkerOptions.Default)
    {
    }

    internal WorkerRuntime(int workerCount, WorkerOptions options)
    {
        if (workerCount <= 0)
        {
            workerCount = 1;
        }

        _options = options ?? WorkerOptions.Default;
        _threads = new Thread[workerCount];
        _jobs = new Queue<IWorkerJobItem>(_options.JobQueueCapacity);
        _events = new Queue<IWorkerEventItem>(_options.EventQueueCapacity);
        _states = new WorkerSlot[_options.StateCapacity];
    }

    internal int StateStorageCapacityForTest => _states.Length;

    internal int JobQueueCountForTest
    {
        get
        {
            lock (_jobGate) return _jobs.Count;
        }
    }

    internal int EventQueueCountForTest
    {
        get
        {
            lock (_eventGate) return _events.Count;
        }
    }

    internal int CreatedThreadCountForTest
    {
        get
        {
            int count = 0;
            foreach (var thread in _threads)
            {
                if (thread != null)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public WorkerHandle RunEventJob<TJob, TInput, TEvent>(in TJob job, in TInput input)
        where TJob : struct, IWorkerEventJob<TInput, TEvent>
        where TInput : struct
        where TEvent : struct
    {
        if (Volatile.Read(ref _lifecycleState) >= StateStopping)
        {
            return WorkerHandle.Invalid;
        }

        WorkerHandle handle = AllocateHandle();
        if (handle.IsInvalid)
        {
            return handle;
        }

        var item = WorkerJobItem<TJob, TInput, TEvent>.Rent(handle, job, input, this);
        lock (_jobGate)
        {
            if (_jobs.Count >= _options.JobQueueCapacity ||
                Volatile.Read(ref _lifecycleState) >= StateStopping)
            {
                SetState(handle, WorkerState.Cancelled);
                return handle;
            }

            _jobs.Enqueue(item);
        }

        _signal.Set();
        return handle;
    }

    public WorkerState GetState(WorkerHandle handle)
    {
        if (handle.IsInvalid) return WorkerState.Cancelled;

        lock (_stateGate)
        {
            if ((uint)handle.Id >= (uint)_states.Length)
            {
                return WorkerState.Cancelled;
            }

            ref readonly var slot = ref _states[handle.Id];
            return slot.InUse && slot.Generation == handle.Version
                ? slot.State
                : WorkerState.Cancelled;
        }
    }

    public bool Cancel(WorkerHandle handle)
    {
        if (handle.IsInvalid) return false;

        lock (_stateGate)
        {
            if ((uint)handle.Id >= (uint)_states.Length)
            {
                return false;
            }

            var slot = _states[handle.Id];
            if (!slot.InUse ||
                slot.Generation != handle.Version ||
                slot.State != WorkerState.Pending)
            {
                return false;
            }

            _states[handle.Id] = slot.WithState(WorkerState.Cancelled);
            return true;
        }
    }

    internal void Start()
    {
        if (Interlocked.CompareExchange(ref _lifecycleState, StateRunning, StateCreated) != StateCreated)
        {
            return;
        }

        for (int i = 0; i < _threads.Length; i++)
        {
            var thread = new Thread(Run)
            {
                IsBackground = true,
                Name = $"LayerBase.Worker.{i}"
            };
            _threads[i] = thread;
            thread.Start();
        }
    }

    internal void DrainEventsTo(PostScheduler scheduler, int maxCount)
    {
        int drained = 0;
        while (maxCount <= 0 || drained < maxCount)
        {
            IWorkerEventItem item;
            lock (_eventGate)
            {
                if (_events.Count == 0)
                {
                    return;
                }

                item = _events.Dequeue();
            }

            try
            {
                item.PostTo(scheduler);
            }
            finally
            {
                item.Release();
            }
            drained++;
        }
    }

    internal void MarkRunning(WorkerHandle handle)
    {
        SetStateIfCurrent(handle, WorkerState.Pending, WorkerState.Running);
    }

    internal void MarkCompleted(WorkerHandle handle)
    {
        SetState(handle, WorkerState.Completed);
    }

    internal void MarkFailed(WorkerHandle handle)
    {
        SetState(handle, WorkerState.Failed);
    }

    internal bool IsCancelled(WorkerHandle handle)
    {
        return GetState(handle) == WorkerState.Cancelled;
    }

    internal bool EnqueueEvent<TEvent>(in TEvent value)
        where TEvent : struct
    {
        lock (_eventGate)
        {
            if (_events.Count >= _options.EventQueueCapacity ||
                Volatile.Read(ref _lifecycleState) >= StateStopping)
            {
                return false;
            }

            _events.Enqueue(WorkerEventItem<TEvent>.Rent(value));
            return true;
        }
    }

    public void Dispose()
    {
        var previous = Interlocked.Exchange(ref _lifecycleState, StateDisposed);
        if (previous == StateDisposed)
        {
            return;
        }

        CancelPendingJobs();

        for (int i = 0; i < _threads.Length; i++)
        {
            _signal.Set();
        }

        foreach (Thread thread in _threads)
        {
            thread?.Join();
        }

        lock (_eventGate)
        {
            while (_events.Count > 0)
            {
                _events.Dequeue().Release();
            }
        }

        _signal.Dispose();
    }

    private WorkerHandle AllocateHandle()
    {
        lock (_stateGate)
        {
            for (int i = 0; i < _states.Length; i++)
            {
                var slot = _states[i];
                if (slot.InUse && !IsTerminal(slot.State))
                {
                    continue;
                }

                int generation = NextGeneration(slot.Generation);
                _states[i] = new WorkerSlot(generation, WorkerState.Pending, inUse: true);
                return new WorkerHandle(i, generation);
            }
        }

        return WorkerHandle.Invalid;
    }

    private void Run()
    {
        while (Volatile.Read(ref _lifecycleState) == StateRunning)
        {
            IWorkerJobItem? item = null;
            lock (_jobGate)
            {
                if (_jobs.Count > 0)
                {
                    item = _jobs.Dequeue();
                }
            }

            if (item == null)
            {
                _signal.WaitOne();
                continue;
            }

            try
            {
                item.Execute();
            }
            finally
            {
                item.Release();
            }
        }
    }

    private void CancelPendingJobs()
    {
        lock (_jobGate)
        {
            while (_jobs.Count > 0)
            {
                IWorkerJobItem item = _jobs.Dequeue();
                try
                {
                    item.Cancel();
                }
                finally
                {
                    item.Release();
                }
            }
        }
    }

    private void SetStateIfCurrent(WorkerHandle handle, WorkerState current, WorkerState next)
    {
        if (handle.IsInvalid) return;

        lock (_stateGate)
        {
            if ((uint)handle.Id < (uint)_states.Length &&
                _states[handle.Id].InUse &&
                _states[handle.Id].Generation == handle.Version &&
                _states[handle.Id].State == current)
            {
                _states[handle.Id] = _states[handle.Id].WithState(next);
            }
        }
    }

    private void SetState(WorkerHandle handle, WorkerState state)
    {
        if (handle.IsInvalid) return;

        lock (_stateGate)
        {
            if ((uint)handle.Id < (uint)_states.Length &&
                _states[handle.Id].InUse &&
                _states[handle.Id].Generation == handle.Version)
            {
                _states[handle.Id] = _states[handle.Id].WithState(state);
            }
        }
    }

    private static bool IsTerminal(WorkerState state)
    {
        return state is WorkerState.Completed or WorkerState.Failed or WorkerState.Cancelled;
    }

    private static int NextGeneration(int current)
    {
        int next = unchecked(current + 1);
        return next <= 0 ? 1 : next;
    }

    private readonly struct WorkerSlot
    {
        public WorkerSlot(int generation, WorkerState state, bool inUse)
        {
            Generation = generation;
            State = state;
            InUse = inUse;
        }

        public int Generation { get; }

        public WorkerState State { get; }

        public bool InUse { get; }

        public WorkerSlot WithState(WorkerState state)
        {
            return new WorkerSlot(Generation, state, InUse);
        }
    }
}

internal interface IWorkerJobItem
{
    void Execute();

    void Cancel();

    void Release();
}

internal interface IWorkerEventItem
{
    void PostTo(PostScheduler scheduler);

    void Release();
}

internal sealed class WorkerJobItem<TJob, TInput, TEvent> : IWorkerJobItem
    where TJob : struct, IWorkerEventJob<TInput, TEvent>
    where TInput : struct
    where TEvent : struct
{
    private const int MaxPoolSize = 1024;
    private static readonly Queue<WorkerJobItem<TJob, TInput, TEvent>> Pool = new();
    private static readonly object PoolGate = new();

    private WorkerHandle _handle;
    private WorkerRuntime? _runtime;
    private TJob _job;
    private TInput _input;

    private WorkerJobItem()
    {
    }

    public static WorkerJobItem<TJob, TInput, TEvent> Rent(
        WorkerHandle handle,
        in TJob job,
        in TInput input,
        WorkerRuntime runtime)
    {
        WorkerJobItem<TJob, TInput, TEvent> item;
        lock (PoolGate)
        {
            item = Pool.Count > 0 ? Pool.Dequeue() : new WorkerJobItem<TJob, TInput, TEvent>();
        }

        item._handle = handle;
        item._job = job;
        item._input = input;
        item._runtime = runtime;
        return item;
    }

    public void Execute()
    {
        WorkerRuntime runtime = _runtime ?? throw new ObjectDisposedException(nameof(WorkerJobItem<TJob, TInput, TEvent>));
        if (runtime.IsCancelled(_handle))
        {
            return;
        }

        runtime.MarkRunning(_handle);

        try
        {
            TEvent result = _job.Execute(in _input);
            if (runtime.EnqueueEvent(result))
            {
                runtime.MarkCompleted(_handle);
            }
            else
            {
                runtime.MarkFailed(_handle);
            }
        }
        catch (Exception ex)
        {
            _ = runtime.EnqueueEvent(new WorkerJobFailedEvent(_handle, typeof(TJob), ex));
            runtime.MarkFailed(_handle);
        }
    }

    public void Cancel()
    {
        _runtime?.Cancel(_handle);
    }

    public void Release()
    {
        _handle = WorkerHandle.Invalid;
        _runtime = null;
        _job = default;
        _input = default;

        lock (PoolGate)
        {
            if (Pool.Count < MaxPoolSize)
            {
                Pool.Enqueue(this);
            }
        }
    }
}

internal sealed class WorkerEventItem<TEvent> : IWorkerEventItem
    where TEvent : struct
{
    private const int MaxPoolSize = 1024;
    private static readonly Queue<WorkerEventItem<TEvent>> Pool = new();
    private static readonly object PoolGate = new();

    private TEvent _value;

    private WorkerEventItem()
    {
    }

    public static WorkerEventItem<TEvent> Rent(in TEvent value)
    {
        WorkerEventItem<TEvent> item;
        lock (PoolGate)
        {
            item = Pool.Count > 0 ? Pool.Dequeue() : new WorkerEventItem<TEvent>();
        }

        item._value = value;
        return item;
    }

    public void PostTo(PostScheduler scheduler)
    {
        scheduler.TryPost(_value);
    }

    public void Release()
    {
        _value = default;

        lock (PoolGate)
        {
            if (Pool.Count < MaxPoolSize)
            {
                Pool.Enqueue(this);
            }
        }
    }
}
