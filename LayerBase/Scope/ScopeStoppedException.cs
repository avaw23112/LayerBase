namespace LayerBase.Scope;

public sealed class ScopeStoppedException : InvalidOperationException
{
    public ScopeStoppedException(int scopeId, string scopeName, string apiName)
        : base($"Scope '{scopeName}' ({scopeId}) cannot accept business ingress through '{apiName}' after stop was requested.")
    {
        ScopeId = scopeId;
        ScopeName = scopeName;
        ApiName = apiName;
    }

    public int ScopeId { get; }

    public string ScopeName { get; }

    public string ApiName { get; }
}
