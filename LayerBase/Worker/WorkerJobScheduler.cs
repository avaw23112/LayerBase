using LayerBase.Scope;

namespace LayerBase.Worker;

internal enum WorkerExecutorShutdownResult
{
    Stopped,
    TimedOut,
    AlreadyStopped
}

internal sealed class WorkerJobScheduler : IDisposable
{
    private readonly WorkerJobSchedulerOptions _options;
    private readonly object _gate = new();
    private readonly Queue<IWorkerExecutionItem> _queue;
    private ManualResetEventSlim _signal = new(false);
    private Thread[] _threads = Array.Empty<Thread>();
    private bool _threadsStarted;
    private bool _accepting = true;
    private bool _stopRequested;
    private bool _resourcesReleased;

    public WorkerJobScheduler(WorkerJobSchedulerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _queue = new Queue<IWorkerExecutionItem>(options.JobQueueCapacity);
    }

    internal bool TryEnqueue(IWorkerExecutionItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        StartThreadsOnce();

        lock (_gate)
        {
            if (!_accepting ||
                _stopRequested ||
                _queue.Count >= _options.JobQueueCapacity)
            {
                return false;
            }

            _queue.Enqueue(item);
            _signal.Set();
            return true;
        }
    }

    internal void BeginStop()
    {
        List<IWorkerExecutionItem> pending = new();

        lock (_gate)
        {
            if (_stopRequested)
                return;

            _accepting = false;
            _stopRequested = true;

            while (_queue.Count > 0)
                pending.Add(_queue.Dequeue());

            if (_threadsStarted)
                _signal.Set();
        }

        foreach (IWorkerExecutionItem item in pending)
        {
            item.CancelBeforeRun();
        }
    }

    internal WorkerExecutorShutdownResult Stop(in ShutdownDeadline deadline)
    {
        BeginStop();

        if (!_threadsStarted)
        {
            ReleaseResources();
            return WorkerExecutorShutdownResult.AlreadyStopped;
        }

        bool hadLiveThread = false;

        for (int i = 0; i < _threads.Length; i++)
        {
            Thread thread = _threads[i];

            if (!thread.IsAlive)
                continue;

            hadLiveThread = true;

            int remainingMilliseconds = deadline.RemainingMilliseconds;

            if (remainingMilliseconds <= 0 ||
                !thread.Join(remainingMilliseconds))
            {
                return WorkerExecutorShutdownResult.TimedOut;
            }
        }

        ReleaseResources();

        return hadLiveThread
            ? WorkerExecutorShutdownResult.Stopped
            : WorkerExecutorShutdownResult.AlreadyStopped;
    }

    public void Dispose()
    {
        var deadline = ShutdownDeadline.Start(
            TimeSpan.FromMilliseconds(_options.ShutdownTotalTimeoutMilliseconds));

        _ = Stop(in deadline);
    }

    private void StartThreadsOnce()
    {
        if (_threadsStarted)
            return;

        lock (_gate)
        {
            if (_threadsStarted)
                return;

            int count = _options.WorkerCount;
            _signal = new ManualResetEventSlim(false);
            _threads = new Thread[count];

            for (int i = 0; i < count; i++)
            {
                int workerIndex = i;

                _threads[i] = new Thread(() => WorkerLoop(workerIndex))
                {
                    IsBackground = true,
                    Name = $"LayerBase Worker {workerIndex}"
                };

                _threads[i].Start();
            }

            _threadsStarted = true;
        }
    }

    private void WorkerLoop(int workerIndex)
    {
        while (true)
        {
            IWorkerExecutionItem? item = null;

            lock (_gate)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();

                    if (_queue.Count == 0)
                        _signal.Reset();
                }
                else if (_stopRequested)
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

    private void ReleaseResources()
    {
        if (_resourcesReleased)
            return;

        _resourcesReleased = true;
        _signal.Dispose();
    }
}
