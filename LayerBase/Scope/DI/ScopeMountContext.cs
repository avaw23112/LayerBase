using System;

namespace LayerBase.Scope.DI;

public readonly struct ScopeMountContext
{
    private readonly object[] _objects;
    private readonly int[] _dependencySlots;
    private readonly int _offset;

    internal ScopeMountContext(
        object[] objects,
        int[] dependencySlots,
        int offset)
    {
        _objects = objects ?? throw new ArgumentNullException(nameof(objects));
        _dependencySlots = dependencySlots ?? throw new ArgumentNullException(nameof(dependencySlots));
        _offset = offset;
    }

    public T GetAt<T>(int localDependencyId) where T : class
    {
        int slot = _dependencySlots[_offset + localDependencyId];
        return (T)_objects[slot];
    }

    [Obsolete("Generated mount code should use GetAt<T>(localDependencyId).")]
    public T Get<T>() where T : class
    {
        return GetAt<T>(0);
    }
}
