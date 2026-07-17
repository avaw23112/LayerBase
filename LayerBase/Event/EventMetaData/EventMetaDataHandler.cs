using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;

internal static class EventMetaDataHandler
{
    private static Dictionary<Type, IEventMetaData> s_metaDataByType = new();
    private static readonly object s_lock = new();
    private static int s_registryVersion;

    static EventMetaDataHandler()
    {
        LayerHub.RegisterCacheResetter(Clear);
    }

    internal static void Clear()
    {
        lock (s_lock)
        {
            Volatile.Write(ref s_metaDataByType, new Dictionary<Type, IEventMetaData>());
            Interlocked.Increment(ref s_registryVersion);
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

    public static Actor.ActorMailOptions? GetActorMailOptions<TEvent>() where TEvent : struct
    {
        return ResolveMetaData<TEvent>()?.GetActorMailOptions();
    }

    internal static bool TryMergePostEvent<TEvent>(
        in  TEvent oldValue,
        in  TEvent newValue,
        out TEvent mergedValue)
        where TEvent : struct
    {
        if (ResolveMetaData<TEvent>() is EventMetaData<TEvent> metaData)
        {
            mergedValue = oldValue;
            if (metaData.TryMergePostEvent(ref mergedValue, in newValue))
            {
                return true;
            }
        }

        mergedValue = default;
        return false;
    }

    internal static IEnumerable<(Type Type, IEventMetaData MetaData)> GetAllMetaData()
    {
        var byType = Volatile.Read(ref s_metaDataByType);
        foreach (var kvp in byType)
        {
            yield return (kvp.Key, kvp.Value);
        }
    }

    internal static EventMetaData<TEvent>? ResolveRegisteredMetaData<TEvent>()
        where TEvent : struct
    {
        return ResolveMetaData<TEvent>() as EventMetaData<TEvent>;
    }

    private static IEventMetaData? ResolveMetaData<EventType>() where EventType : struct
    {
        var version = Volatile.Read(ref s_registryVersion);
        if (MetaDataCache<EventType>.Version == version) return MetaDataCache<EventType>.MetaData;

        var byType = Volatile.Read(ref s_metaDataByType);
        byType.TryGetValue(typeof(EventType), out var metaData);
        MetaDataCache<EventType>.MetaData = metaData;
        MetaDataCache<EventType>.Version = version;
        return metaData;
    }

    private static class MetaDataCache<TEvent> where TEvent : struct
    {
        public static int Version = -1;
        public static IEventMetaData? MetaData;
    }
}