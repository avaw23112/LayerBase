using System.Reflection;
using LayerBase.DI;
using LayerBase.Scope.Resources;

namespace LayerBase.Scope;

internal static class ScopeResourceBinder
{
    public static void Bind(ScopeRuntime runtime, bool skipGeneratedResources = false)
    {
        if (runtime == null) throw new ArgumentNullException(nameof(runtime));

        var candidates = runtime.Services.Cast<object>().Concat(runtime.Contexts).ToArray();
        var published = new Dictionary<(Type ProviderType, string LocalKey), PublishedResource>();
        var consumers = new List<(object Owner, FieldInfo Field, FromAttribute Attribute)>();

        foreach (object candidate in candidates)
        {
            if (skipGeneratedResources &&
                (candidate is IGeneratedScopeResourcePublisher ||
                 candidate is IGeneratedScopeResourceConsumer))
            {
                continue;
            }

            Type type = candidate.GetType();
            foreach (FieldInfo field in GetFields(type))
            {
                ProvideAttribute? provide = field.GetCustomAttribute<ProvideAttribute>();
                FromAttribute? from = field.GetCustomAttribute<FromAttribute>();
                if (provide != null && from != null)
                {
                    throw new InvalidOperationException(
                        $"Scope resource member '{type.FullName}.{field.Name}' cannot declare both [Provide] and [From].");
                }

                if (provide != null)
                {
                    AddPublishedResource(candidate, field, provide, published);
                }

                if (from != null)
                {
                    consumers.Add((candidate, field, from));
                }
            }

            foreach (PropertyInfo property in GetProperties(type))
            {
                ProvideAttribute? provide = property.GetCustomAttribute<ProvideAttribute>();
                if (provide != null)
                {
                    AddPublishedResource(candidate, property, provide, published);
                }
            }
        }

        foreach ((object owner, FieldInfo field, FromAttribute attribute) in consumers)
        {
            BindConsumer(runtime, published, owner, field, attribute);
        }
    }

    private static void AddPublishedResource(
        object owner,
        FieldInfo field,
        ProvideAttribute attribute,
        Dictionary<(Type ProviderType, string LocalKey), PublishedResource> published)
    {
        object? value = field.GetValue(owner);
        AddPublishedResource(owner, field.DeclaringType ?? owner.GetType(), field.Name, field.FieldType, value, attribute, published);
    }

    private static void AddPublishedResource(
        object owner,
        PropertyInfo property,
        ProvideAttribute attribute,
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
        ProvideAttribute attribute,
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
        Dictionary<(Type ProviderType, string LocalKey), PublishedResource> published,
        object owner,
        FieldInfo field,
        FromAttribute attribute)
    {
        var key = (attribute.ProviderType, attribute.LocalKey);
        if (!published.TryGetValue(key, out PublishedResource resource))
        {
            throw new InvalidOperationException(
                $"Scope resource consumer '{owner.GetType().FullName}.{field.Name}' could not find a published scope resource " +
                $"for providerType '{attribute.ProviderType.FullName}' and localKey '{attribute.LocalKey}'.");
        }

        if (!field.FieldType.IsInstanceOfType(resource.Value))
        {
            throw new InvalidOperationException(
                $"Scope resource consumer '{owner.GetType().FullName}.{field.Name}' cannot read provider '{attribute.ProviderType.FullName}.{attribute.LocalKey}' " +
                $"as '{field.FieldType.FullName}'.");
        }

        field.SetValue(owner, resource.Value);

        if (!field.FieldType.IsValueType)
        {
            runtime.ResourceRegistry.TrackUnbindAction(() =>
            {
                field.SetValue(owner, null);
            });
        }
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
