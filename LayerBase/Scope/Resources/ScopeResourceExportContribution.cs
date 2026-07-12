using System;

namespace LayerBase.Scope.Resources;

public readonly struct ScopeResourceExportContribution
{
    public ScopeResourceExportContribution(
        RuntimeTypeHandle providerType,
        RuntimeTypeHandle declaredResourceType,
        string localKey,
        int exportId)
    {
        ProviderType = providerType;
        DeclaredResourceType = declaredResourceType;
        LocalKey = localKey ?? throw new ArgumentNullException(nameof(localKey));
        ExportId = exportId;
    }

    public RuntimeTypeHandle ProviderType { get; }
    public RuntimeTypeHandle DeclaredResourceType { get; }
    public string LocalKey { get; }
    public int ExportId { get; }
}
