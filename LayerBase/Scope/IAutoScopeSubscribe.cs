namespace LayerBase.Scope;

internal interface IAutoScopeSubscribe
{
    void Bind(in ScopeSubscriptionContext context);
}
