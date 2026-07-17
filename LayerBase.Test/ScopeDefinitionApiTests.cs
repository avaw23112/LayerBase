using LayerBase.ECS;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeDefinitionApiTests
{
    [Test]
    public void Scope_definition_exposes_its_own_options()
    {
        var scope = new WorkerProbeScope();

        Assert.Multiple(() =>
        {
            Assert.That(scope.Options.Threading, Is.EqualTo(ScopeThreadingMode.Worker));
            Assert.That(scope.Options.Clock, Is.EqualTo(ScopeClockMode.FixedRate));
            Assert.That(scope.Options.TickRateHz, Is.EqualTo(37));
            Assert.That(scope.Options.FaultPolicy, Is.EqualTo(ScopeFaultPolicy.StopScope));
        });
    }

    [Test]
    public void Stable_identity_attribute_trims_surrounding_whitespace()
    {
        var attribute = new ScopeIdentityAttribute("  game.inventory  ");

        Assert.That(attribute.Value, Is.EqualTo("game.inventory"));
    }

    [Test]
    public void Stable_identity_attribute_rejects_empty_value()
    {
        Assert.That(
            () => new ScopeIdentityAttribute("   "),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Generated_definition_rejects_custom_scope_id_zero()
    {
        Assert.That(
            () => new GeneratedScopeDefinition(
                scopeId: 0,
                identity: "scope-key:game.inventory",
                scopeType: typeof(WorkerProbeScope),
                factory: static () => new WorkerProbeScope()),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Generated_definition_factory_must_return_declared_type()
    {
        var descriptor = new GeneratedScopeDefinition(
            scopeId: 17,
            identity: "scope-key:game.inventory",
            scopeType: typeof(WorkerProbeScope),
            factory: static () => new InlineProbeScope());

        Assert.That(
            descriptor.CreateDefinition,
            Throws.TypeOf<InvalidOperationException>());
    }

    private sealed class WorkerProbeScope : IScopeDefinition
    {
        public ScopeOptions Options { get; } = ScopeOptions.Worker(
            tickRateHz: 37,
            faultPolicy: ScopeFaultPolicy.StopScope,
            ecsRuntime: EcsRuntimeOptions.Default);
    }

    private sealed class InlineProbeScope : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }
}
