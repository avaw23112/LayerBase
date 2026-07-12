namespace LayerBase.Scope.Resources;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ScopeResourceExportManifestAttribute : Attribute
{
    public ScopeResourceExportManifestAttribute(Type providerType, Type resourceType, string localKey)
    {
        ProviderType = providerType ?? throw new ArgumentNullException(nameof(providerType));
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        LocalKey = localKey ?? throw new ArgumentNullException(nameof(localKey));
    }

    public Type ProviderType { get; }

    public Type ResourceType { get; }

    public string LocalKey { get; }
}
