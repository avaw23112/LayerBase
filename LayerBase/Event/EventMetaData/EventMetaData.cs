using LayerBase.Core;
using LayerBase.Core.EventCatalogue;
using LayerBase.Core.EventStateTrace;
using LayerBase.Core.Event;
using LayerBase.Tools.Timer;

namespace LayerBase.Event.EventMetaData;


public interface IEventMetaData
{
    EventCategoryToken GetEventCategoryToken();
    void OnEventCreated(ref                EventState e);
    void OnEventDestroyed(ref              EventState e);
    void OnEventExpectation<EventType>(EventType  e,Exception exception) where EventType : struct;
    int MaxBufferSize { get; }
    int EventHandlerJitter { get; }
    EventQueueOverflowStrategy EventQueueOverflowStrategy { get; }
    void EventMergeStrategy<EventType>(PooledChunkedOverwriteQueue<Event<EventType>>  queue) where EventType  : struct;
    
}


/// <summary>
/// 用于配置事件的元数据
/// </summary>
/// <typeparam name="EventType"></typeparam>
public abstract class EventMetaData<EventType> :IEventMetaData where EventType : struct 
{
    public static TimerScheduler TimerScheduler = TimerSchedulers.GetOrCreate(nameof(EventType));
    public static bool IsFrequencyGateOpen => TimerScheduler.IsFrequencyGateOpen;
    
    public EventMetaData()
    {
        TimerScheduler.SetFrequency(EventHandlerJitter);
    }
    
    // ---------------接口适配---------------------
    public EventCategoryToken GetEventCategoryToken()
    {
        return Category;
    }
    
    /// <summary>
    /// 当该类事件创建
    /// </summary>
    /// <param name="e"></param>
    public virtual void OnEventCreated(ref EventState e)
    {
        
    }
    
    /// <summary>
    /// 当该类事件销毁
    /// </summary>
    /// <param name="e"></param>
    public virtual void OnEventDestroyed(ref EventState e)
    {
        
    }
    /// <summary>
    /// 事件合并策略:只会在事件被处理时调用，对事件队列进行处理
    /// </summary>
    public virtual void OnEventExpectation<EventType>(EventType e, Exception exception) where EventType : struct
    {
    }
    /// <summary>
    /// 事件合并策略:只会在事件被处理时调用，对事件队列进行处理
    /// </summary>
    public virtual void EventMergeStrategy<EventType>(PooledChunkedOverwriteQueue<Event<EventType>> queue) where EventType : struct
    {
    }
    

    // ---------------事件配置---------------------
    
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