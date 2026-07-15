namespace LayerBase.Scope;

public interface IScopeDefinition
{
}

public readonly struct MainScope : IScopeDefinition
{
    public const int ScopeId = 0;
}

internal static class ScopeDefinitionIds
{
    public const int Main = MainScope.ScopeId;
}
