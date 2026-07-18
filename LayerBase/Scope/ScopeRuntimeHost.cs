using LayerBase.Async;
using LayerBase.Actor;

namespace LayerBase.Scope;

internal sealed class ScopeRuntimeHost : IDisposable
{
    private readonly ScopeRuntimeDirectory _directory;
    private readonly ScopeRuntime[] _inlineScopes;
    private readonly ScopeWorker[] _workers;
    private bool _disposed;

    private ScopeRuntimeHost(ScopeRuntimeDirectory directory, ScopeWorker[] workers)
    {
        var scopes = directory.Runtimes;
        var inlineScopes = new List<ScopeRuntime>();

        for (int i = 0; i < scopes.Length; i++)
        {
            if (scopes[i].Options.Threading == ScopeThreadingMode.Inline)
                inlineScopes.Add(scopes[i]);
        }

        _directory = directory;
        _inlineScopes = inlineScopes.ToArray();
        _workers = workers ?? Array.Empty<ScopeWorker>();

        var mainEndpoint = scopes[0].Endpoint;
        for (int i = 0; i < scopes.Length; i++)
        {
            scopes[i].BindHost(this);
            scopes[i].BindMainActorEndpoint(mainEndpoint);
            scopes[i].BindScopeEndpoints(_directory);
        }
    }

    public ScopeRuntime MainScope => _directory.MainScope;

    public IReadOnlyList<ScopeRuntime> Scopes => _directory.Runtimes;

    public bool HasWorkerScopes => _workers.Length > 0;

    public bool TryGetRuntime(int scopeId, out ScopeRuntime scope)
    {
        ThrowIfDisposed();

        if (_directory.TryGetRuntime(scopeId, out var runtime))
        {
            scope = runtime;
            return true;
        }

        scope = null!;
        return false;
    }

    public static ScopeRuntimeHost CreateMain(LayerRuntime runtime, int runtimeId, int generation)
    {
        return Create(runtime, new[] { ScopeExecutionPlan.CreateMain() }, runtimeId, generation);
    }

    public static ScopeRuntimeHost Create(
        LayerRuntime runtime,
        IReadOnlyList<ScopeExecutionPlan> plans,
        int runtimeId,
        int generation)
    {
        if (runtime == null)
            throw new ArgumentNullException(nameof(runtime));
        if (plans == null)
            throw new ArgumentNullException(nameof(plans));
        if (plans.Count == 0)
            throw new ArgumentException("Scope host requires at least MainScope.", nameof(plans));

        var scopes = new ScopeRuntime[plans.Count];
        var workers = new List<ScopeWorker>();
        try
        {
            for (int i = 0; i < plans.Count; i++)
            {
                scopes[i] = new ScopeRuntime(runtime, plans[i], runtimeId, generation);
                if (plans[i].Options.Threading == ScopeThreadingMode.Worker)
                    workers.Add(new ScopeWorker(scopes[i]));
            }

            var directory = new ScopeRuntimeDirectory(scopes);
            return new ScopeRuntimeHost(directory, workers.ToArray());
        }
        catch
        {
            for (int i = 0; i < scopes.Length; i++)
                scopes[i]?.Dispose();
            foreach (var worker in workers)
                worker.Dispose();
            throw;
        }
    }

    public void StartWorkers()
    {
        ThrowIfDisposed();
        for (int i = 0; i < _workers.Length; i++)
            _workers[i].Start();
    }

    public void ApplyFaultPolicy(in ScopeFaultRecord record)
    {
        if (!TryGetRuntime(record.SourceScopeId, out var sourceScope))
            return;

        switch (sourceScope.Options.FaultPolicy)
        {
            case ScopeFaultPolicy.ReportAndContinue:
                break;
            case ScopeFaultPolicy.StopScope:
                _ = sourceScope.RequestStopAsync();
                break;
            case ScopeFaultPolicy.StopRuntime:
                _ = MainScope.RequestStopAsync();
                break;
        }
    }

    public bool TryGetScope<TScope>(out ScopeRef<TScope> scope)
        where TScope : IScopeDefinition
    {
        ThrowIfDisposed();
        var scopeType = typeof(TScope);
        var runtimes = _directory.Runtimes;
        for (int i = 0; i < runtimes.Length; i++)
        {
            if (runtimes[i].Descriptor.ScopeType == scopeType)
            {
                scope = new ScopeRef<TScope>(runtimes[i].Endpoint);
                return true;
            }
        }

        scope = default;
        return false;
    }

    public bool HasInlineScopes =>
        _inlineScopes.Length > 0;

    internal ScopeRuntimeDirectory Directory => _directory;

    public void PumpInlineScopes(
        float deltaTime,
        ref RuntimeFrameBudget budget,
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        ThrowIfDisposed();

        if (_inlineScopes.Length == 0)
            return;

        int startIndex = budget.StartingScopeIndex % _inlineScopes.Length;

        for (int i = 0; i < _inlineScopes.Length; i++)
        {
            int idx = (startIndex + i) % _inlineScopes.Length;
            _inlineScopes[idx].PumpScopeResources(
                deltaTime,
                ref budget,
                exceptionPolicy,
                reportException);
        }

        budget.StartingScopeIndex = (startIndex + 1) % _inlineScopes.Length;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        bool hasTimedOut = false;
        for (int i = _workers.Length - 1; i >= 0; i--)
        {
            _workers[i].Dispose();
            if (_workers[i].ShutdownResult == ScopeWorkerShutdownResult.TimedOut)
                hasTimedOut = true;
        }

        var runtimes = _directory.Runtimes;
        for (int i = runtimes.Length - 1; i >= 0; i--)
        {
            var scope = runtimes[i];
            if (scope.Options.Threading == ScopeThreadingMode.Worker && hasTimedOut)
            {
                if (scope.State != ScopeRuntimeState.Disposed)
                {
                    scope.Transport.CloseBusinessAdmission();
                }
                continue;
            }

            DisposeScopeThroughControl(scope);
        }
    }

    private static void DisposeScopeThroughControl(ScopeRuntime scope)
    {
        if (scope.State == ScopeRuntimeState.Disposed)
            return;

        var disposeTask = scope.RequestDisposeAsync();
        scope.PumpIngress();

        if (!disposeTask.GetAwaiter().IsCompleted)
            scope.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ScopeRuntimeHost));
    }
}
