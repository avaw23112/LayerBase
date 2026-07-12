using System;

namespace LayerBase.Scope.Resources;

public readonly struct ScopeResourceExportContribution
{
    public ScopeResourceExportContribution(
        RuntimeTypeHandle providerType,
        RuntimeTypeHandle declaredResourceType,
        string localKey,
        int exportId)
        : this(providerType, declaredResourceType, localKey, exportId, exportId)
    {
    }

    public ScopeResourceExportContribution(
        RuntimeTypeHandle providerType,
        RuntimeTypeHandle declaredResourceType,
        string localKey,
        int exportId,
        int providerLocalSlot)
    {
        ProviderType = providerType;
        DeclaredResourceType = declaredResourceType;
        LocalKey = localKey ?? throw new ArgumentNullException(nameof(localKey));
        ExportId = exportId;
        ProviderLocalSlot = providerLocalSlot;
    }

    public RuntimeTypeHandle ProviderType { get; }
    public RuntimeTypeHandle DeclaredResourceType { get; }
    public string LocalKey { get; }
    public int ExportId { get; }
    public int ProviderLocalSlot { get; }
}
