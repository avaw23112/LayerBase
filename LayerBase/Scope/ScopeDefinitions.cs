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

    public static int Resolve(Type scopeType)
    {
        if (scopeType == null)
            throw new ArgumentNullException(nameof(scopeType));
        if (!typeof(IScopeDefinition).IsAssignableFrom(scopeType))
            throw new InvalidOperationException($"Scope type `{scopeType.FullName}` must implement {nameof(IScopeDefinition)}.");

        var field = scopeType.GetField("ScopeId");
        if (field == null || field.FieldType != typeof(int) || !field.IsStatic)
            throw new InvalidOperationException($"Scope type `{scopeType.FullName}` must declare a public static int ScopeId.");

        return (int)field.GetValue(null)!;
    }
}
