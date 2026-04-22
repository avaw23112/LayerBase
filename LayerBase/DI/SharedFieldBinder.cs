using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using LayerBase.Layers;

namespace LayerBase.DI;

internal static class SharedFieldBinder
{
    private static readonly ConcurrentDictionary<Type, FieldBindingMetadata[]> MetadataCache = new();

    internal readonly struct Participant
    {
        public Participant(object instance, Layer layer, int serviceScopeId)
        {
            Instance = instance;
            Layer = layer;
            ServiceScopeId = serviceScopeId;
        }

        public object Instance { get; }
        public Layer Layer { get; }
        public int ServiceScopeId { get; }
    }

    private readonly struct PublishedField
    {
        public PublishedField(PublicType scope, Layer layer, int serviceScopeId, string key, object owner, FieldInfo field, object value)
        {
            Scope = scope;
            Layer = layer;
            ServiceScopeId = serviceScopeId;
            Key = key;
            Owner = owner;
            Field = field;
            Value = value;
        }

        public PublicType Scope { get; }
        public Layer Layer { get; }
        public int ServiceScopeId { get; }
        public string Key { get; }
        public object Owner { get; }
        public FieldInfo Field { get; }
        public object Value { get; }
    }

    public static void Bind(IEnumerable<Participant> participants)
    {
        if (participants == null) throw new ArgumentNullException(nameof(participants));

        var participantList = participants.ToList();
        if (participantList.Count == 0) return;

        var published = new Dictionary<(PublicType Scope, int LayerId, int ServiceScopeId, string Key), PublishedField>();
        var pendingConsumers = new List<(Participant Participant, FieldInfo Field, FromAttribute Attribute)>();

        foreach (var participant in participantList)
        {
            var metadata = GetMetadata(participant.Instance.GetType());
            foreach (var item in metadata)
            {
                if (item.PublicAttribute != null && item.FromAttribute != null)
                {
                    throw new InvalidOperationException(
                        $"Shared field '{participant.Instance.GetType().FullName}.{item.Field.Name}' cannot declare both [Public] and [From].");
                }

                if (item.PublicAttribute != null)
                {
                    var value = ResolvePublishedValue(participant.Instance, item.Field, item.PublicAttribute);
                    var key = CreateScopeKey(item.PublicAttribute.Scope, participant.Layer, participant.ServiceScopeId,
                        item.PublicAttribute.Key);

                    if (published.TryGetValue(key, out var existing))
                    {
                        throw new InvalidOperationException(
                            $"Shared field publisher conflict for scope '{item.PublicAttribute.Scope}' and key '{item.PublicAttribute.Key}'. " +
                            $"Owners: {existing.Owner.GetType().FullName}.{existing.Field.Name} and {participant.Instance.GetType().FullName}.{item.Field.Name}.");
                    }

                    published[key] = new PublishedField(
                        item.PublicAttribute.Scope,
                        participant.Layer,
                        participant.ServiceScopeId,
                        item.PublicAttribute.Key,
                        participant.Instance,
                        item.Field,
                        value);
                }

                if (item.FromAttribute != null)
                    pendingConsumers.Add((participant, item.Field, item.FromAttribute));
            }
        }

        foreach (var consumer in pendingConsumers)
        {
            var key = CreateScopeKey(consumer.Attribute.Scope, consumer.Participant.Layer, consumer.Participant.ServiceScopeId,
                consumer.Attribute.Key);

            if (!published.TryGetValue(key, out var publisher))
            {
                throw new InvalidOperationException(
                    $"Shared field consumer '{consumer.Participant.Instance.GetType().FullName}.{consumer.Field.Name}' could not find " +
                    $"a publisher for scope '{consumer.Attribute.Scope}' and key '{consumer.Attribute.Key}'.");
            }

            if (!TryAdaptValue(publisher.Value, consumer.Field.FieldType, out var adaptedValue))
            {
                throw new InvalidOperationException(
                    $"Shared field consumer '{consumer.Participant.Instance.GetType().FullName}.{consumer.Field.Name}' " +
                    $"of type '{consumer.Field.FieldType.FullName}' cannot consume publisher '{publisher.Owner.GetType().FullName}.{publisher.Field.Name}' " +
                    $"of type '{publisher.Field.FieldType.FullName}'.");
            }

            consumer.Field.SetValue(consumer.Participant.Instance, adaptedValue);
        }
    }

    private static object ResolvePublishedValue(object owner, FieldInfo field, PublicAttribute attribute)
    {
        var value = field.GetValue(owner);
        if (value != null) return value;

        var fieldType = field.FieldType;
        if (fieldType.IsValueType)
        {
            value = Activator.CreateInstance(fieldType)!;
            field.SetValue(owner, value);
            return value;
        }

        var ctor = fieldType.GetConstructor(Type.EmptyTypes);
        if (ctor == null || !ctor.IsPublic)
        {
            throw new InvalidOperationException(
                $"Shared field publisher '{owner.GetType().FullName}.{field.Name}' for scope '{attribute.Scope}' and key '{attribute.Key}' " +
                "must be initialized inline or expose a public parameterless constructor.");
        }

        value = ctor.Invoke(null)!;
        field.SetValue(owner, value);
        return value;
    }

    private static bool TryAdaptValue(object publishedValue, Type targetType, out object? adaptedValue)
    {
        if (IsWritableContainerExposure(targetType, publishedValue.GetType()))
        {
            adaptedValue = null;
            return false;
        }

        if (targetType.IsInstanceOfType(publishedValue))
        {
            adaptedValue = publishedValue;
            return true;
        }

        adaptedValue = null;
        return false;
    }

    private static bool IsWritableContainerExposure(Type targetType, Type publishedType)
    {
        if (targetType.IsGenericType)
        {
            var targetDef = targetType.GetGenericTypeDefinition();
            if (targetDef == typeof(ICollection<>) ||
                targetDef == typeof(IList<>) ||
                targetDef == typeof(IDictionary<,>) ||
                targetDef == typeof(ISet<>) ||
                targetDef == typeof(IProducerConsumerCollection<>))
            {
                return true;
            }
        }

        if (targetType != publishedType) return false;

        if (!targetType.IsGenericType) return typeof(ICollection).IsAssignableFrom(targetType);

        var publishedDef = targetType.GetGenericTypeDefinition();
        return publishedDef == typeof(List<>) ||
               publishedDef == typeof(Dictionary<,>) ||
               publishedDef == typeof(Queue<>) ||
               publishedDef == typeof(Stack<>) ||
               publishedDef == typeof(HashSet<>) ||
               publishedDef == typeof(LinkedList<>) ||
               publishedDef == typeof(ConcurrentDictionary<,>) ||
               publishedDef == typeof(ConcurrentQueue<>) ||
               publishedDef == typeof(ConcurrentStack<>) ||
               publishedDef == typeof(ConcurrentBag<>);
    }

    private static (PublicType Scope, int LayerId, int ServiceScopeId, string Key) CreateScopeKey(
        PublicType scope,
        Layer layer,
        int serviceScopeId,
        string key)
    {
        if (scope == PublicType.Service && serviceScopeId <= 0)
        {
            throw new InvalidOperationException(
                $"Shared field key '{key}' uses Service scope outside of a service registration boundary.");
        }

        return scope switch
        {
            PublicType.Global => (scope, -1, -1, key),
            PublicType.Layer => (scope, layer.RouteIndex, -1, key),
            PublicType.Service => (scope, layer.RouteIndex, serviceScopeId, key),
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
        };
    }

    private static FieldBindingMetadata[] GetMetadata(Type type)
    {
        return MetadataCache.GetOrAdd(type, static currentType =>
            currentType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(static field => new FieldBindingMetadata(
                    field,
                    field.GetCustomAttribute<PublicAttribute>(),
                    field.GetCustomAttribute<FromAttribute>()))
                .Where(static item => item.PublicAttribute != null || item.FromAttribute != null)
                .ToArray());
    }

    private readonly struct FieldBindingMetadata
    {
        public FieldBindingMetadata(FieldInfo field, PublicAttribute? publicAttribute, FromAttribute? fromAttribute)
        {
            Field = field;
            PublicAttribute = publicAttribute;
            FromAttribute = fromAttribute;
        }

        public FieldInfo Field { get; }
        public PublicAttribute? PublicAttribute { get; }
        public FromAttribute? FromAttribute { get; }
    }
}
