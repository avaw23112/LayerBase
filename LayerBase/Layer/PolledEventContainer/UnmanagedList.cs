using LayerBase.Core.Event;

namespace LayerBase.Core.UnmanagedList;

internal interface IUnmanagedList
{
    void Pump();
    void MarkClean();
}

internal class UnmanagedList<Value> : IUnmanagedList where Value : struct
{
    private readonly GlobalEventCenter _center;
    private readonly int _layerIndex;
    private readonly PooledChunkedOverwriteQueue<Event<Value>> _queue;
    private readonly Action<IUnmanagedList> _onDirty;
    private int _isDirty;

    public UnmanagedList(GlobalEventCenter center, int layerIndex, Action<IUnmanagedList> onDirty)
    {
        _center = center;
        _queue = new PooledChunkedOverwriteQueue<Event<Value>>();
        _layerIndex = layerIndex;
        _onDirty = onDirty;
    }

    public void MarkClean()
    {
        Interlocked.Exchange(ref _isDirty, 0);
    }

    public void Pump()
    {
        MarkClean();
        var count = _queue.Count;
        if (count <= 0) return;

        var forwarded = false;
        var lastTargetLayer = -1;

        for (var i = 0; i < count; i++)
        {
            if (!_queue.TryDequeue(out var @event)) break;

            var state = _center.DispatchLocal(_layerIndex, in @event);

            if (state == EventHandledState.Continue)
            {
                var nextLayer = @event.FindNextTarget(_layerIndex, _center);
                if (nextLayer != -1)
                {
                    _center.EnqueueEventInternal(nextLayer, in @event);
                    forwarded = true;
                    lastTargetLayer = nextLayer;
                }
            }
        }

        if (forwarded) _center.WakeLayer(lastTargetLayer);
    }

    public void Post(in Event<Value> val)
    {
        _queue.EnqueueOverwrite(val);
        if (Interlocked.CompareExchange(ref _isDirty, 1, 0) == 0)
        {
            _onDirty(this);
        }
    }

    public bool TryDequeue(out Event<Value> @event)
    {
        return _queue.TryDequeue(out @event);
    }
}
