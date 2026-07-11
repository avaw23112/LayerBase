namespace LayerBase.Scope;

public sealed class ScopeRuntimeHost : IDisposable
{
    private readonly ScopeRuntime[] _scopes;
    private readonly ScopeRouteTable _routes;
    private bool _disposed;

    private ScopeRuntimeHost(
        ScopeRuntime[] scopes,
        IReadOnlyList<ScopeRuntimePlan> plans,
        ScopeTypeIdResolver? scopeTypeResolver)
    {
        _scopes = scopes;
        IReadOnlyDictionary<Type, int>? scopeTypeRoutes = scopeTypeResolver == null
            ? CreateScopeTypeRoutes(plans)
            : null;
        _routes = new ScopeRouteTable(scopes, scopeTypeRoutes, scopeTypeResolver);
        for (int i = 0; i < scopes.Length; i++)
        {
            scopes[i].BindRoutes(_routes);
        }
    }

    public IReadOnlyList<ScopeRuntime> Scopes => _scopes;

    public ScopeRouteTable Routes => _routes;

    public bool TryGetScope(int scopeId, out ScopeRuntime scope)
    {
        ThrowIfDisposed();
        return _routes.TryGetScope(scopeId, out scope);
    }

    public ScopeRef<TScope> GetScopeRef<TScope>(int targetScopeId)
    {
        ThrowIfDisposed();
        return _routes.GetScopeRef<TScope>(targetScopeId);
    }

    public ScopeRef<TScope> GetScopeRef<TScope>()
    {
        ThrowIfDisposed();
        return _routes.GetScopeRef<TScope>();
    }

    public bool TryPost(int targetScopeId, ScopePostMessage message)
    {
        ThrowIfDisposed();
        return _routes.TryPost(targetScopeId, message);
    }

    public static ScopeRuntimeHost Create(
        IReadOnlyList<ScopeRuntimePlan> plans,
        ScopeRuntimeOptions? options = null,
        ScopePostDispatcher? postDispatcher = null,
        ScopeCallDispatcher? callDispatcher = null,
        ScopeTypeIdResolver? scopeTypeResolver = null)
    {
        if (plans == null)
        {
            throw new ArgumentNullException(nameof(plans));
        }

        var scopes = new ScopeRuntime[plans.Count];
        try
        {
            for (int i = 0; i < plans.Count; i++)
            {
                ScopeRuntimePlan plan = plans[i] ?? throw new ArgumentException("Scope plan list cannot contain null.", nameof(plans));
                scopes[i] = new ScopeRuntime(
                    plan.Descriptor,
                    plan.Services,
                    options,
                    postDispatcher,
                    callDispatcher);
            }

            return new ScopeRuntimeHost(scopes, plans, scopeTypeResolver);
        }
        catch
        {
            for (int i = 0; i < scopes.Length; i++)
            {
                scopes[i]?.Dispose();
            }

            throw;
        }
    }

    private static IReadOnlyDictionary<Type, int> CreateScopeTypeRoutes(IReadOnlyList<ScopeRuntimePlan> plans)
    {
        var routes = new Dictionary<Type, int>();
        for (int i = 0; i < plans.Count; i++)
        {
            ScopeRuntimePlan plan = plans[i];
            if (plan.ScopeType != null)
            {
                routes.Add(plan.ScopeType, plan.Descriptor.ScopeId);
            }
        }

        return routes;
    }

    public void Start()
    {
        ThrowIfDisposed();
        for (int i = 0; i < _scopes.Length; i++)
        {
            _scopes[i].Start();
        }
    }

    public void Pump(float deltaTime)
    {
        ThrowIfDisposed();
        for (int i = 0; i < _scopes.Length; i++)
        {
            _scopes[i].Pump(deltaTime);
        }
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        for (int i = _scopes.Length - 1; i >= 0; i--)
        {
            _scopes[i].Stop();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _routes.Dispose();
        for (int i = _scopes.Length - 1; i >= 0; i--)
        {
            _scopes[i].Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopeRuntimeHost));
        }
    }
}
