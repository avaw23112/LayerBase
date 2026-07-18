using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeSparseIdTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Large_scope_id_does_not_allocate_dense_array()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new SparseScopeLayer(scopeId: 1_500_000_000))
            .Build();

        var host = runtime.ScopeHost;
        Assert.That(host.Scopes.Count, Is.EqualTo(2));
        Assert.That(host.Scopes[0].ScopeId, Is.EqualTo(0));
        Assert.That(host.Scopes[1].ScopeId, Is.EqualTo(1_500_000_000));
    }

    [Test]
    public void Int_max_scope_id_can_build_without_oom()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new SparseScopeLayer(scopeId: int.MaxValue))
            .Build();

        Assert.That(runtime.ScopeHost.Scopes.Count, Is.EqualTo(2));
    }

    [Test]
    public void Duplicate_scope_id_is_rejected()
    {
        var builder = LayerHub.CreateLayers();
        builder.Push(new DuplicateScopeLayer());
        var ex = Assert.Catch(() => builder.Build());
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("same ID"));
    }

    [Test]
    public void Sparse_scope_ids_can_route_events_and_calls()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new SparseScopeLayer(scopeId: 777))
            .Build();

        Assert.That(runtime.ScopeHost.TryGetRuntime(0, out var mainRuntime), Is.True);
        Assert.That(mainRuntime!.ScopeId, Is.EqualTo(0));

        Assert.That(runtime.ScopeHost.TryGetRuntime(777, out var sparseRuntime), Is.True);
        Assert.That(sparseRuntime!.ScopeId, Is.EqualTo(777));
    }

    [Test]
    public void Main_scope_remains_runtime_slot_zero()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new SparseScopeLayer(scopeId: 777))
            .Build();

        Assert.That(runtime.ScopeHost.Scopes[0].ScopeId, Is.EqualTo(0));
        Assert.That(runtime.ScopeHost.Scopes[1].ScopeId, Is.EqualTo(777));
    }

    [Test]
    public void Unknown_scope_id_returns_false_without_exception()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new SparseScopeLayer(scopeId: 777))
            .Build();

        Assert.That(runtime.ScopeHost.TryGetRuntime(999999, out _), Is.False);
    }

    private sealed class SparseScopeLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly int _scopeId;

        public SparseScopeLayer(int scopeId)
        {
            _scopeId = scopeId;
        }

        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: _scopeId,
                    identity: $"scope:test:SparseScope:{_scopeId}",
                    scopeType: typeof(SparseScope),
                    factory: static () => new SparseScope())
            };
        }
    }

    private sealed class DuplicateScopeLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 777,
                    identity: "scope:test:DupScope1",
                    scopeType: typeof(DupScope1),
                    factory: static () => new DupScope1()),
                new GeneratedScopeDefinition(
                    scopeId: 777,
                    identity: "scope:test:DupScope2",
                    scopeType: typeof(DupScope2),
                    factory: static () => new DupScope2()),
            };
        }
    }

    private sealed class SparseScope : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }

    private sealed class DupScope1 : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }

    private sealed class DupScope2 : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }
}
