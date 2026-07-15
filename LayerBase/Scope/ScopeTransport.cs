using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;

namespace LayerBase.Scope;

internal sealed class ScopeTransport : IDisposable
{
    private readonly RuntimeScopeEventWriter _eventWriter;
    private readonly RuntimeScopeCallWriter _callWriter;
    private readonly EventPayloadStorage _eventPayloadStorage = new();
    private readonly EventPayloadStorage _callPayloadStorage = new();
    private int _callSequence;
    private bool _disposed;

    public ScopeTransport(ScopeAddress address)
    {
        EventInbox = ScopeBoundedInbox<ScopeEventEnvelope>.CreateEventInbox(
            new ScopeEventInboxOptions(capacity: 1024, reservedForInternal: 128, reservedForCritical: 16));
        CallInbox = ScopeBoundedInbox<ScopeCallEnvelope>.CreateCallInbox(
            new ScopeCallInboxOptions(capacity: 1024, reservedForResponseAndControl: 128));
        _eventWriter = new RuntimeScopeEventWriter(this);
        _callWriter = new RuntimeScopeCallWriter(this);
        Endpoint = new ScopeEndpoint(
            address,
            _eventWriter,
            _callWriter);
    }

    public ScopeEndpoint Endpoint { get; }

    internal ScopeBoundedInbox<ScopeEventEnvelope> EventInbox { get; }

    internal ScopeBoundedInbox<ScopeCallEnvelope> CallInbox { get; }

    internal EventPayloadStorage EventPayloadStorage => _eventPayloadStorage;

    internal EventPayloadStorage CallPayloadStorage => _callPayloadStorage;

    internal ScopePostResult EnqueueEvent<TEvent>(in TEvent value)
        where TEvent : struct
    {
        if (_disposed)
            return ScopePostResult.RuntimeDisposed;

        var payload = _eventPayloadStorage.Store(Endpoint.Address.RuntimeId, in value);
        var envelope = new ScopeEventEnvelope(
            Endpoint.Address,
            EventTypeId<TEvent>.Id,
            ScopeEventClass.Business,
            payload);

        var result = EventInbox.TryEnqueue(envelope, envelope.Class.ToAdmissionClass());
        if (result == ScopeEnqueueResult.Accepted)
            return ScopePostResult.Accepted;

        _eventPayloadStorage.Release(payload);
        return ToPostResult(result);
    }

    internal LBTask<TResponse> EnqueueCall<TRequest, TResponse>(
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (_disposed)
            return LBTask<TResponse>.FromException(new ObjectDisposedException(nameof(ScopeTransport)));
        if (cancellationToken.IsCancellationRequested)
            return LBTask<TResponse>.FromCanceled(cancellationToken);

        var completion = new ScopeCallCompletion<TResponse>();
        var queuedCall = new ScopeQueuedCall<TRequest, TResponse>(
            request,
            completion,
            cancellationToken);
        var payload = _callPayloadStorage.Store(Endpoint.Address.RuntimeId, in queuedCall);
        var envelope = new ScopeCallEnvelope(
            ScopeCallEnvelopeKind.Request,
            ScopeCallClass.BusinessRequest,
            NextCallToken(),
            Endpoint.Address,
            ScopeLocalCallRouteId<TRequest, TResponse>.Id,
            payload,
            ScopeCallResult.None,
            completion);

        var result = CallInbox.TryEnqueue(envelope, envelope.Class.ToAdmissionClass());
        if (result == ScopeEnqueueResult.Accepted)
            return completion.Task;

        _callPayloadStorage.Release(payload);
        completion.TrySetException(new InvalidOperationException($"Scope call enqueue failed: {result}."));
        return completion.Task;
    }

    internal ScopeCallToken NextCallToken()
    {
        return new ScopeCallToken(
            Endpoint.Address.RuntimeGeneration,
            Endpoint.Address.ScopeId,
            Interlocked.Increment(ref _callSequence),
            version: 1);
    }

    internal static ScopePostResult ToPostResult(ScopeEnqueueResult result)
    {
        return result switch
        {
            ScopeEnqueueResult.Full => ScopePostResult.QueueFull,
            ScopeEnqueueResult.Closed => ScopePostResult.RuntimeDisposed,
            ScopeEnqueueResult.StaleEndpoint => ScopePostResult.StaleEndpoint,
            _ => ScopePostResult.Rejected
        };
    }

    public void CloseBusinessAdmission()
    {
        EventInbox.CloseBusinessAdmission();
        CallInbox.CloseBusinessAdmission();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        EventInbox.CloseAllAdmission();
        CallInbox.CloseAllAdmission();
        _callPayloadStorage.Dispose();
        _eventPayloadStorage.Dispose();
    }
}
