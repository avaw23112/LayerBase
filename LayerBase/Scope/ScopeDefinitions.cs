namespace LayerBase.Scope;

public interface IScopeDefinition
{
    ScopeOptions Options { get; }
}

public sealed class MainScope : IScopeDefinition
{
    public const int ScopeId = 0;

    public ScopeOptions Options => ScopeOptions.Main;
}

internal static class ScopeDefinitionIds
{
    public const int Main = MainScope.ScopeId;

    public const string MainIdentity =
        "scope:LayerBase:LayerBase.Scope.MainScope";

    public static int FromType(Type scopeType)
    {
        if (scopeType == typeof(MainScope))
            return Main;

        throw new NotSupportedException(
            $"Scope type '{scopeType.FullName}' must be resolved through " +
            "the ScopeDefinitionRegistry. Direct resolution is no longer supported.");
    }
}
