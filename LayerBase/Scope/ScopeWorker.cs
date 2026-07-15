namespace LayerBase.Scope;

internal sealed class ScopeWorker : IDisposable
{
    private readonly ScopeRuntime _runtime;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _started = new(false);
    private bool _startedThread;

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
        if (_startedThread)
            return;

        _startedThread = true;
        _thread.Start();
        _started.Wait();
    }

    public void Dispose()
    {
        if (_startedThread && _runtime.State != ScopeRuntimeState.Disposed)
            _ = _runtime.RequestDisposeAsync();

        if (_thread.IsAlive && !ReferenceEquals(Thread.CurrentThread, _thread))
            _thread.Join();

        if (!_startedThread)
            _runtime.Dispose();
    }

    private void Run()
    {
        _started.Set();
        SynchronizationContext? previousContext = SynchronizationContext.Current;
        try
        {
            _runtime.InstallSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_runtime.SynchronizationContext);

            while (_runtime.State != ScopeRuntimeState.Disposed)
            {
                float deltaTime = GetDeltaTime();
                _runtime.PumpScopeResources(deltaTime);
                Sleep();
            }
        }
        finally
        {
            try
            {
                if (_runtime.State != ScopeRuntimeState.Disposed)
                    _runtime.RunRuntimeStop();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
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
