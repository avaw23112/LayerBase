using Arch.Core;

namespace LayerBase.ECS.Runtime;

internal sealed class EcsWorker : IDisposable
{
    private readonly LayerRuntime _runtime;
    private readonly World _world;
    private readonly EcsWorkQueue _workQueue;
    private readonly EcsResultQueue _resultQueue;
    private readonly EcsRuntimeOptions _options;
    private readonly AutoResetEvent _signal = new(false);

    private Thread? _thread;
    private volatile bool _running;
    private int _executing;

    public EcsWorker(
        LayerRuntime runtime,
        World world,
        EcsWorkQueue workQueue,
        EcsResultQueue resultQueue,
        EcsRuntimeOptions options)
    {
        _runtime = runtime;
        _world = world;
        _workQueue = workQueue;
        _resultQueue = resultQueue;
        _options = options;
    }

    public bool IsExecuting => Volatile.Read(ref _executing) != 0;

    public void Start()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = _options.WorkerName
        };
        _thread.Start();
    }

    public void Signal()
    {
        _signal.Set();
    }

    public void Stop()
    {
        _running = false;
        _signal.Set();
        _thread?.Join();
        _thread = null;
    }

    public void Dispose()
    {
        Stop();
        _signal.Dispose();
    }

    private void Run()
    {
        EcsThreadGuard.Bind(_runtime.Id, Environment.CurrentManagedThreadId);

        try
        {
            while (_running)
            {
                bool didWork = false;
                int processed = 0;

                while ((_options.MaxWorkItemsPerWake <= 0 || processed < _options.MaxWorkItemsPerWake) &&
                       _workQueue.TryDequeue(out IEcsWorkItem? item))
                {
                    if (item == null)
                    {
                        continue;
                    }

                    didWork = true;
                    processed++;
                    Interlocked.Exchange(ref _executing, 1);
                    EcsThreadGuard.EnterExecution(_runtime.Id, _resultQueue);

                    try
                    {
                        item.Execute(_world, _resultQueue);
                    }
                    catch (Exception ex)
                    {
                        _resultQueue.Enqueue(new EcsWorkFailedResult(item.DebugName, ex));
                    }
                    finally
                    {
                        EcsThreadGuard.ExitExecution(_runtime.Id);
                        Interlocked.Exchange(ref _executing, 0);
                    }
                }

                if (!didWork)
                {
                    _signal.WaitOne();
                }
            }
        }
        finally
        {
            EcsThreadGuard.Unbind(_runtime.Id);
        }
    }
}
