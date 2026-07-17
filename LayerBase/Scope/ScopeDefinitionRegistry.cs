using System.Diagnostics.CodeAnalysis;

namespace LayerBase.Scope;

internal sealed class ScopeDefinitionRegistry
{
    private readonly Dictionary<Type, Entry> _byType = new();
    private readonly Dictionary<int, Entry> _byId = new();
    private readonly Dictionary<string, Entry> _byIdentity =
        new(StringComparer.Ordinal);

    public ScopeDefinitionRegistry()
    {
        Add(GeneratedScopeDefinition.Main, source: "framework:MainScope");
    }

    public IEnumerable<GeneratedScopeDefinition> OrderedDefinitions =>
        _byId.Values
            .OrderBy(static entry => entry.Definition.ScopeId)
            .ThenBy(
                static entry => entry.Definition.Identity,
                StringComparer.Ordinal)
            .Select(static entry => entry.Definition);

    public void Add(
        GeneratedScopeDefinition definition,
        string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException(
                "Scope definition source is required.",
                nameof(source));

        if (_byType.TryGetValue(definition.ScopeType, out Entry byType))
        {
            if (IsSame(byType.Definition, definition))
                return;

            throw Conflict(
                "The same Scope type was registered with different identity or ID.",
                byType,
                definition,
                source);
        }

        if (_byIdentity.TryGetValue(definition.Identity, out Entry byIdentity))
        {
            throw Conflict(
                "Different Scope types use the same identity.",
                byIdentity,
                definition,
                source);
        }

        if (_byId.TryGetValue(definition.ScopeId, out Entry byId))
        {
            throw Conflict(
                "Different Scope types use the same ID.",
                byId,
                definition,
                source);
        }

        var entry = new Entry(definition, source);
        _byType.Add(definition.ScopeType, entry);
        _byId.Add(definition.ScopeId, entry);
        _byIdentity.Add(definition.Identity, entry);
    }

    public GeneratedScopeDefinition Require(Type scopeType)
    {
        if (_byType.TryGetValue(scopeType, out Entry entry))
            return entry.Definition;

        throw new KeyNotFoundException(
            $"Scope type '{scopeType.FullName}' is not registered. " +
            "Ensure the owning layer or module is pushed and the scope has been registered.");
    }

    public bool TryGet(Type scopeType, out GeneratedScopeDefinition definition)
    {
        if (_byType.TryGetValue(scopeType, out Entry entry))
        {
            definition = entry.Definition;
            return true;
        }

        definition = default;
        return false;
    }

    private static ScopeDefinitionConflictException Conflict(
        string message,
        Entry existing,
        GeneratedScopeDefinition incoming,
        string incomingSource)
    {
        return new ScopeDefinitionConflictException(
            $"{message} Existing: '{existing.Definition.Identity}' (ID={existing.Definition.ScopeId}, " +
            $"Type={existing.Definition.ScopeType.FullName}, source='{existing.Source}'). " +
            $"Incoming: '{incoming.Identity}' (ID={incoming.ScopeId}, " +
            $"Type={incoming.ScopeType.FullName}, source='{incomingSource}').");
    }

    private static bool IsSame(GeneratedScopeDefinition a, GeneratedScopeDefinition b)
    {
        return a.ScopeId == b.ScopeId &&
               string.Equals(a.Identity, b.Identity, StringComparison.Ordinal) &&
               a.ScopeType == b.ScopeType;
    }

    private readonly record struct Entry(GeneratedScopeDefinition Definition, string Source);
}

internal sealed class ScopeDefinitionConflictException : InvalidOperationException
{
    public ScopeDefinitionConflictException(string message) : base(message)
    {
    }
}
