using System.Collections.Concurrent;
using LayerBase.Core.Event;

namespace LayerBase.Worker;

public sealed class WorkerRuntime : IDisposable
{
    private readonly ConcurrentQueue<IWorkerJobItem> _jobs = new();
    private readonly ConcurrentQueue<IWorkerEventItem> _events = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly Thread[] _threads;
    private readonly object _stateGate = new();
    private readonly Dictionary<int, WorkerSlot> _states = new();

    private volatile bool _running;
    private int _nextId;

    internal WorkerRuntime(int workerCount)
    {
        if (workerCount <= 0)
        {
            workerCount = 1;
        }

        _threads = new Thread[workerCount];
    }

    public WorkerHandle RunEventJob<TJob, TInput, TEvent>(in TJob job, in TInput input)
        where TJob : struct, IWorkerEventJob<TInput, TEvent>
        where TInput : struct
        where TEvent : struct
    {
        WorkerHandle handle = AllocateHandle();
        _jobs.Enqueue(new WorkerJobItem<TJob, TInput, TEvent>(handle, job, input, this));
        _signal.Set();
        return handle;
    }

    public WorkerState GetState(WorkerHandle handle)
    {
        lock (_stateGate)
        {
            return _states.TryGetValue(handle.Id, out WorkerSlot slot) && slot.Version == handle.Version
                ? slot.State
                : WorkerState.Cancelled;
        }
    }

    public bool Cancel(WorkerHandle handle)
    {
        lock (_stateGate)
        {
            if (!_states.TryGetValue(handle.Id, out WorkerSlot slot) ||
                slot.Version != handle.Version ||
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
        if (_running)
        {
            return;
        }

        _running = true;

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
        while ((maxCount <= 0 || drained < maxCount) &&
               _events.TryDequeue(out IWorkerEventItem? item))
        {
            item.PostTo(scheduler);
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

    internal void EnqueueEvent<TEvent>(in TEvent value)
        where TEvent : struct
    {
        _events.Enqueue(new WorkerEventItem<TEvent>(value));
    }

    public void Dispose()
    {
        _running = false;

        for (int i = 0; i < _threads.Length; i++)
        {
            _signal.Set();
        }

        foreach (Thread thread in _threads)
        {
            thread?.Join();
        }

        _signal.Dispose();

        while (_jobs.TryDequeue(out _))
        {
        }

        while (_events.TryDequeue(out _))
        {
        }
    }

    private WorkerHandle AllocateHandle()
    {
        int id = Interlocked.Increment(ref _nextId);
        var handle = new WorkerHandle(id, version: 1);

        lock (_stateGate)
        {
            _states[id] = new WorkerSlot(handle.Version, WorkerState.Pending);
        }

        return handle;
    }

    private void Run()
    {
        while (_running)
        {
            if (!_jobs.TryDequeue(out IWorkerJobItem? item))
            {
                _signal.WaitOne();
                continue;
            }

            item.Execute();
        }
    }

    private void SetStateIfCurrent(WorkerHandle handle, WorkerState current, WorkerState next)
    {
        lock (_stateGate)
        {
            if (_states.TryGetValue(handle.Id, out WorkerSlot slot) &&
                slot.Version == handle.Version &&
                slot.State == current)
            {
                _states[handle.Id] = slot.WithState(next);
            }
        }
    }

    private void SetState(WorkerHandle handle, WorkerState state)
    {
        lock (_stateGate)
        {
            if (_states.TryGetValue(handle.Id, out WorkerSlot slot) &&
                slot.Version == handle.Version)
            {
                _states[handle.Id] = slot.WithState(state);
            }
        }
    }

    private readonly struct WorkerSlot
    {
        public WorkerSlot(int version, WorkerState state)
        {
            Version = version;
            State = state;
        }

        public int Version { get; }

        public WorkerState State { get; }

        public WorkerSlot WithState(WorkerState state)
        {
            return new WorkerSlot(Version, state);
        }
    }
}

internal interface IWorkerJobItem
{
    void Execute();
}

internal interface IWorkerEventItem
{
    void PostTo(PostScheduler scheduler);
}

internal sealed class WorkerJobItem<TJob, TInput, TEvent> : IWorkerJobItem
    where TJob : struct, IWorkerEventJob<TInput, TEvent>
    where TInput : struct
    where TEvent : struct
{
    private readonly WorkerHandle _handle;
    private readonly WorkerRuntime _runtime;
    private TJob _job;
    private TInput _input;

    public WorkerJobItem(WorkerHandle handle, TJob job, TInput input, WorkerRuntime runtime)
    {
        _handle = handle;
        _job = job;
        _input = input;
        _runtime = runtime;
    }

    public void Execute()
    {
        if (_runtime.IsCancelled(_handle))
        {
            return;
        }

        _runtime.MarkRunning(_handle);

        try
        {
            TEvent result = _job.Execute(in _input);
            _runtime.EnqueueEvent(result);
            _runtime.MarkCompleted(_handle);
        }
        catch (Exception ex)
        {
            _runtime.EnqueueEvent(new WorkerJobFailedEvent(_handle, typeof(TJob), ex));
            _runtime.MarkFailed(_handle);
        }
    }
}

internal sealed class WorkerEventItem<TEvent> : IWorkerEventItem
    where TEvent : struct
{
    private readonly TEvent _value;

    public WorkerEventItem(in TEvent value)
    {
        _value = value;
    }

    public void PostTo(PostScheduler scheduler)
    {
        scheduler.TryPost(_value);
    }
}
