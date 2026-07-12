using System;

namespace LayerBase.Scope.DI;

internal struct ScopeServiceSlot
{
    public ScopeServiceSlot(Type serviceType, int slot)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        Slot = slot;
    }

    public Type ServiceType { get; }
    public int Slot { get; }
}
