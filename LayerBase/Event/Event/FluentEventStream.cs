using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;

namespace LayerBase.Core.Event;

public delegate bool EventFilterDelegate<T>(in T value) where T : struct;

/// <summary>
///     提供链式 API（Fluent API）的事件订阅流。
/// </summary>
public readonly struct LayerEventStream<T> where T : struct
{
    private readonly Layer _layer;
    private readonly EventFilterDelegate<T>? _predicate;

    internal LayerEventStream(Layer layer, EventFilterDelegate<T>? predicate = null)
    {
        _layer = layer;
        _predicate = predicate;
    }

    /// <summary>
    ///     添加过滤条件。只有满足条件的事件才会传递给后续的 Handler。
    /// </summary>
    public LayerEventStream<T> Where(EventFilterDelegate<T> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        var current = _predicate;
        EventFilterDelegate<T> next = current == null ? predicate : (in T e) => current(in e) && predicate(in e);
        return new LayerEventStream<T>(_layer, next);
    }

    /// <summary>
    ///     绑定同步处理函数。
    /// </summary>
    public void Handle(EventHandleDelegate<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (_predicate == null)
        {
            _layer.Subscribe(handler);
        }
        else
        {
            var pred = _predicate;
            _layer.Subscribe((in T e) => pred(in e) ? handler(in e) : EventHandledState.Continue);
        }
    }

    /// <summary>
    ///     绑定异步处理函数。
    /// </summary>
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

    /// <summary>
    ///     绑定并行处理函数。
    /// </summary>
    public void HandleParallel(EventHandleDelegate<T> handler, Action<int, string, string, Exception> reportError)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (_predicate == null)
        {
            _layer.SubscribeParallel(handler, reportError);
        }
        else
        {
            var pred = _predicate;
            _layer.SubscribeParallel((in T e) => { if (pred(in e)) handler(in e); return EventHandledState.Continue; }, reportError);
        }
    }
}
