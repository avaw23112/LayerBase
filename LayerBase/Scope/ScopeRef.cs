using LayerBase.Async;

namespace LayerBase.Scope;

public readonly struct ScopeRef<TScope>
    where TScope : IScopeDefinition
{
    private readonly ScopeEndpoint _endpoint;

    internal ScopeRef(ScopeEndpoint endpoint)
    {
        _endpoint = endpoint;
    }

    public ScopeAddress Address => _endpoint.Address;

    public bool TryPost<TEvent>(in TEvent value)
        where TEvent : struct
    {
        return Post(in value).IsAccepted;
    }

    public ScopePostResult Post<TEvent>(in TEvent value)
        where TEvent : struct
    {
        return _endpoint.Transport != null
            ? _endpoint.Transport.EnqueueEvent(in value)
            : ScopePostResult.StaleEndpoint;
    }

    public LBTask<TResponse> Call<TRequest, TResponse>(
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return _endpoint.Transport != null
            ? _endpoint.Transport.EnqueueCall<TRequest, TResponse>(in request, cancellationToken)
            : LBTask<TResponse>.FromException(
                new InvalidOperationException("Scope endpoint is not available."));
    }

    internal ScopePostResult PostInternal<TEvent>(
        int routeId,
        ScopeEventClass eventClass,
        in TEvent value)
        where TEvent : struct
    {
        return _endpoint.Transport != null
            ? _endpoint.Transport.EnqueueEvent(routeId, eventClass, in value)
            : ScopePostResult.StaleEndpoint;
    }

    internal LBTask<TResponse> CallInternal<TRequest, TResponse>(
        int routeId,
        ScopeCallClass callClass,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return _endpoint.Transport != null
            ? _endpoint.Transport.EnqueueCall<TRequest, TResponse>(routeId, callClass, in request, cancellationToken)
            : LBTask<TResponse>.FromException(
                new InvalidOperationException("Scope endpoint is not available."));
    }
}
