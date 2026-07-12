using System;

namespace LayerBase.Scope.DI;

internal readonly struct ScopeMountContext
{
    private readonly object[] _instances;

    internal ScopeMountContext(object[] instances)
    {
        _instances = instances ?? throw new ArgumentNullException(nameof(instances));
    }

    public T GetAt<T>(int slot) where T : class
    {
        return (T)_instances[slot];
    }
}
