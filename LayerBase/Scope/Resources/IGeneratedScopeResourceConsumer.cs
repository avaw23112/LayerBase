namespace LayerBase.Scope.Resources;

public interface IGeneratedScopeResourceConsumer
{
    void BindScopeResource(int importId, object resource);
    void UnbindScopeResources();
}
