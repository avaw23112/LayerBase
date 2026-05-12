using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using LayerBase.Layers;

namespace LayerBase.DI;

internal static class SharedFieldBinder
{
    private static readonly ConcurrentDictionary<Type, FieldBindingMetadata[]> MetadataCache = new();

    public static void Bind(IEnumerable<Participant> participants)
    {
        if (participants == null) throw new ArgumentNullException(nameof(participants));

        var participantList = participants.ToList();
        if (participantList.Count == 0) return;

        var published = new Dictionary<(Type OwnerType, string LocalKey), PublishedField>();
        var pendingConsumers = new List<(Participant Participant, FieldInfo Field, FromAttribute Attribute)>();

        foreach (var participant in participantList)
        {
            var metadata = GetMetadata(participant.Instance.GetType());
            foreach (var item in metadata)
            {
                if (item.ProvideAttribute != null && item.FromAttribute != null)
                    throw new InvalidOperationException(
                        $"Shared field '{participant.Instance.GetType().FullName}.{item.Field.Name}' cannot declare both [Provide] and [From].");

                if (item.ProvideAttribute != null)
                {
                    var value = ResolvePublishedValue(participant.Instance, item.Field, item.ProvideAttribute);
                    var key = (item.ProvideAttribute.OwnerType, item.ProvideAttribute.LocalKey);

                    if (published.TryGetValue(key, out var existing))
                        throw new InvalidOperationException(
                            $"Shared field provider conflict for ownerType '{item.ProvideAttribute.OwnerType.FullName}' and localKey '{item.ProvideAttribute.LocalKey}'. " +
                            $"Owners: {existing.Owner.GetType().FullName}.{existing.Field.Name} and {participant.Instance.GetType().FullName}.{item.Field.Name}.");

                    published[key] = new PublishedField(
                        item.ProvideAttribute.OwnerType,
                        participant.Layer,
                        participant.ServiceScopeId,
                        item.ProvideAttribute.LocalKey,
                        participant.Instance,
                        item.Field,
                        value);

                    participant.Layer.RecordSharedField(item.ProvideAttribute.OwnerType, item.ProvideAttribute.LocalKey,
                        item.Field.FieldType, true);
                }

                if (item.FromAttribute != null)
                {
                    pendingConsumers.Add((participant, item.Field, item.FromAttribute));
                    participant.Layer.RecordSharedField(item.FromAttribute.OwnerType, item.FromAttribute.LocalKey,
                        item.Field.FieldType, false);
                }
            }
        }

        foreach (var consumer in pendingConsumers)
        {
            var key = (consumer.Attribute.OwnerType, consumer.Attribute.LocalKey);

            if (!published.TryGetValue(key, out var publisher))
                throw new InvalidOperationException(
                    $"Shared field consumer '{consumer.Participant.Instance.GetType().FullName}.{consumer.Field.Name}' could not find " +
                    $"a provider for ownerType '{consumer.Attribute.OwnerType.FullName}' and localKey '{consumer.Attribute.LocalKey}'.");

            if (!TryAdaptValue(publisher.Value, consumer.Field.FieldType, out var adaptedValue))
                throw new InvalidOperationException(
                    $"Shared field consumer '{consumer.Participant.Instance.GetType().FullName}.{consumer.Field.Name}' " +
                    $"of type '{consumer.Field.FieldType.FullName}' cannot consume provider '{publisher.Owner.GetType().FullName}.{publisher.Field.Name}' " +
                    $"of type '{publisher.Field.FieldType.FullName}'. Only read-only projections are allowed.");

            consumer.Field.SetValue(consumer.Participant.Instance, adaptedValue);
        }
    }

    private static object ResolvePublishedValue(object owner, FieldInfo field, ProvideAttribute attribute)
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
            throw new InvalidOperationException(
                $"Shared field provider '{owner.GetType().FullName}.{field.Name}' for ownerType '{attribute.OwnerType.FullName}' and localKey '{attribute.LocalKey}' " +
                "must be initialized inline or expose a public parameterless constructor.");

        value = ctor.Invoke(null)!;
        field.SetValue(owner, value);
        return value;
    }

    private static bool TryAdaptValue(object publishedValue, Type targetType, out object? adaptedValue)
    {
        if (IsWritableContainerExposure(targetType))
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

    private static bool IsWritableContainerExposure(Type targetType)
    {
        if (targetType.IsGenericType)
        {
            var targetDef = targetType.GetGenericTypeDefinition();
            if (targetDef == typeof(ICollection<>) ||
                targetDef == typeof(IList<>) ||
                targetDef == typeof(IDictionary<,>) ||
                targetDef == typeof(ISet<>) ||
                targetDef == typeof(List<>) ||
                targetDef == typeof(Dictionary<,>) ||
                targetDef == typeof(Queue<>) ||
                targetDef == typeof(Stack<>) ||
                targetDef == typeof(HashSet<>) ||
                targetDef == typeof(LinkedList<>) ||
                targetDef == typeof(ConcurrentDictionary<,>) ||
                targetDef == typeof(ConcurrentQueue<>) ||
                targetDef == typeof(ConcurrentStack<>) ||
                targetDef == typeof(ConcurrentBag<>))
                return true;
        }

        if (targetType == typeof(ICollection) || targetType == typeof(IList) || targetType == typeof(IDictionary))
            return true;

        return false;
    }

    private static FieldBindingMetadata[] GetMetadata(Type type)
    {
        return MetadataCache.GetOrAdd(type, static currentType =>
            currentType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(static field => new FieldBindingMetadata(
                    field,
                    field.GetCustomAttribute<ProvideAttribute>(),
                    field.GetCustomAttribute<FromAttribute>()))
                .Where(static item => item.ProvideAttribute != null || item.FromAttribute != null)
                .ToArray());
    }

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
        public PublishedField(Type      ownerType, Layer  layer, int serviceScopeId, string localKey, object owner,
                              FieldInfo field,     object value)
        {
            OwnerType = ownerType;
            Layer = layer;
            ServiceScopeId = serviceScopeId;
            LocalKey = localKey;
            Owner = owner;
            Field = field;
            Value = value;
        }

        public Type OwnerType { get; }
        public Layer Layer { get; }
        public int ServiceScopeId { get; }
        public string LocalKey { get; }
        public object Owner { get; }
        public FieldInfo Field { get; }
        public object Value { get; }
    }

    private readonly struct FieldBindingMetadata
    {
        public FieldBindingMetadata(FieldInfo field, ProvideAttribute? provideAttribute, FromAttribute? fromAttribute)
        {
            Field = field;
            ProvideAttribute = provideAttribute;
            FromAttribute = fromAttribute;
        }

        public FieldInfo Field { get; }
        public ProvideAttribute? ProvideAttribute { get; }
        public FromAttribute? FromAttribute { get; }
    }
}