using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;

namespace LayerBase.Core.Event;

public delegate bool EventFilterDelegate<T>(in T value) where T : struct;

public readonly struct LayerEventStream<T> where T : struct
{
    private readonly Layer _layer;
    private readonly EventFilterDelegate<T>? _predicate;

    internal LayerEventStream(Layer layer, EventFilterDelegate<T>? predicate = null)
    {
        _layer = layer;
        _predicate = predicate;
    }


    public LayerEventStream<T> Where(EventFilterDelegate<T> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        var current = _predicate;
        var next = current == null ? predicate : (in T e) => current(in e) && predicate(in e);
        return new LayerEventStream<T>(_layer, next);
    }


    public void HandleFlow(EventHandleDelegate<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (_predicate == null)
        {
            _layer.SubscribeFlow(handler);
        }
        else
        {
            var pred = _predicate;
            _layer.SubscribeFlow((in T e) => pred(in e) ? handler(in e) : EventHandledState.Continue);
        }
    }


    public void HandleAsync(EventHandleDelegateAsync<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (_predicate == null)
        {
            _layer.SubscribeAsync(handler);
        }
        else
        {
            var pred = _predicate;
            _layer.SubscribeAsync((T e) => pred(in e) ? handler(e) : LBTask.CompletedTask);
        }
    }


    public void HandleParallel(EventNotifyDelegate<T> handler, Action<int, int, int, Exception> reportError)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (_predicate == null)
        {
            _layer.SubscribeParallel(handler, reportError);
        }
        else
        {
            var pred = _predicate;
            _layer.SubscribeParallel((in T e) =>
            {
                if (pred(in e)) handler(in e);
            }, reportError);
        }
    }


    public void Handle(EventNotifyDelegate<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (_predicate == null)
        {
            _layer.Subscribe(handler);
        }
        else
        {
            var pred = _predicate;
            _layer.Subscribe((in T e) =>
            {
                if (pred(in e)) handler(in e);
            });
        }
    }
}