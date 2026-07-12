using System.Threading;
using LayerBase.Async;

namespace LayerBase.Scope;

public delegate bool ScopeTypeIdResolver(Type scopeType, out int scopeId);

public sealed class ScopeRouteTable : IDisposable
{
    private static int s_generationCounter;
    private readonly ScopeRuntime?[] _scopes;
    private readonly IReadOnlyDictionary<Type, int>? _scopeIdsByType;
    private readonly ScopeTypeIdResolver? _scopeIdResolver;

    // High 32 bits: RouteTable Generation
    // Low 32 bits: (reserved)
    private long _cachedEntry;
    private bool _disposed;

    public ScopeRouteTable(
        IReadOnlyList<ScopeRuntime> scopes,
        IReadOnlyDictionary<Type, int>? scopeIdsByType = null,
        ScopeTypeIdResolver? scopeIdResolver = null)
    {
        if (scopes == null)
        {
            throw new ArgumentNullException(nameof(scopes));
        }

        int generation = Interlocked.Increment(ref s_generationCounter);
        Interlocked.Exchange(ref _cachedEntry, PackEntry(generation, 0));
        int maxScopeId = -1;
        for (int i = 0; i < scopes.Count; i++)
        {
            ScopeRuntime scope = scopes[i] ?? throw new ArgumentException("Scope list cannot contain null.", nameof(scopes));
            maxScopeId = Math.Max(maxScopeId, scope.ScopeId);
        }

        _scopes = new ScopeRuntime?[Math.Max(0, maxScopeId + 1)];
        for (int i = 0; i < scopes.Count; i++)
        {
            ScopeRuntime scope = scopes[i];
            if (_scopes[scope.ScopeId] != null)
            {
                throw new ArgumentException($"Duplicate scope id {scope.ScopeId}.", nameof(scopes));
            }

            _scopes[scope.ScopeId] = scope;
        }

        _scopeIdsByType = scopeIdsByType;
        _scopeIdResolver = scopeIdResolver;
    }

    public int Count
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _scopes.Length; i++)
            {
                if (_scopes[i] != null)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public int Generation
    {
        get
        {
            UnpackEntry(Volatile.Read(ref _cachedEntry), out int generation, out _);
            return generation;
        }
    }

    public bool TryGetScope(int scopeId, out ScopeRuntime scope)
    {
        if ((uint)scopeId < (uint)_scopes.Length && _scopes[scopeId] != null)
        {
            scope = _scopes[scopeId]!;
            return true;
        }

        scope = null!;
        return false;
    }

    public ScopeRef<TScope> GetScopeRef<TScope>(int targetScopeId)
    {
        ThrowIfDisposed();
        return new ScopeRef<TScope>(this, targetScopeId);
    }

    public ScopeRef<TScope> GetScopeRef<TScope>()
    {
        ThrowIfDisposed();
        UnpackEntry(Volatile.Read(ref _cachedEntry), out int generation, out _);
        if (!ScopeTypeRouteCache<TScope>.TryGet(generation, out int targetScopeId))
        {
            if (!TryGetScopeId(typeof(TScope), out targetScopeId))
            {
                throw new InvalidOperationException(
                    $"Scope type '{typeof(TScope).FullName}' is not registered.");
            }

            ScopeTypeRouteCache<TScope>.Set(generation, targetScopeId);
        }

        return new ScopeRef<TScope>(this, targetScopeId);
    }

    public bool TryGetScopeId<TScope>(out int scopeId)
    {
        ThrowIfDisposed();
        UnpackEntry(Volatile.Read(ref _cachedEntry), out int generation, out _);
        if (ScopeTypeRouteCache<TScope>.TryGet(generation, out scopeId))
        {
            return true;
        }

        bool found = TryGetScopeId(typeof(TScope), out scopeId);
        if (found)
        {
            ScopeTypeRouteCache<TScope>.Set(generation, scopeId);
        }

        return found;
    }

    public bool TryPost(int targetScopeId, ScopePostMessage message)
    {
        ThrowIfDisposed();
        return TryGetScope(targetScopeId, out ScopeRuntime scope) &&
               scope.TryPost(message);
    }

    public bool TryCall(int targetScopeId, ScopeCallMessage message)
    {
        ThrowIfDisposed();
        return TryGetScope(targetScopeId, out ScopeRuntime scope) &&
               scope.TryCall(message);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private static long PackEntry(int generation, int scopeId)
    {
        return ((long)generation << 32) | (uint)scopeId;
    }

    private static void UnpackEntry(long entry, out int generation, out int scopeId)
    {
        generation = (int)(entry >> 32);
        scopeId = (int)(entry & 0xFFFFFFFF);
    }

    private bool TryGetScopeId(Type scopeType, out int scopeId)
    {
        if (_scopeIdResolver != null && _scopeIdResolver(scopeType, out scopeId))
        {
            return true;
        }

        if (_scopeIdsByType != null && _scopeIdsByType.TryGetValue(scopeType, out scopeId))
        {
            return true;
        }

        scopeId = -1;
        return false;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopeRouteTable));
        }
    }
}

public readonly struct ScopeRef<TScope>
{
    private readonly ScopeRouteTable _routes;

    internal ScopeRef(ScopeRouteTable routes, int targetScopeId)
    {
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        TargetScopeId = targetScopeId;
    }

    public int TargetScopeId { get; }

    public bool TryPost(int eventId, object payload)
    {
        return _routes.TryPost(TargetScopeId, new ScopePostMessage(eventId, payload));
    }

    public ScopePromise<TResult> Call<TResult>(int callId, object payload)
    {
        ScopeRuntime? originScope = ScopeExecution.Current.Runtime;
        var promise = new ScopePromise<TResult>(originScope);
        bool accepted = _routes.TryCall(
            TargetScopeId,
            new ScopeCallMessage(callId, payload, promise));

        if (!accepted)
        {
            promise.SetException(new InvalidOperationException(
                $"Scope id {TargetScopeId} call queue is unavailable or full."));
        }

        return promise;
    }

    public async LBTask<TResult> CallTask<TResult>(int callId, object payload)
    {
        return await Call<TResult>(callId, payload);
    }
}
