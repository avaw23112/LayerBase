using LayerBase.Async;
using LayerBase.Core.Event;

namespace LayerBase.Core.EventHandler
{
    /// <summary>
    /// 阻塞事件.可截断事件流
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public delegate EventHandledState EventHandlerDelegate<TValue>(in Event<TValue>  value) where TValue : struct;
    
    /// <summary>
    /// 异步事件.不可截断事件流
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public delegate LBTask EventHandlerDelegateAsync<TValue>( Event<TValue> value) where TValue : struct;
    
    /// <summary>
    /// 事件处理器
    /// </summary>
    public interface IEventHandler {}
    public interface IEventHandler<TValue> : IEventHandler where TValue : struct
    {
        public void Deal(in Event<TValue> @event);
    } 
    public interface IEventHandlerAsync<TValue> :IEventHandler where TValue : struct
    {
        public LBTask Deal(Event<TValue> @event);
    } 
}

