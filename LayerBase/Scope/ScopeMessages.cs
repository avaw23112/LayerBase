namespace LayerBase.Scope;

public readonly struct ScopePostMessage
{
    public readonly int EventId;
    public readonly object Payload;

    public ScopePostMessage(int eventId, object payload)
    {
        EventId = eventId;
        Payload = payload;
    }
}

public readonly struct ScopeCallMessage
{
    public readonly int CallId;
    public readonly object Payload;
    public readonly IScopePromise Promise;

    public ScopeCallMessage(int callId, object payload, IScopePromise promise)
    {
        CallId = callId;
        Payload = payload;
        Promise = promise ?? throw new ArgumentNullException(nameof(promise));
    }
}

public interface IScopePromise
{
    void SetException(Exception exception);
}
