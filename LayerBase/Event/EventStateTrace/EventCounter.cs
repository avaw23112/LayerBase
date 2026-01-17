using System;
using System.Collections.Generic;
using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Core.EventStateTrace;

internal struct EventGlobalInfo
{
    public int EventTypeId;
    public EventCategoryToken EventCategoryToken;
    public int EventCount;
}

/// <summary>
/// 按分类统计活跃事件数量，用于触发分类创建/销毁回调。
/// </summary>
internal sealed class EventCounter
{
    private readonly Dictionary<EventCategoryToken, EventGlobalInfo> _infoByCategory = new();

    public int Increment<T>() where T : struct
    {
        var category = EventMetaDataHandler.Category<T>();
        if (category.IsEmpty)
        {
            return -1;
        }

        if (!_infoByCategory.TryGetValue(category, out var info))
        {
            info = new EventGlobalInfo
            {
                EventTypeId = EventTypeId<T>.Id,
                EventCategoryToken = category,
                EventCount = 0,
            };
        }

        info.EventCount += 1;
        _infoByCategory[category] = info;
        return info.EventCount;
    }

    public int Decrement(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        var category = EventMetaDataHandler.Category(type);
        if (category.IsEmpty)
        {
            return -1;
        }

        if (!_infoByCategory.TryGetValue(category, out var info) || info.EventCount <= 0)
        {
            throw new InvalidOperationException("事件计数异常，无法完成减计数。");
        }

        info.EventCount -= 1;
        _infoByCategory[category] = info;
        return info.EventCount;
    }
}
