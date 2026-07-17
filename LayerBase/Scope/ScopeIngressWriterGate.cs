using System;
using System.Threading;

namespace LayerBase.Scope;

internal sealed class ScopeIngressWriterGate : IDisposable
{
    private const int StateOpen = 0;
    private const int StateClosing = 1;
    private const int StateDisposed = 2;

    private int _state;
    private int _activeWriters;
    private readonly ManualResetEventSlim _drained = new(initialState: false);

    public bool TryEnter(out ScopeIngressWriterLease lease)
    {
        lease = default;

        if (Volatile.Read(ref _state) != StateOpen)
        {
            return false;
        }

        Interlocked.Increment(ref _activeWriters);

        if (Volatile.Read(ref _state) != StateOpen)
        {
            Exit();
            return false;
        }

        lease = new ScopeIngressWriterLease(this);
        return true;
    }

    public void CloseAndWait()
    {
        int previous = Interlocked.CompareExchange(
            ref _state,
            StateClosing,
            StateOpen);

        if (previous == StateDisposed)
        {
            return;
        }

        if (Volatile.Read(ref _activeWriters) == 0)
        {
            _drained.Set();
        }

        _drained.Wait();
    }

    public void MarkDisposed()
    {
        CloseAndWait();
        Volatile.Write(ref _state, StateDisposed);
        _drained.Set();
    }

    internal void Exit()
    {
        int remaining = Interlocked.Decrement(ref _activeWriters);

        if (remaining < 0)
        {
            throw new InvalidOperationException(
                "Scope ingress writer lease was released more than once.");
        }

        if (remaining == 0 && Volatile.Read(ref _state) != StateOpen)
        {
            _drained.Set();
        }
    }

    public void Dispose()
    {
        MarkDisposed();
        _drained.Dispose();
    }
}

internal struct ScopeIngressWriterLease : IDisposable
{
    private ScopeIngressWriterGate? _owner;

    internal ScopeIngressWriterLease(ScopeIngressWriterGate owner)
    {
        _owner = owner;
    }

    public void Dispose()
    {
        var owner = Interlocked.Exchange(ref _owner, null);
        owner?.Exit();
    }
}
