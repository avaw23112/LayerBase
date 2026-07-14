using System.Threading;
using LayerBase.Async;

namespace LayerBase.Scope;

public delegate bool ScopeTypeIdResolver(Type scopeType, out int scopeId);

public delegate bool ScopeMessageRouteResolver(Type messageType, out int routeId);

public sealed class ScopeRouteTable : IDisposable
{
    private static int s_generationCounter;
    private readonly ScopeRuntime?[] _scopes;
    private readonly IReadOnlyDictionary<Type, int>? _scopeIdsByType;
    private readonly ScopeTypeIdResolver? _scopeIdResolver;
    private readonly IReadOnlyDictionary<Type, int>? _messageRouteIdsByType;
    private readonly ScopeMessageRouteResolver? _messageRouteResolver;

    // High 32 bits: RouteTable Generation
    // Low 32 bits: (reserved)
    private long _cachedEntry;
    private bool _disposed;

    public ScopeRouteTable(
        IReadOnlyList<ScopeRuntime> scopes,
        IReadOnlyDictionary<Type, int>? scopeIdsByType = null,
        ScopeTypeIdResolver? scopeIdResolver = null,
        IReadOnlyDictionary<Type, int>? messageRouteIdsByType = null,
        ScopeMessageRouteResolver? messageRouteResolver = null)
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
        _messageRouteIdsByType = messageRouteIdsByType;
        _messageRouteResolver = messageRouteResolver;
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

    public bool TryGetMessageRouteId<TMessage>(out int routeId)
    {
        ThrowIfDisposed();
        UnpackEntry(Volatile.Read(ref _cachedEntry), out int generation, out _);
        if (ScopeMessageRouteCache<TMessage>.TryGet(generation, out routeId))
        {
            return true;
        }

        bool found = TryGetMessageRouteId(typeof(TMessage), out routeId);
        if (found)
        {
            ScopeMessageRouteCache<TMessage>.Set(generation, routeId);
        }

        return found;
    }

    internal bool IsScopeRefTargetValid<TScope>(int targetScopeId)
    {
        if (Volatile.Read(ref _disposed))
        {
            return true;
        }

        return !TryGetScopeIdNoThrow(typeof(TScope), out var expectedScopeId) ||
               expectedScopeId == targetScopeId;
    }

    public bool TryPost(int targetScopeId, ScopePostMessage message)
    {
        if (Volatile.Read(ref _disposed))
        {
            return false;
        }

        return TryGetScope(targetScopeId, out ScopeRuntime scope) &&
               scope.TryPost(message);
    }

    public bool TryCall(int targetScopeId, ScopeCallMessage message)
    {
        if (Volatile.Read(ref _disposed))
        {
            return false;
        }

        return TryGetScope(targetScopeId, out ScopeRuntime scope) &&
               scope.TryCall(message);
    }

    public void Dispose()
    {
        Volatile.Write(ref _disposed, true);
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
        if (TryGetScopeIdNoThrow(scopeType, out scopeId))
        {
            return true;
        }

        return false;
    }

    private bool TryGetScopeIdNoThrow(Type scopeType, out int scopeId)
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

    private bool TryGetMessageRouteId(Type messageType, out int routeId)
    {
        if (_messageRouteResolver != null && _messageRouteResolver(messageType, out routeId))
        {
            return true;
        }

        if (_messageRouteIdsByType != null && _messageRouteIdsByType.TryGetValue(messageType, out routeId))
        {
            return true;
        }

        routeId = -1;
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
        if (!_routes.IsScopeRefTargetValid<TScope>(TargetScopeId))
        {
            return false;
        }

        return _routes.TryPost(TargetScopeId, new ScopePostMessage(eventId, payload));
    }

    public bool TryPost<TMessage>(TMessage payload)
    {
        if (!_routes.TryGetMessageRouteId<TMessage>(out int eventId))
        {
            throw new InvalidOperationException(
                $"Scope message type '{typeof(TMessage).FullName}' is not registered.");
        }

        return TryPost(eventId, payload!);
    }

    public ScopePromise<TResult> Call<TResult>(int callId, object payload)
    {
        ScopeRuntime? originScope = ScopeExecution.Current.Runtime;
        var promise = new ScopePromise<TResult>(originScope);
        if (!promise.IsAccepted)
        {
            return promise;
        }

        if (!_routes.IsScopeRefTargetValid<TScope>(TargetScopeId))
        {
            promise.SetException(new InvalidOperationException(
                $"ScopeRef target id {TargetScopeId} does not match scope type '{typeof(TScope).FullName}'."));
            return promise;
        }

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

    public LBTask<TResult> CallTask<TResult>(int callId, object payload)
    {
        return Call<TResult>(callId, payload).ToLBTask();
    }

    public ScopePromise<TResult> Call<TResult, TMessage>(TMessage payload)
    {
        if (!_routes.TryGetMessageRouteId<TMessage>(out int callId))
        {
            var promise = new ScopePromise<TResult>(ScopeExecution.Current.Runtime);
            promise.SetException(new InvalidOperationException(
                $"Scope message type '{typeof(TMessage).FullName}' is not registered."));
            return promise;
        }

        return Call<TResult>(callId, payload!);
    }

    public LBTask<TResult> CallTask<TResult, TMessage>(TMessage payload)
    {
        return Call<TResult, TMessage>(payload).ToLBTask();
    }
}
