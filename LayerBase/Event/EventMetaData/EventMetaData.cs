using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;

public interface IEventMetaData
{
    EventCategoryToken GetEventCategoryToken();
    void OnEventExpectation<EventType>(EventType e, Exception exception) where EventType : struct;
}

/// <summary>
///     事件元数据：用于配置分类和异常观察。
/// </summary>
public abstract class EventMetaData<EventType> : IEventMetaData where EventType : struct
{
    /// <summary>事件类别定义。</summary>
    public virtual EventCategoryToken Category => EventCategoryToken.Empty;

    public EventCategoryToken GetEventCategoryToken()
    {
        return Category;
    }

    /// <summary>事件处理异常时触发，可用于记录或观察异常。</summary>
    public virtual void OnEventExpectation<TValue>(TValue e, Exception exception) where TValue : struct
    {
    }
}