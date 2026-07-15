namespace LayerBase.Scope;

public readonly struct ScopeEndpoint
{
    internal ScopeEndpoint(ScopeAddress address, IScopeEventWriter eventWriter, IScopeCallWriter callWriter)
    {
        Address = address;
        EventWriter = eventWriter ?? throw new ArgumentNullException(nameof(eventWriter));
        CallWriter = callWriter ?? throw new ArgumentNullException(nameof(callWriter));
    }

    public ScopeAddress Address { get; }

    internal IScopeEventWriter? EventWriter { get; }

    internal IScopeCallWriter? CallWriter { get; }
}
