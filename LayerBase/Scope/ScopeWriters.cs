using LayerBase.Core.Event;
using LayerBase.Async;

namespace LayerBase.Scope;

internal interface IScopeEventWriter
{
    bool AcceptsWorkerJobs { get; }

    ScopePostResult Post<TEvent>(in TEvent value)
        where TEvent : struct;

    ScopePostResult PostInternal<TEvent>(
        int routeId,
        ScopeEventClass eventClass,
        in TEvent value)
        where TEvent : struct;
}

internal interface IScopeCallWriter
{
    LBTask<TResponse> Call<TRequest, TResponse>(
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct;
}

internal sealed class RuntimeScopeEventWriter : IScopeEventWriter
{
    private readonly ScopeTransport _transport;

    public RuntimeScopeEventWriter(ScopeTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public bool AcceptsWorkerJobs => _transport.AcceptsWorkerJobs;

    public ScopePostResult Post<TEvent>(in TEvent value)
        where TEvent : struct
    {
        return _transport.EnqueueEvent(in value);
    }

    public ScopePostResult PostInternal<TEvent>(
        int routeId,
        ScopeEventClass eventClass,
        in TEvent value)
        where TEvent : struct
    {
        return _transport.EnqueueEvent(routeId, eventClass, in value);
    }
}

internal sealed class RuntimeScopeCallWriter : IScopeCallWriter
{
    private readonly ScopeTransport _transport;

    public RuntimeScopeCallWriter(ScopeTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public LBTask<TResponse> Call<TRequest, TResponse>(
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return _transport.EnqueueCall<TRequest, TResponse>(in request, cancellationToken);
    }
}
