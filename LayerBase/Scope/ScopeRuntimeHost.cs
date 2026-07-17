using LayerBase.Async;

namespace LayerBase.Scope;

internal sealed class ScopeRuntimeHost : IDisposable
{
    private readonly ScopeRuntime[] _scopes;
    private readonly ScopeRuntime[] _inlineScopes;
    private readonly ScopeRuntime?[] _scopeById;
    private readonly ScopeEndpoint?[] _endpointById;
    private readonly ScopeWorker[] _workers;
    private bool _disposed;

    private ScopeRuntimeHost(ScopeRuntime[] scopes, ScopeWorker[] workers)
    {
        if (scopes == null || scopes.Length == 0)
            throw new ArgumentException("Scope host must contain at least MainScope.", nameof(scopes));

        if (scopes[0].Descriptor.ScopeId != ScopeDefinitionIds.Main)
            throw new InvalidOperationException("MainScope must be the first scope runtime.");

        int maxScopeId = 0;
        for (int i = 0; i < scopes.Length; i++)
        {
            int scopeId = scopes[i].ScopeId;
            if (scopeId < 0)
                throw new InvalidOperationException($"Scope id cannot be negative: {scopeId}.");

            if (scopeId > maxScopeId)
                maxScopeId = scopeId;
        }

        _scopeById = new ScopeRuntime?[maxScopeId + 1];
        _endpointById = new ScopeEndpoint?[maxScopeId + 1];

        var inlineScopes = new List<ScopeRuntime>();

        for (int i = 0; i < scopes.Length; i++)
        {
            ScopeRuntime scope = scopes[i];
            int scopeId = scope.ScopeId;

            if (_scopeById[scopeId] != null)
                throw new InvalidOperationException($"Duplicate scope id: {scopeId}.");

            _scopeById[scopeId] = scope;
            _endpointById[scopeId] = scope.Endpoint;

            if (scope.Options.Threading == ScopeThreadingMode.Inline)
                inlineScopes.Add(scope);
        }

        _scopes = scopes;
        _inlineScopes = inlineScopes.ToArray();
        _workers = workers ?? Array.Empty<ScopeWorker>();

        var mainEndpoint = _scopes[0].Endpoint;
        for (int i = 0; i < _scopes.Length; i++)
        {
            _scopes[i].BindHost(this);
            _scopes[i].BindMainActorEndpoint(mainEndpoint);
            _scopes[i].BindScopeEndpoints(_endpointById);
        }
    }

    public ScopeRuntime MainScope
    {
        get
        {
            return _scopes[0];
        }
    }

    public IReadOnlyList<ScopeRuntime> Scopes => _scopes;

    public bool HasWorkerScopes => _workers.Length > 0;

    public bool TryGetRuntime(int scopeId, out ScopeRuntime scope)
    {
        ThrowIfDisposed();

        if ((uint)scopeId < (uint)_scopeById.Length)
        {
            ScopeRuntime? runtime = _scopeById[scopeId];
            if (runtime != null)
            {
                scope = runtime;
                return true;
            }
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

            return new ScopeRuntimeHost(scopes, workers.ToArray());
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
        for (int i = 0; i < _scopes.Length; i++)
        {
            if (_scopes[i].Descriptor.ScopeType == scopeType)
            {
                scope = new ScopeRef<TScope>(_scopes[i].Endpoint);
                return true;
            }
        }

        scope = default;
        return false;
    }

    public bool HasInlineScopes =>
        _inlineScopes.Length > 0;

    public void PumpInlineScopes(
        float deltaTime,
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        ThrowIfDisposed();

        for (int i = 0; i < _inlineScopes.Length; i++)
        {
            _inlineScopes[i].PumpScopeResources(
                deltaTime,
                exceptionPolicy,
                reportException);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        for (int i = _workers.Length - 1; i >= 0; i--)
            _workers[i].Dispose();
        for (int i = _scopes.Length - 1; i >= 0; i--)
            DisposeScopeThroughControl(_scopes[i]);
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
