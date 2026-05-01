using System.Collections.Concurrent;
using LayerBase.Core.EventCatalogue;

namespace LayerBase.Event.EventMetaData;

internal static class EventMetaDataHandler
{
    private static Dictionary<Type, IEventMetaData> s_metaDataByType = new();
    private static readonly object s_lock = new();
    private static int s_registryVersion;
    private static readonly ConcurrentQueue<IEventExpectation> s_pendingExpectations = new();

    internal static void Clear()
    {
        lock (s_lock)
        {
            Volatile.Write(ref s_metaDataByType, new Dictionary<Type, IEventMetaData>());
            Interlocked.Increment(ref s_registryVersion);
        }

        while (s_pendingExpectations.TryDequeue(out _))
        {
        }
    }

    internal static void PumpExpectations()
    {
        while (s_pendingExpectations.TryDequeue(out var expectation))
            try
            {
                expectation.Invoke();
            }
            catch
            {
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

    public static void OnEventExpectation<EventType>(EventType e, Exception exception) where EventType : struct
    {
        var metaData = ResolveMetaData<EventType>();
        if (metaData == null) return;

        s_pendingExpectations.Enqueue(new EventExpectation<EventType>(metaData, in e, exception));
    }

    internal static IEnumerable<(Type Type, IEventMetaData MetaData)> GetAllMetaData()
    {
        var byType = Volatile.Read(ref s_metaDataByType);
        foreach (var kvp in byType)
        {
            yield return (kvp.Key, kvp.Value);
        }
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
}