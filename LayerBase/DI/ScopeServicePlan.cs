namespace LayerBase.DI;

internal sealed class ScopeServicePlan
{
    private readonly Dictionary<Type, int> _slotsByServiceType;
    private readonly ServiceDescriptor[] _descriptorsBySlot;

    private ScopeServicePlan(
        int ownerScopeId,
        Dictionary<Type, int> slotsByServiceType,
        ServiceDescriptor[] descriptorsBySlot)
    {
        OwnerScopeId = ownerScopeId;
        _slotsByServiceType = slotsByServiceType;
        _descriptorsBySlot = descriptorsBySlot;
    }

    public int OwnerScopeId { get; }

    public int SlotCount => _descriptorsBySlot.Length;

    public IEnumerable<Type> ServiceTypes => _slotsByServiceType.Keys;

    public IEnumerable<ServiceDescriptor> Descriptors => _descriptorsBySlot;

    public static ScopeServicePlan Empty(int ownerScopeId)
    {
        return new ScopeServicePlan(
            ownerScopeId,
            new Dictionary<Type, int>(),
            Array.Empty<ServiceDescriptor>());
    }

    public static ScopeServicePlan Compile(
        int ownerScopeId,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        var byType = new Dictionary<Type, ServiceDescriptor>();
        foreach (ServiceDescriptor descriptor in descriptors)
            byType[descriptor.ServiceType] = descriptor;

        var slots = new Dictionary<Type, int>();
        var descriptorArray = new ServiceDescriptor[byType.Count];
        var index = 0;
        foreach (ServiceDescriptor descriptor in byType.Values)
        {
            slots.Add(descriptor.ServiceType, index);
            descriptorArray[index] = descriptor;
            index++;
        }

        return new ScopeServicePlan(ownerScopeId, slots, descriptorArray);
    }

    public bool Contains(Type serviceType)
    {
        return _slotsByServiceType.ContainsKey(serviceType);
    }

    public bool TryGetDescriptor(
        Type serviceType,
        out int slot,
        out ServiceDescriptor descriptor)
    {
        if (_slotsByServiceType.TryGetValue(serviceType, out slot))
        {
            descriptor = _descriptorsBySlot[slot];
            return true;
        }

        descriptor = null!;
        return false;
    }

    public ServiceDescriptor GetDescriptor(int slot)
    {
        return _descriptorsBySlot[slot];
    }
}
