using System;
using System.Collections.Generic;
using System.Threading;

namespace LayerBase.Scope.Completion;

internal sealed class ScopeAwaitRegistry
{
    private readonly List<IScopePromiseControl> _promises = new();
    private readonly object _gate = new();
    private bool _closed;

    public bool TryRegister(IScopePromiseControl promise)
    {
        if (promise == null) throw new ArgumentNullException(nameof(promise));

        lock (_gate)
        {
            if (_closed) return false;
            _promises.Add(promise);
            return true;
        }
    }

    public void Unregister(IScopePromiseControl promise)
    {
        if (promise == null) throw new ArgumentNullException(nameof(promise));

        lock (_gate)
        {
            _promises.Remove(promise);
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
            _promises.Clear();
        }

        foreach (var promise in pending)
        {
            promise.TrySetException(reason);
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            _closed = true;
            _promises.Clear();
        }
    }

    public int PendingCount
    {
        get
        {
            lock (_gate) return _promises.Count;
        }
    }
}
