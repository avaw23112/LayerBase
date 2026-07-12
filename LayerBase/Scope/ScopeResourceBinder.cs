using System.Reflection;
using LayerBase.DI;

namespace LayerBase.Scope;

internal static class ScopeResourceBinder
{
    public static void Bind(ScopeRuntime runtime)
    {
        if (runtime == null) throw new ArgumentNullException(nameof(runtime));

        var candidates = runtime.Services.Cast<object>().Concat(runtime.Contexts).ToArray();
        var published = new Dictionary<(Type ProviderType, string LocalKey), PublishedResource>();
        var consumers = new List<(object Owner, FieldInfo Field, FromAttribute Attribute)>();

        foreach (object candidate in candidates)
        {
            Type type = candidate.GetType();
            foreach (FieldInfo field in GetFields(type))
            {
                PublishAttribute? publish = field.GetCustomAttribute<PublishAttribute>();
                FromAttribute? from = field.GetCustomAttribute<FromAttribute>();
                if (publish != null && from != null)
                {
                    throw new InvalidOperationException(
                        $"Scope resource member '{type.FullName}.{field.Name}' cannot declare both [Publish] and [From].");
                }

                if (publish != null)
                {
                    AddPublishedResource(candidate, field, publish, published);
                }

                if (from != null)
                {
                    consumers.Add((candidate, field, from));
                }
            }

            foreach (PropertyInfo property in GetProperties(type))
            {
                PublishAttribute? publish = property.GetCustomAttribute<PublishAttribute>();
                if (publish != null)
                {
                    AddPublishedResource(candidate, property, publish, published);
                }
            }
        }

        var ordered = published
            .OrderBy(static item => item.Key.ProviderType.FullName, StringComparer.Ordinal)
            .ThenBy(static item => item.Key.LocalKey, StringComparer.Ordinal)
            .Select(static item => new ScopeResourceEntry(item.Key.ProviderType, item.Key.LocalKey, item.Value.Value))
            .ToArray();
        int generation = runtime.Resources.Initialize(ordered);
        var slots = new Dictionary<(Type ProviderType, string LocalKey), int>();
        for (int i = 0; i < ordered.Length; i++)
        {
            slots[(ordered[i].ProviderType, ordered[i].LocalKey)] = i;
        }

        foreach ((object owner, FieldInfo field, FromAttribute attribute) in consumers)
        {
            BindConsumer(runtime, generation, slots, published, owner, field, attribute);
        }
    }

    private static void AddPublishedResource(
        object owner,
        FieldInfo field,
        PublishAttribute attribute,
        Dictionary<(Type ProviderType, string LocalKey), PublishedResource> published)
    {
        object? value = field.GetValue(owner);
        AddPublishedResource(owner, field.DeclaringType ?? owner.GetType(), field.Name, field.FieldType, value, attribute, published);
    }

    private static void AddPublishedResource(
        object owner,
        PropertyInfo property,
        PublishAttribute attribute,
        Dictionary<(Type ProviderType, string LocalKey), PublishedResource> published)
    {
        MethodInfo? getter = property.GetMethod;
        if (getter == null)
        {
            throw new InvalidOperationException(
                $"Scope resource provider '{owner.GetType().FullName}.{property.Name}' must expose a getter.");
        }

        object? value = property.GetValue(owner);
        AddPublishedResource(owner, property.DeclaringType ?? owner.GetType(), property.Name, property.PropertyType, value, attribute, published);
    }

    private static void AddPublishedResource(
        object owner,
        Type providerType,
        string memberName,
        Type resourceType,
        object? value,
        PublishAttribute attribute,
        Dictionary<(Type ProviderType, string LocalKey), PublishedResource> published)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                $"Scope resource provider '{owner.GetType().FullName}.{memberName}' for localKey '{attribute.LocalKey}' returned null.");
        }

        var key = (providerType, attribute.LocalKey);
        if (published.TryGetValue(key, out PublishedResource existing))
        {
            throw new InvalidOperationException(
                $"Scope resource provider conflict for providerType '{providerType.FullName}' and localKey '{attribute.LocalKey}'. " +
                $"Owners: {existing.Owner.GetType().FullName}.{existing.MemberName} and {owner.GetType().FullName}.{memberName}.");
        }

        published[key] = new PublishedResource(owner, memberName, resourceType, value);
    }

    private static void BindConsumer(
        ScopeRuntime runtime,
        int generation,
        Dictionary<(Type ProviderType, string LocalKey), int> slots,
        Dictionary<(Type ProviderType, string LocalKey), PublishedResource> published,
        object owner,
        FieldInfo field,
        FromAttribute attribute)
    {
        if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(ScopeRead<>))
        {
            throw new InvalidOperationException(
                $"Scope resource consumer '{owner.GetType().FullName}.{field.Name}' must use ScopeRead<TView>; direct resource access is not allowed.");
        }

        var key = (attribute.ProviderType, attribute.LocalKey);
        if (!slots.TryGetValue(key, out int slot) || !published.TryGetValue(key, out PublishedResource resource))
        {
            throw new InvalidOperationException(
                $"Scope resource consumer '{owner.GetType().FullName}.{field.Name}' could not find a published scope resource " +
                $"for providerType '{attribute.ProviderType.FullName}' and localKey '{attribute.LocalKey}'.");
        }

        Type viewType = field.FieldType.GetGenericArguments()[0];
        if (!viewType.IsInstanceOfType(resource.Value))
        {
            throw new InvalidOperationException(
                $"Scope resource consumer '{owner.GetType().FullName}.{field.Name}' cannot read provider '{attribute.ProviderType.FullName}.{attribute.LocalKey}' " +
                $"as '{viewType.FullName}'.");
        }

        ConstructorInfo? constructor = field.FieldType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ScopeRuntime), typeof(int), typeof(int)],
            modifiers: null);
        if (constructor == null)
        {
            throw new InvalidOperationException($"ScopeRead constructor for '{field.FieldType.FullName}' could not be resolved.");
        }

        object reader = constructor.Invoke([runtime, slot, generation]);
        field.SetValue(owner, reader);
    }

    private static IEnumerable<FieldInfo> GetFields(Type type)
    {
        for (Type? current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                yield return field;
            }
        }
    }

    private static IEnumerable<PropertyInfo> GetProperties(Type type)
    {
        for (Type? current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            foreach (PropertyInfo property in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                yield return property;
            }
        }
    }

    private readonly struct PublishedResource
    {
        public PublishedResource(object owner, string memberName, Type resourceType, object value)
        {
            Owner = owner;
            MemberName = memberName;
            ResourceType = resourceType;
            Value = value;
        }

        public object Owner { get; }

        public string MemberName { get; }

        public Type ResourceType { get; }

        public object Value { get; }
    }
}
