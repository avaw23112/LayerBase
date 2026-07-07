using LayerBase.Async;
using LayerBase.Core.Event;

namespace LayerBase.Core.EventHandler;

/// <summary>
/// 同步事件处理委托。返回 EventHandledState 以控制事件流是否继续。
/// </summary>
public delegate EventHandledState EventHandleDelegate<TValue>(in TValue value) where TValue : struct;

/// <summary>
/// 事件通知委托（无返回值）。
/// </summary>
public delegate void EventNotifyDelegate<TValue>(in TValue value) where TValue : struct;

/// <summary>
/// 异步事件处理委托。
/// </summary>
public delegate LBTask EventHandleDelegateAsync<in TValue>(TValue value) where TValue : struct;

/// <summary>
/// 事件处理器的标记接口。
/// </summary>
public interface IEventHandler
{
}

/// <summary>
/// 泛型同步事件处理器接口。实现 Deal 方法处理事件。
/// </summary>
public interface IEventHandler<TValue> : IEventHandler where TValue : struct
{
    public void Deal(in TValue @event);
}

/// <summary>
/// 泛型异步事件处理器接口。实现 Deal 方法以 LBTask 方式处理事件。
/// </summary>
public interface IEventHandlerAsync<in TValue> : IEventHandler where TValue : struct
{
    public LBTask Deal(TValue @event);
}