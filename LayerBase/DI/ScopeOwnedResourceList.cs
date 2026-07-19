using System.Runtime.CompilerServices;
using LayerBase.Lifetime;

namespace LayerBase.DI;

internal sealed class ScopeOwnedResourceList
{
    private readonly List<object> _creationOrder = new();
    private readonly HashSet<object> _instances = new(ReferenceIdentityComparer.Instance);

    public void Add(object instance)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (instance is IAsyncDisposable and not IDisposable)
        {
            throw new InvalidOperationException(
                $"Service `{instance.GetType().FullName}` implements only IAsyncDisposable. Scope service disposal is synchronous.");
        }

        if (instance is not IDisposable)
            return;

        if (_instances.Add(instance))
            _creationOrder.Add(instance);
    }

    public void ReleaseAll()
    {
        var cleanup = new TerminalCleanupRunner();

        for (int i = _creationOrder.Count - 1; i >= 0; i--)
        {
            object instance = _creationOrder[i];
            cleanup.Run(instance.GetType().Name, () =>
            {
                if (instance is IDisposable disposable)
                    disposable.Dispose();

                ServiceLayerBinder.Detach(instance);
            });
        }

        _creationOrder.Clear();
        _instances.Clear();
    }

    private sealed class ReferenceIdentityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceIdentityComparer Instance = new();

        public new bool Equals(object? left, object? right)
        {
            return ReferenceEquals(left, right);
        }

        public int GetHashCode(object instance)
        {
            return RuntimeHelpers.GetHashCode(instance);
        }
    }
}
