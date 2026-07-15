namespace LayerBase.Scope;

internal sealed class ScopeWorker : IDisposable
{
    private readonly ScopeRuntime _runtime;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _started = new(false);
    private volatile bool _running;

    public ScopeWorker(ScopeRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = $"LayerBase.Scope.{runtime.Descriptor.Name}"
        };
    }

    public void Start()
    {
        if (_running)
            return;

        _running = true;
        _thread.Start();
        _started.Wait();
    }

    public void RequestStop()
    {
        _running = false;
    }

    public void Dispose()
    {
        RequestStop();
        if (_thread.IsAlive && !ReferenceEquals(Thread.CurrentThread, _thread))
            _thread.Join();
    }

    private void Run()
    {
        _started.Set();
        try
        {
            while (_running)
            {
                float deltaTime = GetDeltaTime();
                _runtime.PumpScopeResources(deltaTime);
                Sleep();
            }
        }
        finally
        {
            _runtime.RunRuntimeStop();
        }
    }

    private float GetDeltaTime()
    {
        int tickRate = _runtime.Options.TickRateHz;
        return tickRate > 0 ? 1f / tickRate : 0f;
    }

    private void Sleep()
    {
        int tickRate = _runtime.Options.TickRateHz;
        if (_runtime.Options.Clock == ScopeClockMode.FixedRate && tickRate > 0)
        {
            Thread.Sleep(Math.Max(1, 1000 / tickRate));
            return;
        }

        Thread.Sleep(1);
    }
}
