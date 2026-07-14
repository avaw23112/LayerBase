using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LayerBase.Scope.Completion;

internal sealed class ScopeAwaitRegistry
{
    private readonly LinkedList<IScopePromiseControl> _promises = new();
    private readonly Dictionary<IScopePromiseControl, LinkedListNode<IScopePromiseControl>> _nodes = new(PromiseReferenceComparer.Instance);
    private readonly object _gate = new();
    private bool _closed;

    public bool TryRegister(IScopePromiseControl promise)
    {
        if (promise == null) throw new ArgumentNullException(nameof(promise));

        lock (_gate)
        {
            if (_closed) return false;
            if (_nodes.ContainsKey(promise)) return false;

            LinkedListNode<IScopePromiseControl> node = _promises.AddLast(promise);
            _nodes.Add(promise, node);
            return true;
        }
    }

    public void Unregister(IScopePromiseControl promise)
    {
        if (promise == null) throw new ArgumentNullException(nameof(promise));

        lock (_gate)
        {
            if (_nodes.Remove(promise, out LinkedListNode<IScopePromiseControl>? node))
            {
                _promises.Remove(node);
            }
        }
    }

    public void CancelAll(Exception reason)
    {
        if (reason == null) throw new ArgumentNullException(nameof(reason));

        List<IScopePromiseControl> pending;
        lock (_gate)
        {
            _closed = true;
            pending = new List<IScopePromiseControl>(_promises);
        }

        foreach (var promise in pending)
        {
            if (!promise.IsCompleted && !promise.IsCancelled)
            {
                promise.TrySetException(reason);
            }
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            _closed = true;
            _promises.Clear();
            _nodes.Clear();
        }
    }

    public int PendingCount
    {
        get
        {
            lock (_gate) return _promises.Count;
        }
    }

    private sealed class PromiseReferenceComparer : IEqualityComparer<IScopePromiseControl>
    {
        public static readonly PromiseReferenceComparer Instance = new();

        public bool Equals(IScopePromiseControl? x, IScopePromiseControl? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(IScopePromiseControl obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
