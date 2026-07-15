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
        var writer = _endpoint.EventWriter;
        return writer != null
            ? writer.Post(in value)
            : ScopePostResult.StaleEndpoint;
    }

    public LBTask<TResponse> Call<TRequest, TResponse>(
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        var writer = _endpoint.CallWriter;
        return writer != null
            ? writer.Call<TRequest, TResponse>(in request, cancellationToken)
            : LBTask<TResponse>.FromException(
                new InvalidOperationException("Scope endpoint is not available."));
    }
}
