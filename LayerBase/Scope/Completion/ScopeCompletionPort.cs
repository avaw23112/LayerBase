using System;
using System.Collections.Generic;
using LayerBase.Async;

namespace LayerBase.Scope.Completion;

internal sealed class ScopeCompletionPort
{
    private readonly ReliableContinuationInbox _ready;
    private readonly HashSet<IScopePromiseControl> _pending = new();
    private readonly object _gate = new();
    private readonly int _capacity;
    private bool _closed;

    public ScopeCompletionPort(int capacity)
    {
        _capacity = Math.Max(1, capacity);
        _ready = new ReliableContinuationInbox(_capacity);
    }

    public int PendingCount
    {
        get
        {
            lock (_gate) return _pending.Count;
        }
    }

    public bool TryReserve(IScopePromiseControl promise)
    {
        if (promise == null) throw new ArgumentNullException(nameof(promise));

        lock (_gate)
        {
            if (_closed || _pending.Count >= _capacity)
            {
                return false;
            }

            return _pending.Add(promise);
        }
    }

    public bool TryPublishCompleted(IScopePromiseControl promise, Action continuation)
    {
        if (promise == null) throw new ArgumentNullException(nameof(promise));
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));

        bool reserved;
        lock (_gate)
        {
            reserved = _pending.Contains(promise);
        }

        if (!reserved)
        {
            return false;
        }

        return _ready.TryEnqueue(new LayerContinuation(
            () =>
            {
                try
                {
                    continuation();
                }
                finally
                {
                    Release(promise);
                }
            },
            serviceId: -1,
            taskId: -1,
            trace: ScopeTrace.Empty));
    }

    public void Release(IScopePromiseControl promise)
    {
        if (promise == null) throw new ArgumentNullException(nameof(promise));

        lock (_gate)
        {
            _pending.Remove(promise);
        }
    }

    public void CloseForNewReservations()
    {
        lock (_gate)
        {
            _closed = true;
        }
    }

    public void CloseAndCancelAll(Exception reason)
    {
        if (reason == null) throw new ArgumentNullException(nameof(reason));

        IScopePromiseControl[] pending;
        lock (_gate)
        {
            _closed = true;
            pending = new IScopePromiseControl[_pending.Count];
            _pending.CopyTo(pending);
        }

        foreach (IScopePromiseControl promise in pending)
        {
            promise.TrySetException(reason);
        }
    }

    public bool TryDequeue(out LayerContinuation continuation)
    {
        return _ready.TryDequeue(out continuation);
    }
}
