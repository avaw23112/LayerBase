using LayerBase.Core;
using LayerBase.Core.EventCatalogue;
using LayerBase.Core.EventStateTrace;
using LayerBase.Core.Event;
using LayerBase.Tools.Timer;

namespace LayerBase.Event.EventMetaData;


public interface IEventMetaData
{
    EventCategoryToken GetEventCategoryToken();
    void OnEventCreated(ref EventState e);
    void OnEventDestroyed(ref EventState e);
    void OnEventExpectation<EventType>(EventType e, Exception exception) where EventType : struct;
    void EventMergeStrategy<EventType>(PooledChunkedOverwriteQueue<Event<EventType>> queue) where EventType : struct;
    int MaxBufferSize { get; }
    int EventHandlerJitter { get; }
    EventQueueOverflowStrategy EventQueueOverflowStrategy { get; }
}

/// <summary>
/// 事件元数据：用于配置分类、频率、缓冲与合并策略。
/// </summary>
public abstract class EventMetaData<EventType> : IEventMetaData where EventType : struct
{
    public static readonly TimerScheduler TimerScheduler = TimerSchedulers.GetOrCreate(nameof(EventType));
    public static bool IsFrequencyGateOpen => TimerScheduler.IsFrequencyGateOpen;

    protected EventMetaData()
    {
        // 使用事件抖动值设置频率阀，0 表示关闭频率节流。
        TimerScheduler.SetFrequency(EventHandlerJitter);
    }

    // --------------- 接口适配 ---------------------
    public EventCategoryToken GetEventCategoryToken() => Category;

    /// <summary>事件实例创建时触发。</summary>
    public virtual void OnEventCreated(ref EventState e) { }

    /// <summary>事件实例销毁时触发。</summary>
    public virtual void OnEventDestroyed(ref EventState e) { }

    /// <summary>事件处理异常时触发，可用于记录或合并。</summary>
    public virtual void OnEventExpectation<TValue>(TValue e, Exception exception) where TValue : struct { }

    /// <summary>事件合并策略：处理队列内的积压事件。</summary>
    public virtual void EventMergeStrategy<TValue>(PooledChunkedOverwriteQueue<Event<TValue>> queue) where TValue : struct { }

    // --------------- 事件配置 ---------------------

    /// <summary>事件类别定义。</summary>
    public virtual EventCategoryToken Category => EventCategoryToken.Empty;

    /// <summary>事件最大缓存长度。</summary>
    public virtual int MaxBufferSize => 256;

    /// <summary>事件处理器抖动（秒），用于频率阀。</summary>
    public virtual int EventHandlerJitter => 0;

    /// <summary>事件超限策略。</summary>
    public virtual EventQueueOverflowStrategy EventQueueOverflowStrategy => EventQueueOverflowStrategy.OverWrite;
}
