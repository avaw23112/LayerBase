namespace LayerBase.DI;

internal readonly struct ScopeServiceSlot
{
    public ScopeServiceSlot(int id, Type serviceType)
    {
        Id = id;
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }

    public int Id { get; }

    public Type ServiceType { get; }
}
