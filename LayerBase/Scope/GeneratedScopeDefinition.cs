namespace LayerBase.Scope;

public delegate IScopeDefinition ScopeDefinitionFactory();

public readonly struct GeneratedScopeDefinition
{
    public GeneratedScopeDefinition(
        int scopeId,
        string identity,
        Type scopeType,
        ScopeDefinitionFactory factory)
    {
        if (scopeType == null)
            throw new ArgumentNullException(nameof(scopeType));
        if (!typeof(IScopeDefinition).IsAssignableFrom(scopeType))
            throw new ArgumentException(
                $"Scope type '{scopeType.FullName}' must implement {nameof(IScopeDefinition)}.",
                nameof(scopeType));
        if (scopeId < 0)
            throw new ArgumentOutOfRangeException(nameof(scopeId));
        if (scopeId == ScopeDefinitionIds.Main && scopeType != typeof(MainScope))
            throw new ArgumentOutOfRangeException(
                nameof(scopeId),
                "Scope ID zero is reserved for MainScope.");
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("Scope identity is required.", nameof(identity));

        ScopeId = scopeId;
        Identity = identity;
        ScopeType = scopeType;
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public int ScopeId { get; }

    public string Identity { get; }

    public Type ScopeType { get; }

    public ScopeDefinitionFactory Factory { get; }

    public IScopeDefinition CreateDefinition()
    {
        IScopeDefinition definition = Factory()
            ?? throw new InvalidOperationException(
                $"Scope factory for '{ScopeType.FullName}' returned null.");

        if (definition.GetType() != ScopeType)
        {
            throw new InvalidOperationException(
                $"Scope factory for '{ScopeType.FullName}' returned " +
                $"'{definition.GetType().FullName}'.");
        }

        return definition;
    }

    internal static GeneratedScopeDefinition Main { get; } = new(
        scopeId: ScopeDefinitionIds.Main,
        identity: ScopeDefinitionIds.MainIdentity,
        scopeType: typeof(MainScope),
        factory: static () => new MainScope());
}
