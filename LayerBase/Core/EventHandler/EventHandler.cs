using LayerBase.Async;
using LayerBase.Core.Event;

namespace LayerBase.Core.EventHandler
{
    /// <summary>
    /// 阻塞事件.可截断事件流
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public delegate EventHandledState EventHandlerDelegate<TValue>(TValue  value);
    
    /// <summary>
    /// 异步事件.不可截断事件流
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public delegate LBTask EventHandlerDelegateAsync<TValue>(TValue value);
    
    /// <summary>
    /// 事件处理器
    /// </summary>
    public interface IEventHandler {}
    public interface IEventHandler<TValue> :IEventHandler
    {
        public void Deal(TValue @event);
    } 
    public interface IEventHandlerAsync<TValue> :IEventHandler
    {
        public LBTask Deal(TValue @event);
    } 
}

