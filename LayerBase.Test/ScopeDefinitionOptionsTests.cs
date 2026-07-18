using LayerBase;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeDefinitionOptionsTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Worker_scope_preserves_declared_options()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new WorkerScopeLayer())
            .Build();

        ScopeExecutionPlan plan = runtime.CompositionPlan.Scopes
            .Single(p => p.Descriptor.ScopeType == typeof(TestWorkerScope));

        Assert.That(plan.Options.Threading, Is.EqualTo(ScopeThreadingMode.Worker));
        Assert.That(plan.Options.Clock, Is.EqualTo(ScopeClockMode.FixedRate));
        Assert.That(plan.Options.TickRateHz, Is.EqualTo(30));
        Assert.That(
            plan.Options.FaultPolicy,
            Is.EqualTo(ScopeFaultPolicy.StopScope));
        Assert.That(runtime.ScopeHost.HasWorkerScopes, Is.True);
    }

    [Test]
    public void Conflicting_scope_ids_are_rejected_during_build()
    {
        var conflictingModule = new ConflictingScopeIdsModule();

        Assert.That(
            () => LayerHub.CreateLayers()
                .Push(new WorkerScopeLayer())
                .AddAssemblyModule(conflictingModule)
                .Build(),
            Throws.TypeOf<ScopeDefinitionConflictException>());
    }

    [Test]
    public void Manual_scope_preserves_declared_options()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new ManualScopeLayer())
            .Build();

        ScopeExecutionPlan plan = runtime.CompositionPlan.Scopes
            .Single(p => p.Descriptor.ScopeType == typeof(TestManualScope));

        Assert.That(plan.Options.Threading, Is.EqualTo(ScopeThreadingMode.Inline));
        Assert.That(plan.Options.Clock, Is.EqualTo(ScopeClockMode.Manual));
        Assert.That(plan.Options.FaultPolicy, Is.EqualTo(ScopeFaultPolicy.StopScope));
    }

    private sealed class TestWorkerScope : IScopeDefinition
    {
        public ScopeOptions Options =>
            ScopeOptions.Worker(
                tickRateHz: 30,
                faultPolicy: ScopeFaultPolicy.StopScope);
    }

    private sealed class TestManualScope : IScopeDefinition
    {
        public ScopeOptions Options =>
            ScopeOptions.Manual(
                faultPolicy: ScopeFaultPolicy.StopScope);
    }

    private sealed class WorkerScopeLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 481,
                    identity: "scope:test:TestWorkerScope",
                    scopeType: typeof(TestWorkerScope),
                    factory: static () => new TestWorkerScope())
            };
        }
    }

    private sealed class ManualScopeLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 482,
                    identity: "scope:test:TestManualScope",
                    scopeType: typeof(TestManualScope),
                    factory: static () => new TestManualScope())
            };
        }
    }

    private sealed class ConflictingScopeIdsModule : IAssemblyModule
    {
        public AssemblyModuleId Id => new("conflicting-scope-ids");

        private static readonly AssemblyModuleManifest _manifest = CreateManifest();

        private static AssemblyModuleManifest CreateManifest()
        {
            return new AssemblyModuleManifest(
                new AssemblyModuleId("conflicting-scope-ids"),
                Array.Empty<ServiceContribution>(),
                Array.Empty<ContextContribution>(),
                Array.Empty<LocalCallContribution>(),
                Array.Empty<EventHandlerContribution>(),
                Array.Empty<LayerToolContribution>(),
                Array.Empty<EventContribution>(),
                new[]
                {
                    new GeneratedScopeDefinition(
                        scopeId: 481,
                        identity: "scope:test:ConflictingScope",
                        scopeType: typeof(ConflictingScope),
                        factory: static () => new ConflictingScope())
                });
        }

        public AssemblyModuleManifest Manifest => _manifest;

        private sealed class ConflictingScope : IScopeDefinition
        {
            public ScopeOptions Options => ScopeOptions.Inline;
        }
    }
}
