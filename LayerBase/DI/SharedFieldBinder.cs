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

        var published = new Dictionary<ProvideBindingKey, PublishedField>();
        var publishedByProviderAndKey = new Dictionary<(Type ProviderServiceType, string LocalKey), List<PublishedField>>();
        var serviceTypesByLayerScope = new HashSet<(int LayerIndex, int ScopeId, Type ProviderServiceType)>();
        var pendingConsumers = new List<(Participant Participant, FieldInfo Field, FromAttribute Attribute)>();

        foreach (var participant in participantList)
            serviceTypesByLayerScope.Add((
                participant.Layer.RouteIndex,
                participant.OwnerScopeId,
                participant.ProviderServiceType));

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
                    var key = new ProvideBindingKey(
                        participant.Layer.RouteIndex,
                        participant.OwnerScopeId,
                        participant.ProviderServiceType,
                        item.ProvideAttribute.LocalKey);

                    if (published.TryGetValue(key, out var existing))
                        throw new InvalidOperationException(
                            $"Shared field provider conflict for providerServiceType '{participant.ProviderServiceType.FullName}' and localKey '{item.ProvideAttribute.LocalKey}'. " +
                            $"Owners: {existing.Owner.GetType().FullName}.{existing.Field.Name} and {participant.Instance.GetType().FullName}.{item.Field.Name}.");

                    var field = new PublishedField(
                        participant.ProviderServiceType,
                        participant.Layer,
                        participant.OwnerScopeId,
                        participant.ServiceScopeId,
                        item.ProvideAttribute.LocalKey,
                        participant.Instance,
                        item.Field,
                        value);

                    published[key] = field;
                    var providerKey = (participant.ProviderServiceType, item.ProvideAttribute.LocalKey);
                    if (!publishedByProviderAndKey.TryGetValue(providerKey, out var providerFields))
                    {
                        providerFields = new List<PublishedField>();
                        publishedByProviderAndKey[providerKey] = providerFields;
                    }

                    providerFields.Add(field);

                    participant.Layer.RecordSharedField(participant.ProviderServiceType, item.ProvideAttribute.LocalKey,
                        item.Field.FieldType, true);
                }

                if (item.FromAttribute != null)
                {
                    pendingConsumers.Add((participant, item.Field, item.FromAttribute));
                    participant.Layer.RecordSharedField(item.FromAttribute.ProviderServiceType, item.FromAttribute.LocalKey,
                        item.Field.FieldType, false);
                }
            }
        }

        foreach (var consumer in pendingConsumers)
        {
            var key = new ProvideBindingKey(
                consumer.Participant.Layer.RouteIndex,
                consumer.Participant.OwnerScopeId,
                consumer.Attribute.ProviderServiceType,
                consumer.Attribute.LocalKey);

            if (!published.TryGetValue(key, out var publisher))
                ThrowProviderNotFound(
                    consumer,
                    publishedByProviderAndKey,
                    serviceTypesByLayerScope);

            if (!TryAdaptValue(publisher.Value, consumer.Field.FieldType, out var adaptedValue))
                throw new InvalidOperationException(
                    $"Shared field consumer '{consumer.Participant.Instance.GetType().FullName}.{consumer.Field.Name}' " +
                    $"of type '{consumer.Field.FieldType.FullName}' cannot consume provider '{publisher.Owner.GetType().FullName}.{publisher.Field.Name}' " +
                    $"of type '{publisher.Field.FieldType.FullName}'. Only read-only projections are allowed.");

            consumer.Field.SetValue(consumer.Participant.Instance, adaptedValue);
        }
    }

    private static void ThrowProviderNotFound(
        (Participant Participant, FieldInfo Field, FromAttribute Attribute) consumer,
        Dictionary<(Type ProviderServiceType, string LocalKey), List<PublishedField>> publishedByProviderAndKey,
        HashSet<(int LayerIndex, int ScopeId, Type ProviderServiceType)> serviceTypesByLayerScope)
    {
        if (publishedByProviderAndKey.TryGetValue(
                (consumer.Attribute.ProviderServiceType, consumer.Attribute.LocalKey),
                out var candidates))
        {
            if (candidates.Any(candidate => candidate.Layer.RouteIndex != consumer.Participant.Layer.RouteIndex))
                throw new InvalidOperationException(
                    "Cross-layer From is not allowed. Use this.Call<TRequest,TResponse>().");

            if (candidates.Any(candidate => candidate.OwnerScopeId != consumer.Participant.OwnerScopeId))
                throw new InvalidOperationException(
                    "Cross-scope From is not allowed. Use ScopeEvent or ScopeCall.");
        }

        if (!serviceTypesByLayerScope.Contains((
                consumer.Participant.Layer.RouteIndex,
                consumer.Participant.OwnerScopeId,
                consumer.Attribute.ProviderServiceType)))
        {
            throw new InvalidOperationException(
                $"Provider service '{consumer.Attribute.ProviderServiceType.FullName}' is not registered in the current Layer provider.");
        }

        throw new InvalidOperationException(
            $"Shared field consumer '{consumer.Participant.Instance.GetType().FullName}.{consumer.Field.Name}' could not find " +
            $"a provider for providerServiceType '{consumer.Attribute.ProviderServiceType.FullName}' and localKey '{consumer.Attribute.LocalKey}'.");
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
                $"Shared field provider '{owner.GetType().FullName}.{field.Name}' for localKey '{attribute.LocalKey}' " +
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
        public Participant(
            object instance,
            Layer layer,
            int ownerScopeId,
            int serviceScopeId,
            Type providerServiceType)
        {
            Instance = instance;
            Layer = layer;
            OwnerScopeId = ownerScopeId;
            ServiceScopeId = serviceScopeId;
            ProviderServiceType = providerServiceType ?? throw new ArgumentNullException(nameof(providerServiceType));
        }

        public object Instance { get; }
        public Layer Layer { get; }
        public int OwnerScopeId { get; }
        public int ServiceScopeId { get; }
        public Type ProviderServiceType { get; }
    }

    private readonly struct PublishedField
    {
        public PublishedField(
            Type providerServiceType,
            Layer layer,
            int ownerScopeId,
            int serviceScopeId,
            string localKey,
            object owner,
            FieldInfo field,
            object value)
        {
            ProviderServiceType = providerServiceType;
            Layer = layer;
            OwnerScopeId = ownerScopeId;
            ServiceScopeId = serviceScopeId;
            LocalKey = localKey;
            Owner = owner;
            Field = field;
            Value = value;
        }

        public Type ProviderServiceType { get; }
        public Layer Layer { get; }
        public int OwnerScopeId { get; }
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

    private readonly struct ProvideBindingKey : IEquatable<ProvideBindingKey>
    {
        public ProvideBindingKey(int layerIndex, int scopeId, Type providerServiceType, string localKey)
        {
            LayerIndex = layerIndex;
            ScopeId = scopeId;
            ProviderServiceType = providerServiceType ?? throw new ArgumentNullException(nameof(providerServiceType));
            LocalKey = localKey ?? throw new ArgumentNullException(nameof(localKey));
        }

        public int LayerIndex { get; }
        public int ScopeId { get; }
        public Type ProviderServiceType { get; }
        public string LocalKey { get; }

        public bool Equals(ProvideBindingKey other)
        {
            return LayerIndex == other.LayerIndex &&
                   ScopeId == other.ScopeId &&
                   ProviderServiceType == other.ProviderServiceType &&
                   LocalKey == other.LocalKey;
        }

        public override bool Equals(object? obj)
        {
            return obj is ProvideBindingKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LayerIndex, ScopeId, ProviderServiceType, LocalKey);
        }
    }
}
