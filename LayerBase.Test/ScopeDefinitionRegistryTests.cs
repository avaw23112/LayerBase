using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeDefinitionRegistryTests
{
    [Test]
    public void Identical_duplicate_is_deduplicated()
    {
        var registry = new ScopeDefinitionRegistry();
        GeneratedScopeDefinition definition = Create(
            scopeId: 11,
            identity: "scope-key:game.inventory",
            scopeType: typeof(InventoryScope),
            factory: static () => new InventoryScope());

        registry.Add(definition, source: "layer:A");
        registry.Add(definition, source: "module:B");

        Assert.That(
            registry.OrderedDefinitions.Count(
                item => item.ScopeType == typeof(InventoryScope)),
            Is.EqualTo(1));
    }

    [Test]
    public void Same_id_with_different_identity_is_rejected()
    {
        var registry = new ScopeDefinitionRegistry();

        registry.Add(
            Create(
                11,
                "scope-key:game.inventory",
                typeof(InventoryScope),
                static () => new InventoryScope()),
            source: "layer:A");

        Assert.That(
            () => registry.Add(
                Create(
                    11,
                    "scope-key:game.payment",
                    typeof(PaymentScope),
                    static () => new PaymentScope()),
                source: "module:B"),
            Throws.TypeOf<ScopeDefinitionConflictException>());
    }

    [Test]
    public void Same_identity_with_different_type_is_rejected()
    {
        var registry = new ScopeDefinitionRegistry();

        registry.Add(
            Create(
                11,
                "scope-key:game.inventory",
                typeof(InventoryScope),
                static () => new InventoryScope()),
            source: "layer:A");

        Assert.That(
            () => registry.Add(
                Create(
                    12,
                    "scope-key:game.inventory",
                    typeof(PaymentScope),
                    static () => new PaymentScope()),
                source: "module:B"),
            Throws.TypeOf<ScopeDefinitionConflictException>());
    }

    [Test]
    public void Same_type_with_different_id_is_rejected()
    {
        var registry = new ScopeDefinitionRegistry();

        registry.Add(
            Create(
                11,
                "scope-key:game.inventory",
                typeof(InventoryScope),
                static () => new InventoryScope()),
            source: "layer:A");

        Assert.That(
            () => registry.Add(
                Create(
                    12,
                    "scope-key:game.inventory",
                    typeof(InventoryScope),
                    static () => new InventoryScope()),
                source: "module:B"),
            Throws.TypeOf<ScopeDefinitionConflictException>());
    }

    private static GeneratedScopeDefinition Create(
        int scopeId,
        string identity,
        Type scopeType,
        ScopeDefinitionFactory factory)
    {
        return new GeneratedScopeDefinition(
            scopeId: scopeId,
            identity: identity,
            scopeType: scopeType,
            factory: factory);
    }

    private sealed class InventoryScope : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }

    private sealed class PaymentScope : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }
}
