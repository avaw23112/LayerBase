namespace LayerBase.Scope;

internal sealed class ScopeTransport : IDisposable
{
    private readonly RuntimeScopeEventWriter _eventWriter = new();
    private readonly RuntimeScopeCallWriter _callWriter = new();

    public ScopeTransport(ScopeAddress address)
    {
        EventInbox = ScopeBoundedInbox<ScopeEventEnvelope>.CreateEventInbox(
            new ScopeEventInboxOptions(capacity: 1024, reservedForInternal: 128, reservedForCritical: 16));
        CallInbox = ScopeBoundedInbox<ScopeCallEnvelope>.CreateCallInbox(
            new ScopeCallInboxOptions(capacity: 1024, reservedForResponseAndControl: 128));
        Endpoint = new ScopeEndpoint(
            address,
            _eventWriter,
            _callWriter);
    }

    public ScopeEndpoint Endpoint { get; }

    internal ScopeBoundedInbox<ScopeEventEnvelope> EventInbox { get; }

    internal ScopeBoundedInbox<ScopeCallEnvelope> CallInbox { get; }

    public void AttachRuntime(ScopeRuntime runtime)
    {
        _eventWriter.Attach(runtime);
        _callWriter.Attach(runtime);
    }

    public void Dispose()
    {
        EventInbox.CloseAllAdmission();
        CallInbox.CloseAllAdmission();
        _eventWriter.Detach();
        _callWriter.Detach();
    }
}
