using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LayerBase.Lifetime;

namespace LayerBase.DI;

internal sealed class OwnedDisposableRegistry
{
    private readonly ConcurrentDictionary<object, int> _instances = new(ReferenceEqualityComparer.Instance);
    private readonly ConcurrentQueue<(object Instance, int OwnerScopeId)> _creationOrder = new();
    private int _disposed;

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public void Register(object instance, int ownerScopeId)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (IsDisposed)
            return;

        if (_instances.TryAdd(instance, ownerScopeId))
        {
            _creationOrder.Enqueue((instance, ownerScopeId));
        }
    }

    public void ReleaseScope(int ownerScopeId, TerminalCleanupRunner cleanup)
    {
        var toRelease = new List<object>();

        foreach (var kvp in _instances)
        {
            if (kvp.Value == ownerScopeId)
            {
                toRelease.Add(kvp.Key);
            }
        }

        for (int i = toRelease.Count - 1; i >= 0; i--)
        {
            object instance = toRelease[i];

            if (_instances.TryRemove(instance, out _))
            {
                if (instance is IDisposable disposable)
                {
                    string name = instance.GetType().Name;
                    cleanup.Run(name, () => disposable.Dispose());
                }
            }
        }
    }

    public void ReleaseAll(TerminalCleanupRunner cleanup)
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        var released = new HashSet<object>(ReferenceEqualityComparer.Instance);

        List<object> reverseOrder = new();
        while (_creationOrder.TryDequeue(out var entry))
        {
            if (released.Add(entry.Instance) &&
                _instances.TryRemove(entry.Instance, out _))
            {
                reverseOrder.Add(entry.Instance);
            }
        }

        for (int i = reverseOrder.Count - 1; i >= 0; i--)
        {
            object instance = reverseOrder[i];

            if (instance is IDisposable disposable)
            {
                string name = instance.GetType().Name;
                cleanup.Run(name, () => disposable.Dispose());
            }
        }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
