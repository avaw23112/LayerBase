using LayerBase.Async;
using LayerBase.Core.Event;

namespace LayerBase.Core.EventHandler;

public delegate EventHandledState EventHandleDelegate<TValue>(in TValue value) where TValue : struct;

public delegate void EventNotifyDelegate<TValue>(in TValue value) where TValue : struct;

public delegate LBTask EventHandleDelegateAsync<in TValue>(TValue value) where TValue : struct;

public interface IEventHandler
{
}

public interface IEventHandler<TValue> : IEventHandler where TValue : struct
{
    public void Deal(in TValue @event);
}

public interface IEventHandlerAsync<in TValue> : IEventHandler where TValue : struct
{
    public LBTask Deal(TValue @event);
}