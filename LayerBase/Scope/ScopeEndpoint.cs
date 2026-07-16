namespace LayerBase.Scope;

public readonly struct ScopeEndpoint
{
    internal ScopeEndpoint(ScopeAddress address, ScopeTransport transport)
    {
        Address = address;
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public ScopeAddress Address { get; }

    internal ScopeTransport Transport { get; }
}
