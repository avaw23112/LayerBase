using System.Reflection;
using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace EventsTest;

[TestFixture]
public sealed class ScopeCompositionPlanTests
{
    [Test]
    public void Composition_is_layer_first_and_push_order_is_the_only_layer_index_source()
    {
        LayerHub.Reset();

        var foundation = new FoundationLayer();
        var gameplay = new GameplayLayer();
        var presentation = new PresentationLayer();

        var runtime = LayerHub.CreateLayers()
                              .Push(foundation)
                              .Push(gameplay)
                              .Push(presentation)
                              .Build();

        var plan = runtime.CompositionPlan;

        Assert.That(plan.Layers.Select(static layer => layer.LayerIndex), Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(plan.Layers.Select(static layer => layer.LayerType),
            Is.EqualTo(new[] { typeof(FoundationLayer), typeof(GameplayLayer), typeof(PresentationLayer) }));
        Assert.That(foundation.RouteIndex, Is.EqualTo(0));
        Assert.That(gameplay.RouteIndex, Is.EqualTo(1));
        Assert.That(presentation.RouteIndex, Is.EqualTo(2));
    }

    [Test]
    public void Scope_execution_plan_is_derived_from_layer_plans_and_preserves_empty_layer_slices()
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new FoundationLayer())
                              .Push(new GameplayLayer())
                              .Push(new PresentationLayer())
                              .Build();

        var mainScopePlan = runtime.CompositionPlan.Scopes.Single(static scope =>
            scope.Descriptor.ScopeId == MainScope.ScopeId);

        Assert.That(mainScopePlan.LayerSlices.Select(static slice => slice.LayerIndex),
            Is.EqualTo(runtime.CompositionPlan.Layers.Select(static layer => layer.LayerIndex)));
    }

    [Test]
    public void Duplicate_layer_types_without_module_owner_resolution_are_preserved_by_push_order()
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new GameplayLayer())
                              .Push(new GameplayLayer())
                              .Build();

        Assert.That(runtime.CompositionPlan.Layers.Select(static layer => layer.LayerIndex),
            Is.EqualTo(new[] { 0, 1 }));
        Assert.That(runtime.CompositionPlan.Layers.Select(static layer => layer.LayerType),
            Is.EqualTo(new[] { typeof(GameplayLayer), typeof(GameplayLayer) }));
    }

    [Test]
    public void Module_contribution_is_written_to_layer_plan_before_scope_projection()
    {
        LayerHub.Reset();

        var module = new TestAssemblyModule(
            "combat",
            ServiceContribution.ForTypes(
                typeof(ICombatService),
                typeof(CombatService),
                typeof(GameplayLayer),
                typeof(MainScope),
                ServiceLifetime.Singleton));

        var runtime = LayerHub.CreateLayers()
                              .Push(new FoundationLayer())
                              .Push(new GameplayLayer())
                              .AddAssemblyModule(module)
                              .Build();

        var gameplayPlan = runtime.CompositionPlan.Layers.Single(static layer =>
            layer.LayerType == typeof(GameplayLayer));
        var contribution = gameplayPlan.ScopeContributions.Single();
        var mainScopePlan = runtime.CompositionPlan.Scopes.Single(static scope =>
            scope.Descriptor.ScopeId == MainScope.ScopeId);

        Assert.That(contribution.OwnerScopeId, Is.EqualTo(MainScope.ScopeId));
        Assert.That(contribution.ServiceCount, Is.EqualTo(1));
        Assert.That(mainScopePlan.LayerSlices.Select(static slice => slice.LayerIndex),
            Does.Contain(gameplayPlan.LayerIndex));
    }

    [Test]
    public void Business_contribution_requires_owner_layer()
    {
        LayerHub.Reset();

        var module = new TestAssemblyModule(
            "missing-owner",
            ServiceContribution.ForTypes(
                typeof(ICombatService),
                typeof(CombatService),
                ownerLayerType: null,
                typeof(MainScope),
                ServiceLifetime.Singleton));

        Assert.Throws<InvalidOperationException>(() =>
            LayerHub.CreateLayers()
                    .Push(new GameplayLayer())
                    .AddAssemblyModule(module)
                    .Build());
    }

    [Test]
    public void Missing_pushed_owner_layer_fails_build()
    {
        LayerHub.Reset();

        var module = new TestAssemblyModule(
            "combat",
            ServiceContribution.ForTypes(
                typeof(ICombatService),
                typeof(CombatService),
                typeof(GameplayLayer),
                typeof(MainScope),
                ServiceLifetime.Singleton));

        Assert.Throws<InvalidOperationException>(() =>
            LayerHub.CreateLayers()
                    .Push(new FoundationLayer())
                    .AddAssemblyModule(module)
                    .Build());
    }

    [Test]
    public void Module_owner_layer_type_must_resolve_to_one_pushed_layer()
    {
        LayerHub.Reset();

        var module = new TestAssemblyModule(
            "combat",
            ServiceContribution.ForTypes(
                typeof(ICombatService),
                typeof(CombatService),
                typeof(GameplayLayer),
                typeof(MainScope),
                ServiceLifetime.Singleton));

        Assert.Throws<InvalidOperationException>(() =>
            LayerHub.CreateLayers()
                    .Push(new GameplayLayer())
                    .Push(new GameplayLayer())
                    .AddAssemblyModule(module)
                    .Build());
    }

    [Test]
    public void Module_install_order_does_not_change_plan()
    {
        var firstOrder = BuildModulePlanSignature(
            new TestAssemblyModule("presentation",
                ServiceContribution.ForTypes(typeof(IPresentationService), typeof(PresentationService), typeof(PresentationLayer), typeof(MainScope), ServiceLifetime.Singleton)),
            new TestAssemblyModule("combat",
                ServiceContribution.ForTypes(typeof(ICombatService), typeof(CombatService), typeof(GameplayLayer), typeof(MainScope), ServiceLifetime.Singleton)));

        var secondOrder = BuildModulePlanSignature(
            new TestAssemblyModule("combat",
                ServiceContribution.ForTypes(typeof(ICombatService), typeof(CombatService), typeof(GameplayLayer), typeof(MainScope), ServiceLifetime.Singleton)),
            new TestAssemblyModule("presentation",
                ServiceContribution.ForTypes(typeof(IPresentationService), typeof(PresentationService), typeof(PresentationLayer), typeof(MainScope), ServiceLifetime.Singleton)));

        Assert.That(secondOrder, Is.EqualTo(firstOrder));
    }

    [Test]
    public void Module_api_cannot_auto_push_layer_or_bypass_layer_owned_service_registration()
    {
        var publicLayerBuilderMethods = typeof(LayerRuntime.LayersBuilder)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(static method => method.Name)
            .ToArray();

        Assert.That(publicLayerBuilderMethods, Does.Not.Contain("AddService"));
        Assert.That(publicLayerBuilderMethods, Does.Not.Contain("AddScopedService"));
        Assert.That(publicLayerBuilderMethods, Does.Not.Contain("AddLayer"));
    }

    private static string[] BuildModulePlanSignature(params IAssemblyModule[] modules)
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new FoundationLayer())
                              .Push(new GameplayLayer())
                              .Push(new PresentationLayer());

        foreach (var module in modules)
            runtime.AddAssemblyModule(module);

        var builtRuntime = runtime.Build();

        return builtRuntime.CompositionPlan.Layers
                           .SelectMany(static layer => layer.ScopeContributions.Select(contribution =>
                               $"{layer.LayerIndex}:{contribution.OwnerScopeId}:{contribution.ServiceStart}:{contribution.ServiceCount}"))
                           .ToArray();
    }

    private sealed class TestAssemblyModule : IAssemblyModule
    {
        public TestAssemblyModule(string id, params ServiceContribution[] services)
        {
            Id = new AssemblyModuleId(id);
            Manifest = new AssemblyModuleManifest(Id, services);
        }

        public AssemblyModuleId Id { get; }

        public AssemblyModuleManifest Manifest { get; }
    }

    private sealed class FoundationLayer : Layer { }

    private sealed class GameplayLayer : Layer { }

    private sealed class PresentationLayer : Layer { }

    private interface ICombatService { }

    private sealed class CombatService : ICombatService { }

    private interface IPresentationService { }

    private sealed class PresentationService : IPresentationService { }
}
