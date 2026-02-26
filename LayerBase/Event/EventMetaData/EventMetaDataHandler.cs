using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LayerBase.Core;
using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;

/// <summary>
/// 事件元数据调度：注册、查询以及生命周期回调。
/// </summary>
internal static class EventMetaDataHandler
{
    private static Dictionary<Type, IEventMetaData> s_metaDataByType = new();
    private static Dictionary<EventCategoryToken, IEventMetaData[]> s_metaDataByCategory = new();
    private static readonly object s_lock = new();
    private static int s_registryVersion;
    private static readonly ConcurrentQueue<IEventExpectation> s_pendingExpectations = new();

    private interface IEventExpectation
    {
        void Invoke();
    }

    private readonly struct EventExpectation<TEvent> : IEventExpectation where TEvent : struct
    {
        private readonly IEventMetaData _metaData;
        private readonly TEvent _eventValue;
        private readonly Exception _exception;

        public EventExpectation(IEventMetaData metaData, in TEvent eventValue, Exception exception)
        {
            _metaData = metaData;
            _eventValue = eventValue;
            _exception = exception;
        }

        public void Invoke()
        {
            _metaData.OnEventExpectation(_eventValue, _exception);
        }
    }

    private static class MetaDataCache<TEvent> where TEvent : struct
    {
        public static int Version = -1;
        public static IEventMetaData? MetaData;
    }

    internal static void Clear()
    {
        lock (s_lock)
        {
            Volatile.Write(ref s_metaDataByType, new Dictionary<Type, IEventMetaData>());
            Volatile.Write(ref s_metaDataByCategory, new Dictionary<EventCategoryToken, IEventMetaData[]>());
            Interlocked.Increment(ref s_registryVersion);
        }

        while (s_pendingExpectations.TryDequeue(out _))
        {
        }
    }

    internal static void OnClassicEventCreated(ref EventCategoryToken category, ref Core.EventStateTrace.EventState state)
    {
        var index = Volatile.Read(ref s_metaDataByCategory);
        if (!index.TryGetValue(category, out var metaDataList))
        {
            return;
        }

        for (int i = 0; i < metaDataList.Length; i++)
        {
            metaDataList[i].OnEventCreated(ref state);
        }
    }

    internal static void OnClassicEventDestroyed(ref EventCategoryToken category, ref Core.EventStateTrace.EventState state)
    {
        var index = Volatile.Read(ref s_metaDataByCategory);
        if (!index.TryGetValue(category, out var metaDataList))
        {
            return;
        }

        for (int i = 0; i < metaDataList.Length; i++)
        {
            metaDataList[i].OnEventDestroyed(ref state);
        }
    }

    internal static void PumpExpectations()
    {
        while (s_pendingExpectations.TryDequeue(out var expectation))
        {
            try
            {
                expectation.Invoke();
            }
            catch
            {
                // Exception observers should not break the layer pump loop.
            }
        }
    }

    public static void RegisterMetaData<EventType>(IEventMetaData metaData)
    {
        if (metaData == null) throw new ArgumentNullException(nameof(metaData));

        lock (s_lock)
        {
            var byType = new Dictionary<Type, IEventMetaData>(Volatile.Read(ref s_metaDataByType))
            {
                [typeof(EventType)] = metaData
            };

            Volatile.Write(ref s_metaDataByType, byType);
            Volatile.Write(ref s_metaDataByCategory, BuildCategoryIndex(byType));
            Interlocked.Increment(ref s_registryVersion);
        }
    }

    public static EventCategoryToken Category<EventType>() where EventType : struct
    {
        return ResolveMetaData<EventType>()?.GetEventCategoryToken() ?? EventCategoryToken.Empty;
    }

    public static EventCategoryToken Category(Type eventType)
    {
        var byType = Volatile.Read(ref s_metaDataByType);
        return byType.TryGetValue(eventType, out var metaData)
            ? metaData.GetEventCategoryToken()
            : EventCategoryToken.Empty;
    }

    public static int MaxBufferSize<EventType>() where EventType : struct
    {
        return ResolveMetaData<EventType>()?.MaxBufferSize ?? 0;
    }

    public static int MaxBufferSize(Type eventType)
    {
        var byType = Volatile.Read(ref s_metaDataByType);
        return byType.TryGetValue(eventType, out var metaData)
            ? metaData.MaxBufferSize
            : 0;
    }

    public static int EventHandlerJitter<EventType>() where EventType : struct
    {
        return ResolveMetaData<EventType>()?.EventHandlerJitter ?? 0;
    }

    public static int EventHandlerJitter(Type eventType)
    {
        var byType = Volatile.Read(ref s_metaDataByType);
        return byType.TryGetValue(eventType, out var metaData)
            ? metaData.EventHandlerJitter
            : 0;
    }

    public static EventQueueOverflowStrategy EventQueueOverflowStrategy<EventType>() where EventType : struct
    {
        return ResolveMetaData<EventType>()?.EventQueueOverflowStrategy ?? Core.EventQueueOverflowStrategy.OverWrite;
    }

    public static EventQueueOverflowStrategy EventQueueOverflowStrategy(Type eventType)
    {
        var byType = Volatile.Read(ref s_metaDataByType);
        return byType.TryGetValue(eventType, out var metaData)
            ? metaData.EventQueueOverflowStrategy
            : Core.EventQueueOverflowStrategy.OverWrite;
    }

    public static void OnEventExpectation<EventType>(EventType e, Exception exception) where EventType : struct
    {
        var metaData = ResolveMetaData<EventType>();
        if (metaData == null)
        {
            return;
        }

        s_pendingExpectations.Enqueue(new EventExpectation<EventType>(metaData, in e, exception));
    }

    public static void EventMergeStrategy<EventType>(PooledChunkedOverwriteQueue<Core.Event.Event<EventType>> queue)
        where EventType : struct
    {
        var metaData = ResolveMetaData<EventType>();
        if (metaData != null)
        {
            metaData.EventMergeStrategy(queue);
        }
    }

    private static IEventMetaData? ResolveMetaData<EventType>() where EventType : struct
    {
        int version = Volatile.Read(ref s_registryVersion);
        if (MetaDataCache<EventType>.Version == version)
        {
            return MetaDataCache<EventType>.MetaData;
        }

        var byType = Volatile.Read(ref s_metaDataByType);
        byType.TryGetValue(typeof(EventType), out var metaData);
        MetaDataCache<EventType>.MetaData = metaData;
        MetaDataCache<EventType>.Version = version;
        return metaData;
    }

    private static Dictionary<EventCategoryToken, IEventMetaData[]> BuildCategoryIndex(
        Dictionary<Type, IEventMetaData> byType)
    {
        var grouped = new Dictionary<EventCategoryToken, List<IEventMetaData>>();
        foreach (var metaData in byType.Values)
        {
            var category = metaData.GetEventCategoryToken();
            if (category.IsEmpty)
            {
                continue;
            }

            if (!grouped.TryGetValue(category, out var list))
            {
                list = new List<IEventMetaData>(2);
                grouped.Add(category, list);
            }

            list.Add(metaData);
        }

        var index = new Dictionary<EventCategoryToken, IEventMetaData[]>(grouped.Count);
        foreach (var pair in grouped)
        {
            index.Add(pair.Key, pair.Value.ToArray());
        }

        return index;
    }
}
