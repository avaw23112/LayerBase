namespace LayerBase.Scope;

internal interface IServiceScopeBinding
{
    void BindScope(ScopeRuntime ownerScope, int serviceId);
}
