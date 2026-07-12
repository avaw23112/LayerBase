using System;

namespace LayerBase.Scope.Resources;

public readonly struct ScopeResourceImportContribution
{
    public ScopeResourceImportContribution(
        RuntimeTypeHandle consumerType,
        RuntimeTypeHandle providerType,
        RuntimeTypeHandle requestedResourceType,
        string localKey,
        int importId)
        : this(consumerType, providerType, requestedResourceType, localKey, importId, importId)
    {
    }

    public ScopeResourceImportContribution(
        RuntimeTypeHandle consumerType,
        RuntimeTypeHandle providerType,
        RuntimeTypeHandle requestedResourceType,
        string localKey,
        int importId,
        int consumerLocalSlot)
    {
        ConsumerType = consumerType;
        ProviderType = providerType;
        RequestedResourceType = requestedResourceType;
        LocalKey = localKey ?? throw new ArgumentNullException(nameof(localKey));
        ImportId = importId;
        ConsumerLocalSlot = consumerLocalSlot;
    }

    public RuntimeTypeHandle ConsumerType { get; }
    public RuntimeTypeHandle ProviderType { get; }
    public RuntimeTypeHandle RequestedResourceType { get; }
    public string LocalKey { get; }
    public int ImportId { get; }
    public int ConsumerLocalSlot { get; }
}
