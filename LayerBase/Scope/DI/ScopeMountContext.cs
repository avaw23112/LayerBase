using System;

namespace LayerBase.Scope.DI;

public readonly struct ScopeMountContext
{
    private readonly ScopeRuntime _scope;
    private readonly int _serviceSlot;
    private readonly int _contextSlot;

    internal ScopeMountContext(ScopeRuntime scope, int serviceSlot, int contextSlot)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _serviceSlot = serviceSlot;
        _contextSlot = contextSlot;
    }

    public T GetAt<T>(int slot) where T : class
    {
        return _scope.GetServiceAt<T>(slot);
    }

    public T Get<T>() where T : class
    {
        return _scope.GetMountedObject<T>(_serviceSlot, _contextSlot);
    }
}
