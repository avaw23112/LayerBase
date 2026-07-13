using LayerBase.Scope;
using LayerBase.Scope.Resources;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeResourceStrictValidationTests
{
    [Test]
    public void PlanBuilder_rejects_duplicate_candidate_type()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ScopeResourcePlanBuilder.Build(
                new[]
                {
                    new ScopeResourceObjectCandidate(typeof(ResourceProvider).TypeHandle, 0),
                    new ScopeResourceObjectCandidate(typeof(ResourceProvider).TypeHandle, 1)
                },
                Array.Empty<ScopeResourceExportContribution>(),
                Array.Empty<ScopeResourceImportContribution>()))!;

        Assert.That(exception.Message, Does.Contain("Duplicate scope resource candidate"));
    }

    [Test]
    public void PlanBuilder_rejects_negative_local_slots()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ScopeResourcePlanBuilder.Build(
                new[] { new ScopeResourceObjectCandidate(typeof(ResourceProvider).TypeHandle, 0) },
                new[]
                {
                    new ScopeResourceExportContribution(
                        typeof(ResourceProvider).TypeHandle,
                        typeof(string).TypeHandle,
                        "value",
                        exportId: 0,
                        providerLocalSlot: -1)
                },
                Array.Empty<ScopeResourceImportContribution>()))!;

        Assert.That(exception.Message, Does.Contain("local slot"));
    }

    [Test]
    public void PlanBuilder_rejects_incompatible_resource_types()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ScopeResourcePlanBuilder.Build(
                new[]
                {
                    new ScopeResourceObjectCandidate(typeof(ResourceProvider).TypeHandle, 0),
                    new ScopeResourceObjectCandidate(typeof(ResourceConsumer).TypeHandle, 1)
                },
                new[]
                {
                    new ScopeResourceExportContribution(
                        typeof(ResourceProvider).TypeHandle,
                        typeof(string).TypeHandle,
                        "value",
                        exportId: 0)
                },
                new[]
                {
                    new ScopeResourceImportContribution(
                        typeof(ResourceConsumer).TypeHandle,
                        typeof(ResourceProvider).TypeHandle,
                        typeof(int).TypeHandle,
                        "value",
                        importId: 0)
                }))!;

        Assert.That(exception.Message, Does.Contain("not assignable"));
    }

    [Test]
    public void Registry_rejects_provider_slot_out_of_range()
    {
        var registry = new ScopeResourceRegistry();
        var plan = new ScopeResourcePlan(
            new[] { new ScopeResourceExportPlan(providerObjectSlot: 1, providerLocalSlot: 0, exportSlot: 0) },
            Array.Empty<ScopeResourceImportPlan>());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            registry.Initialize(new object[] { new ResourceProvider() }, plan))!;

        Assert.That(exception.Message, Does.Contain("provider object slot"));
    }

    [Test]
    public void Registry_rejects_provider_without_publisher_interface()
    {
        var registry = new ScopeResourceRegistry();
        var plan = new ScopeResourcePlan(
            new[] { new ScopeResourceExportPlan(providerObjectSlot: 0, providerLocalSlot: 0, exportSlot: 0) },
            Array.Empty<ScopeResourceImportPlan>());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            registry.Initialize(new object[] { new object() }, plan))!;

        Assert.That(exception.Message, Does.Contain("does not implement"));
    }

    private sealed class ResourceProvider : IGeneratedScopeResourcePublisher
    {
        public object GetPublishedResource(int exportId)
        {
            return "value";
        }
    }

    private sealed class ResourceConsumer : IGeneratedScopeResourceConsumer
    {
        public void BindScopeResource(int importId, object resource)
        {
        }

        public void UnbindScopeResources()
        {
        }
    }
}
