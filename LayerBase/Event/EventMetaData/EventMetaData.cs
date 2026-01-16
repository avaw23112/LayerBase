using LayerBase.Core;
using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;



public interface IEventMetaData
{
    EventCategoryToken GetEventCategoryToken();
}


/// <summary>
/// 用于配置事件的元数据
/// </summary>
/// <typeparam name="Event"></typeparam>
public abstract class EventMetaData<Event> :IEventMetaData where Event : struct 
{
    // ---------------接口适配---------------------
    public EventCategoryToken GetEventCategoryToken()
    {
        return Category;
    }
    
    // ---------------接口适配---------------------
    
    
    // ---------------事件配置---------------------
    
    /// <summary>
    /// 当该类事件创建
    /// </summary>
    /// <param name="e"></param>
    public virtual void OnEventCreated(Event e)
    {
        
    }
    
    /// <summary>
    /// 当该类事件销毁
    /// </summary>
    /// <param name="e"></param>
    public virtual void OnEventDestroyed(Event e)
    {
        
    }
    
    /// <summary>
    /// 当事件出现异常
    /// </summary>
    /// <param name="e"></param>
    public virtual void OnEventExpectation(ref Event e)
    {
        
    }
    
    /// <summary>
    /// 事件合并策略:只会在事件被处理时调用，对事件队列进行处理
    /// </summary>
    public virtual void EventMergeStrategy()
    {
        //需要提供可操作的命令
        //取均值
        //只保留最新
        //保留最旧
        //None
    }
    
    /// <summary>
    /// 事件类别定义
    /// </summary>
    public virtual EventCategoryToken Category => EventCategoryToken.Empty;
    
    /// <summary>
    /// 事件最大缓存队列
    /// </summary>
    public virtual int MaxBufferSize => 256;
    
    /// <summary>
    /// 事件处理器抖动
    /// </summary>
    public virtual int EventHandlerJitter => 0;
    
    /// <summary>
    /// 事件超限策略
    /// </summary>
    public virtual EventQueueOverflowStrategy EventQueueOverflowStrategy => EventQueueOverflowStrategy.OverWrite;
    
  
    // ---------------事件配置---------------------
    
}