using System.Buffers;
using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Core.UnmanagedList;

internal interface IUnmanagedList : IDisposable
{
    void Pump();
    void MarkClean();
}

internal class UnmanagedList<Value> : IUnmanagedList where Value : struct
{
    private readonly GlobalEventCenter _center;
    private readonly int _layerIndex;
    private readonly object _lock = new();
    private readonly Action<IUnmanagedList> _onDirty;
    private readonly PooledChunkedOverwriteQueue<Event<Value>> _queue;
    private bool _disposed;

    private Event<Value>[]? _forwardBuffer;
    private int _forwardCount;
    private int _isDirty;

    public UnmanagedList(GlobalEventCenter center, int layerIndex, Action<IUnmanagedList> onDirty)
    {
        _center = center;
        _queue = new PooledChunkedOverwriteQueue<Event<Value>>();
        _layerIndex = layerIndex;
        _onDirty = onDirty;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _queue.Dispose();
            ReturnBuffer();
        }
    }

    public void MarkClean()
    {
        Interlocked.Exchange(ref _isDirty, 0);
    }

    public void Pump()
    {
        MarkClean();


        if (_queue.IsEmpty) return;

        var forwarded = false;
        var lastTargetLayer = -1;
        var myMask = 1UL << _layerIndex;

        lock (_lock)
        {
            if (_disposed) return;

            _queue.ProcessBatch(span =>
            {
                var len = span.Length;
                var i = 0;

                for (; i <= len - 4; i += 4)
                {
                    ref readonly var e0 = ref span[i];
                    ref readonly var e1 = ref span[i + 1];
                    ref readonly var e2 = ref span[i + 2];
                    ref readonly var e3 = ref span[i + 3];

                    if (((e0.TargetMask | e1.TargetMask | e2.TargetMask | e3.TargetMask) & myMask) != 0)
                    {
                        ProcessEvent(in e0, ref forwarded, ref lastTargetLayer);
                        ProcessEvent(in e1, ref forwarded, ref lastTargetLayer);
                        ProcessEvent(in e2, ref forwarded, ref lastTargetLayer);
                        ProcessEvent(in e3, ref forwarded, ref lastTargetLayer);
                    }
                    else
                    {
                        ForwardOnly(in e0, ref forwarded, ref lastTargetLayer);
                        ForwardOnly(in e1, ref forwarded, ref lastTargetLayer);
                        ForwardOnly(in e2, ref forwarded, ref lastTargetLayer);
                        ForwardOnly(in e3, ref forwarded, ref lastTargetLayer);
                    }
                }

                for (; i < len; i++) ProcessEvent(in span[i], ref forwarded, ref lastTargetLayer);

                FlushForwardBuffer(ref forwarded);
            });
        }

        if (forwarded) _center.WakeLayer(lastTargetLayer);
    }

    private void ReturnBuffer()
    {
        if (_forwardBuffer != null)
        {
            ArrayPool<Event<Value>>.Shared.Return(_forwardBuffer);
            _forwardBuffer = null;
        }

        _forwardCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessEvent(in Event<Value> @event, ref bool forwarded, ref int lastTargetLayer)
    {
        var state = (@event.TargetMask & (1UL << _layerIndex)) != 0
            ? _center.DispatchLocal(_layerIndex, in @event)
            : EventHandledState.Continue;

        if (state == EventHandledState.Continue) ForwardOnly(in @event, ref forwarded, ref lastTargetLayer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ForwardOnly(in Event<Value> @event, ref bool forwarded, ref int lastTargetLayer)
    {
        var nextLayer = @event.FindNextTarget(_layerIndex, _center);
        if (nextLayer != -1)
        {
            if (lastTargetLayer != -1 && lastTargetLayer != nextLayer) FlushForwardBuffer(ref forwarded);
            lastTargetLayer = nextLayer;


            _forwardBuffer ??= ArrayPool<Event<Value>>.Shared.Rent(256);
            if (_forwardCount >= _forwardBuffer.Length)
            {
                var newBuffer = ArrayPool<Event<Value>>.Shared.Rent(_forwardBuffer.Length * 2);
                Array.Copy(_forwardBuffer, newBuffer, _forwardBuffer.Length);
                ArrayPool<Event<Value>>.Shared.Return(_forwardBuffer);
                _forwardBuffer = newBuffer;
            }

            _forwardBuffer[_forwardCount++] = @event;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushForwardBuffer(ref bool forwarded)
    {
        if (_forwardCount == 0 || _forwardBuffer == null) return;

        var target = _forwardBuffer[0].FindNextTarget(_layerIndex, _center);
        var span = _forwardBuffer.AsSpan(0, _forwardCount);

#if NETCOREAPP || NET5_0_OR_GREATER
        _center.EnqueueEventBatchInternal<Value>(target, span);
#else
        foreach (var ev in span) _center.EnqueueEventInternal(target, in ev);
#endif
        _forwardCount = 0;
        forwarded = true;
    }

    public void Post(in Event<Value> val)
    {
        lock (_lock)
        {
            if (_disposed) return;
            _queue.EnqueueOverwrite(val);
        }

        if (Interlocked.CompareExchange(ref _isDirty, 1, 0) == 0) _onDirty(this);
    }

    public bool TryDequeue(out Event<Value> @event)
    {
        lock (_lock)
        {
            return _queue.TryDequeue(out @event);
        }
    }
}