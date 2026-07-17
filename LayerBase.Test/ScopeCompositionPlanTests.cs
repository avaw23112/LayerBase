using System.Reflection;
using LayerBase;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.DI.Options;
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
    public void Same_layer_can_receive_contributions_from_multiple_modules()
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new GameplayLayer())
                              .AddAssemblyModule(new TestAssemblyModule("combat",
                                  ServiceContribution.ForTypes(typeof(ICombatService), typeof(CombatService), typeof(GameplayLayer), typeof(MainScope), ServiceLifetime.Singleton)))
                              .AddAssemblyModule(new TestAssemblyModule("inventory",
                                  ServiceContribution.ForTypes(typeof(IInventoryService), typeof(InventoryService), typeof(GameplayLayer), typeof(MainScope), ServiceLifetime.Singleton)))
                              .Build();

        var contribution = runtime.CompositionPlan.Layers.Single().ScopeContributions.Single();

        Assert.That(contribution.ServiceCount, Is.EqualTo(2));
        Assert.That(runtime.CompositionPlan.Services.Select(static service => service.OwnerLayerIndex),
            Is.EqualTo(new[] { 0, 0 }));
    }

    [Test]
    public void Same_module_can_contribute_one_layer_to_multiple_scopes()
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new GameplayLayer())
                              .AddAssemblyModule(new TestAssemblyModule(
                                  "combat",
                                  ServiceContribution.ForTypes(typeof(ICombatService), typeof(CombatService), typeof(GameplayLayer), typeof(MainScope), ServiceLifetime.Singleton),
                                  ServiceContribution.ForTypes(typeof(IPathfindingService), typeof(PathfindingService), typeof(GameplayLayer), typeof(PathfindingScope), ServiceLifetime.Singleton)))
                              .Build();

        var gameplayPlan = runtime.CompositionPlan.Layers.Single();

        Assert.That(gameplayPlan.ScopeContributions.Select(static contribution => contribution.OwnerScopeId),
            Is.EqualTo(new[] { MainScope.ScopeId, PathfindingScope.ScopeId }));
        Assert.That(runtime.CompositionPlan.Scopes.Select(static scope => scope.Descriptor.ScopeId),
            Does.Contain(PathfindingScope.ScopeId));
    }

    [Test]
    public void Custom_scope_lifecycle_plan_preserves_empty_layer_slices()
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new FoundationLayer())
                              .Push(new GameplayLayer())
                              .Push(new PresentationLayer())
                              .AddAssemblyModule(new TestAssemblyModule(
                                  "pathfinding",
                                  ServiceContribution.ForTypes(
                                      typeof(IPathfindingService),
                                      typeof(PathfindingService),
                                      typeof(GameplayLayer),
                                      typeof(PathfindingScope),
                                      ServiceLifetime.Singleton)))
                              .Build();

        var pathfindingScope = runtime.CompositionPlan.Scopes.Single(static scope =>
            scope.Descriptor.ScopeId == PathfindingScope.ScopeId);

        Assert.That(pathfindingScope.LayerSlices.Select(static slice => slice.LayerIndex),
            Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(pathfindingScope.LifecyclePlan.Layers.Select(static slice => slice.LayerIndex),
            Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(pathfindingScope.LifecyclePlan.Layers.Select(static slice => slice.UpdateCount),
            Is.EqualTo(new[] { 0, 0, 0 }));
    }

    [Test]
    public void Context_must_match_owner_service_layer_and_scope()
    {
        LayerHub.Reset();

        var module = new TestAssemblyModule(
            "combat",
            services: new[]
            {
                ServiceContribution.ForTypes(typeof(ICombatService), typeof(CombatService), typeof(GameplayLayer), typeof(MainScope), ServiceLifetime.Singleton)
            },
            contexts: new[]
            {
                ContextContribution.ForTypes(typeof(CombatContext), typeof(ICombatService), typeof(GameplayLayer), typeof(PathfindingScope))
            });

        Assert.Throws<InvalidOperationException>(() =>
            LayerHub.CreateLayers()
                    .Push(new GameplayLayer())
                    .AddAssemblyModule(module)
                    .Build());
    }

    [Test]
    public void Local_call_uniqueness_is_per_scope()
    {
        LayerHub.Reset();

        var module = new TestAssemblyModule(
            "calls",
            calls: new[]
            {
                LocalCallContribution.ForTypes(typeof(SwitchSceneRequest), typeof(SwitchSceneResponse), typeof(CombatCallHandler), typeof(GameplayLayer), typeof(MainScope)),
                LocalCallContribution.ForTypes(typeof(SwitchSceneRequest), typeof(SwitchSceneResponse), typeof(AlternateCombatCallHandler), typeof(PresentationLayer), typeof(MainScope))
            });

        Assert.Throws<ScopeLocalCallRouteConflictException>(() =>
            LayerHub.CreateLayers()
                    .Push(new GameplayLayer())
                    .Push(new PresentationLayer())
                    .AddAssemblyModule(module)
                    .Build());
    }

    [Test]
    public void Same_call_can_have_handlers_in_different_scopes()
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new GameplayLayer())
                              .Push(new PresentationLayer())
                              .AddAssemblyModule(new TestAssemblyModule(
                                  "calls",
                                  calls: new[]
                                  {
                                      LocalCallContribution.ForTypes(typeof(SwitchSceneRequest), typeof(SwitchSceneResponse), typeof(CombatCallHandler), typeof(GameplayLayer), typeof(MainScope)),
                                      LocalCallContribution.ForTypes(typeof(SwitchSceneRequest), typeof(SwitchSceneResponse), typeof(PathfindingCallHandler), typeof(PresentationLayer), typeof(PathfindingScope))
                                  }))
                              .Build();

        Assert.That(runtime.CompositionPlan.LocalCalls.Select(static call => call.OwnerScopeId),
            Is.EqualTo(new[] { MainScope.ScopeId, PathfindingScope.ScopeId }));
    }

    [Test]
    public void Event_handler_contribution_is_written_to_layer_plan_without_module_dispatcher()
    {
        LayerHub.Reset();

        var runtime = LayerHub.CreateLayers()
                              .Push(new GameplayLayer())
                              .AddAssemblyModule(new TestAssemblyModule(
                                  "events",
                                  eventHandlers: new[]
                                  {
                                      EventHandlerContribution.ForTypes(
                                          typeof(InventoryChangedEvent),
                                          typeof(InventoryChangedHandler),
                                          typeof(IInventoryService),
                                          typeof(GameplayLayer),
                                          typeof(MainScope))
                                  }))
                              .Build();

        var gameplayPlan = runtime.CompositionPlan.Layers.Single();
        var contribution = gameplayPlan.ScopeContributions.Single();

        Assert.That(runtime.CompositionPlan.EventHandlers.Single().OwnerLayerIndex, Is.EqualTo(0));
        Assert.That(runtime.CompositionPlan.EventHandlers.Single().OwnerScopeId, Is.EqualTo(MainScope.ScopeId));
        Assert.That(runtime.CompositionPlan.EventHandlers.Single().EventType, Is.EqualTo(typeof(InventoryChangedEvent)));
        Assert.That(runtime.CompositionPlan.EventHandlers.Single().HandlerType, Is.EqualTo(typeof(InventoryChangedHandler)));
        Assert.That(runtime.CompositionPlan.EventHandlers.Single().OwnerServiceType, Is.EqualTo(typeof(IInventoryService)));
        Assert.That(contribution.EventHandlerCount, Is.EqualTo(1));
    }

    [Test]
    public void Tool_key_is_runtime_global_contract_and_key()
    {
        LayerHub.Reset();

        var module = new TestAssemblyModule(
            "tools",
            tools: new[]
            {
                LayerToolContribution.ForTypes(typeof(ICombatTool), typeof(CombatTool), "default", typeof(GameplayLayer)),
                LayerToolContribution.ForTypes(typeof(ICombatTool), typeof(CombatTool), "default", typeof(PresentationLayer))
            });

        Assert.Throws<InvalidOperationException>(() =>
            LayerHub.CreateLayers()
                    .Push(new GameplayLayer())
                    .Push(new PresentationLayer())
                    .AddAssemblyModule(module)
                    .Build());
    }

    [Test]
    public void Manifest_contains_factories_not_instances_and_runtime_has_no_module_dispatcher()
    {
        var manifestProperties = typeof(AssemblyModuleManifest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .ToArray();

        Assert.That(manifestProperties, Does.Not.Contain("Runtime"));
        Assert.That(manifestProperties, Does.Not.Contain("Scope"));
        Assert.That(manifestProperties, Does.Not.Contain("Instance"));
        Assert.That(typeof(LayerRuntime).Assembly.GetTypes().Any(static type => type.Name.Contains("ModuleDispatcher")),
            Is.False);
    }

    [Test]
    public void Reflection_assembly_module_builds_manifest_from_explicit_assembly_only()
    {
        var module = ReflectionAssemblyModule.Build(typeof(ReflectionFeatureService).Assembly);

        Assert.That(module.Id.Value, Is.EqualTo(typeof(ReflectionFeatureService).Assembly.GetName().Name));
        Assert.That(module.Manifest.Services.Any(static service =>
            service.ServiceType == typeof(ReflectionFeatureService) &&
            service.ImplementationType == typeof(ReflectionFeatureService) &&
            service.OwnerLayerType == typeof(ReflectionFeatureLayer) &&
            service.OwnerScopeType == typeof(ReflectionFeatureScope)), Is.True);
        Assert.That(module.Manifest.Contexts.Any(static context =>
            context.ContextType == typeof(ReflectionFeatureContext) &&
            context.OwnerServiceType == typeof(ReflectionFeatureService) &&
            context.OwnerLayerType == typeof(ReflectionFeatureLayer) &&
            context.OwnerScopeType == typeof(ReflectionFeatureScope)), Is.True);
        Assert.That(module.Manifest.EventHandlers.Any(static handler =>
            handler.EventType == typeof(ReflectionFeatureEvent) &&
            handler.HandlerType == typeof(ReflectionFeatureEventHandler) &&
            handler.OwnerServiceType == typeof(ReflectionFeatureService) &&
            handler.OwnerLayerType == typeof(ReflectionFeatureLayer) &&
            handler.OwnerScopeType == typeof(ReflectionFeatureScope)), Is.True);
        Assert.That(module.Manifest.LocalCalls.Any(static call =>
            call.RequestType == typeof(ReflectionFeatureRequest) &&
            call.ResponseType == typeof(ReflectionFeatureResponse) &&
            call.HandlerType == typeof(ReflectionFeatureCallHandler) &&
            call.OwnerLayerType == typeof(ReflectionFeatureLayer) &&
            call.OwnerScopeType == typeof(MainScope)), Is.True);
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
            : this(
                id,
                services,
                Array.Empty<ContextContribution>(),
                Array.Empty<LocalCallContribution>(),
                Array.Empty<EventHandlerContribution>(),
                Array.Empty<LayerToolContribution>())
        {
        }

        public TestAssemblyModule(
            string id,
            ServiceContribution[]? services = null,
            ContextContribution[]? contexts = null,
            LocalCallContribution[]? calls = null,
            EventHandlerContribution[]? eventHandlers = null,
            LayerToolContribution[]? tools = null)
        {
            Id = new AssemblyModuleId(id);
            Manifest = new AssemblyModuleManifest(
                Id,
                services ?? Array.Empty<ServiceContribution>(),
                contexts ?? Array.Empty<ContextContribution>(),
                calls ?? Array.Empty<LocalCallContribution>(),
                eventHandlers ?? Array.Empty<EventHandlerContribution>(),
                tools ?? Array.Empty<LayerToolContribution>());
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

    private interface IInventoryService { }

    private sealed class InventoryService : IInventoryService { }

    private interface IPathfindingService { }

    private sealed class PathfindingService : IPathfindingService { }

    private sealed class CombatContext { }

    private interface ICombatTool { }

    private sealed class CombatTool : ICombatTool { }

    private readonly struct InventoryChangedEvent { }

    private sealed class InventoryChangedHandler : IEventHandler<InventoryChangedEvent>
    {
        public void Deal(in InventoryChangedEvent @event)
        {
        }
    }

    private sealed class CombatCallHandler { }

    private sealed class AlternateCombatCallHandler { }

    private sealed class PathfindingCallHandler { }

    public sealed class PathfindingScope : IScopeDefinition
    {
        public const int ScopeId = 7;
        public ScopeOptions Options => ScopeOptions.Inline;
    }

    public sealed partial class ReflectionFeatureLayer : Layer { }

    public sealed class ReflectionFeatureScope : IScopeDefinition
    {
        public const int ScopeId = 8;
        public ScopeOptions Options => ScopeOptions.Inline;
    }

    [Scope<ReflectionFeatureScope>]
    [OwnerLayer(typeof(ReflectionFeatureLayer))]
    public sealed partial class ReflectionFeatureService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    [OwnerService(typeof(ReflectionFeatureService))]
    public sealed partial class ReflectionFeatureContext : ILayerContext { }

    public readonly struct ReflectionFeatureEvent { }

    [OwnerService(typeof(ReflectionFeatureService))]
    public sealed partial class ReflectionFeatureEventHandler : IEventHandler<ReflectionFeatureEvent>
    {
        public void Deal(in ReflectionFeatureEvent @event)
        {
        }
    }

    public readonly struct ReflectionFeatureRequest { }

    public readonly struct ReflectionFeatureResponse { }

    [OwnerLayer(typeof(ReflectionFeatureLayer))]
    public sealed partial class ReflectionFeatureCallHandler
        : IScopeLocalCallHandler<ReflectionFeatureRequest, ReflectionFeatureResponse>
    {
        public async LBTask<ReflectionFeatureResponse> HandleAsync(
            ReflectionFeatureRequest request,
            CancellationToken cancellationToken = default)
        {
            await LBTask.CompletedTask;
            return new ReflectionFeatureResponse();
        }
    }
}
