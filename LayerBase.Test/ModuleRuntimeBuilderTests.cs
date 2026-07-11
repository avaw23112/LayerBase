using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ModuleRuntimeBuilderTests
{
    [Test]
    public void Build_merges_definition_and_implementation_contributions()
    {
        using var module = new TestModule(
            layerContracts: [LayerContract<TestLayer>()],
            scopeDefinitions: [ScopeDefinition<TestScope>()],
            messageContracts: [CallContract<TestCall, TestScope, TestResult>()],
            services:
            [
                Service<TestService>(
                    ownerScope: typeof(TestScope),
                    ownerLayers: [typeof(TestLayer)])
            ],
            contexts: [],
            handlers:
            [
                Handler<TestCall, TestService, TestScope>(ScopeMessageKind.Call)
            ]);

        ModuleRuntimeCatalog catalog = ModuleRuntimeBuilder.Build(module);

        Assert.That(catalog.LayerContracts, Has.Count.EqualTo(1));
        Assert.That(catalog.ScopeDefinitions, Has.Count.EqualTo(1));
        Assert.That(catalog.MessageContracts, Has.Count.EqualTo(1));
        Assert.That(catalog.Services, Has.Count.EqualTo(1));
        Assert.That(catalog.Handlers, Has.Count.EqualTo(1));
        Assert.That(catalog.ModuleSlots[module], Is.EqualTo(0));
        Assert.That(catalog.ScopeIds[typeof(TestScope).TypeHandle], Is.EqualTo(0));
        Assert.That(catalog.ServiceSlots[typeof(TestService).TypeHandle], Is.EqualTo(0));
        Assert.That(catalog.MessageRouteIds[typeof(TestCall).TypeHandle], Is.EqualTo(0));
        Assert.That(catalog.CallRoutes, Has.Count.EqualTo(1));
        Assert.That(catalog.EventRoutes, Has.Count.EqualTo(0));
        Assert.That(catalog.EventHandlerRoutes, Has.Count.EqualTo(0));
    }

    [Test]
    public void Build_allocates_scope_local_service_slots()
    {
        using var module = new TestModule(
            layerContracts: [LayerContract<TestLayer>()],
            scopeDefinitions:
            [
                ScopeDefinition<TestScope>(),
                ScopeDefinition<OtherScope>()
            ],
            services:
            [
                Service<TestService>(
                    ownerScope: typeof(TestScope),
                    ownerLayers: [typeof(TestLayer)]),
                Service<OtherService>(
                    ownerScope: typeof(TestScope),
                    ownerLayers: [typeof(TestLayer)]),
                Service<OtherScopeService>(
                    ownerScope: typeof(OtherScope),
                    ownerLayers: [typeof(TestLayer)])
            ]);

        ModuleRuntimeCatalog catalog = ModuleRuntimeBuilder.Build(module);

        Assert.That(catalog.ServiceSlots[typeof(OtherScopeService).TypeHandle], Is.EqualTo(0));
        Assert.That(catalog.ServiceSlots[typeof(OtherService).TypeHandle], Is.EqualTo(0));
        Assert.That(catalog.ServiceSlots[typeof(TestService).TypeHandle], Is.EqualTo(1));
    }

    [Test]
    public void Build_computes_call_routes_with_correct_slots()
    {
        using var module = new TestModule(
            layerContracts: [LayerContract<TestLayer>()],
            scopeDefinitions: [ScopeDefinition<TestScope>()],
            messageContracts: [CallContract<TestCall, TestScope, TestResult>()],
            services:
            [
                Service<TestService>(
                    ownerScope: typeof(TestScope),
                    ownerLayers: [typeof(TestLayer)])
            ],
            handlers:
            [
                Handler<TestCall, TestService, TestScope>(ScopeMessageKind.Call)
            ]);

        ModuleRuntimeCatalog catalog = ModuleRuntimeBuilder.Build(module);

        Assert.That(catalog.CallRoutes, Has.Count.EqualTo(1));
        ScopeCallRoute route = catalog.CallRoutes[0];
        Assert.That(route.ScopeId, Is.EqualTo(0));
        Assert.That(route.ModuleSlot, Is.EqualTo(0));
        Assert.That(route.LocalHandlerId, Is.EqualTo(0));
        Assert.That(route.ServiceSlot, Is.EqualTo(0));
    }

    [Test]
    public void Build_computes_event_routes_with_handler_routes()
    {
        using var module = new TestModule(
            layerContracts: [LayerContract<TestLayer>()],
            scopeDefinitions: [ScopeDefinition<TestScope>()],
            messageContracts: [EventContract<TestEvent, TestScope>()],
            services:
            [
                Service<TestService>(
                    ownerScope: typeof(TestScope),
                    ownerLayers: [typeof(TestLayer)])
            ],
            handlers:
            [
                Handler<TestEvent, TestService, TestScope>(ScopeMessageKind.Event)
            ]);

        ModuleRuntimeCatalog catalog = ModuleRuntimeBuilder.Build(module);

        Assert.That(catalog.EventRoutes, Has.Count.EqualTo(1));
        ScopeEventRoute eventRoute = catalog.EventRoutes[0];
        Assert.That(eventRoute.ScopeId, Is.EqualTo(0));
        Assert.That(eventRoute.HandlerStart, Is.EqualTo(0));
        Assert.That(eventRoute.HandlerCount, Is.EqualTo(1));

        Assert.That(catalog.EventHandlerRoutes, Has.Count.EqualTo(1));
        ScopeEventHandlerRoute handlerRoute = catalog.EventHandlerRoutes[0];
        Assert.That(handlerRoute.ModuleSlot, Is.EqualTo(0));
        Assert.That(handlerRoute.LocalHandlerId, Is.EqualTo(0));
        Assert.That(handlerRoute.ServiceSlot, Is.EqualTo(0));
    }

    [Test]
    public void Build_rejects_duplicate_scope_definition()
    {
        using var first = new TestModule(scopeDefinitions: [ScopeDefinition<TestScope>()]);
        using var second = new TestModule(scopeDefinitions: [ScopeDefinition<TestScope>()]);

        ModuleBuildException exception = Assert.Throws<ModuleBuildException>(
            () => ModuleRuntimeBuilder.Build(first, second))!;

        Assert.That(exception.Code, Is.EqualTo(ModuleBuildErrorCodes.DuplicateScopeDefinition));
        Assert.That(exception.Message, Does.Contain("defined by multiple Modules"));
    }

    [Test]
    public void Build_rejects_service_targeting_missing_scope()
    {
        using var module = new TestModule(
            layerContracts: [LayerContract<TestLayer>()],
            services:
            [
                Service<TestService>(
                    ownerScope: typeof(TestScope),
                    ownerLayers: [typeof(TestLayer)])
            ]);

        ModuleBuildException exception = Assert.Throws<ModuleBuildException>(
            () => ModuleRuntimeBuilder.Build(module))!;

        Assert.That(exception.Code, Is.EqualTo(ModuleBuildErrorCodes.MissingScopeDefinition));
        Assert.That(exception.Message, Does.Contain("no installed Module defines that Scope"));
    }

    [Test]
    public void Build_rejects_handler_scope_mismatch()
    {
        using var module = new TestModule(
            layerContracts: [LayerContract<TestLayer>()],
            scopeDefinitions:
            [
                ScopeDefinition<TestScope>(),
                ScopeDefinition<OtherScope>()
            ],
            messageContracts: [CallContract<TestCall, TestScope, TestResult>()],
            services:
            [
                Service<TestService>(
                    ownerScope: typeof(OtherScope),
                    ownerLayers: [typeof(TestLayer)])
            ],
            handlers:
            [
                Handler<TestCall, TestService, OtherScope>(ScopeMessageKind.Call)
            ]);

        ModuleBuildException exception = Assert.Throws<ModuleBuildException>(
            () => ModuleRuntimeBuilder.Build(module))!;

        Assert.That(exception.Code, Is.EqualTo(ModuleBuildErrorCodes.HandlerScopeMismatch));
        Assert.That(exception.Message, Does.Contain("but message"));
        Assert.That(exception.Message, Does.Contain("targets Scope"));
    }

    [Test]
    public void Build_rejects_call_without_handler()
    {
        using var module = new TestModule(
            messageContracts: [CallContract<TestCall, TestScope, TestResult>()]);

        ModuleBuildException exception = Assert.Throws<ModuleBuildException>(
            () => ModuleRuntimeBuilder.Build(module))!;

        Assert.That(exception.Code, Is.EqualTo(ModuleBuildErrorCodes.CallNoHandler));
        Assert.That(exception.Message, Does.Contain("has no installed handler"));
    }

    [Test]
    public void Build_rejects_call_with_multiple_handlers()
    {
        using var module = new TestModule(
            layerContracts: [LayerContract<TestLayer>()],
            scopeDefinitions: [ScopeDefinition<TestScope>()],
            messageContracts: [CallContract<TestCall, TestScope, TestResult>()],
            services:
            [
                Service<TestService>(
                    ownerScope: typeof(TestScope),
                    ownerLayers: [typeof(TestLayer)]),
                Service<OtherService>(
                    ownerScope: typeof(TestScope),
                    ownerLayers: [typeof(TestLayer)])
            ],
            handlers:
            [
                Handler<TestCall, TestService, TestScope>(ScopeMessageKind.Call),
                Handler<TestCall, OtherService, TestScope>(ScopeMessageKind.Call)
            ]);

        ModuleBuildException exception = Assert.Throws<ModuleBuildException>(
            () => ModuleRuntimeBuilder.Build(module))!;

        Assert.That(exception.Code, Is.EqualTo(ModuleBuildErrorCodes.CallMultipleHandlers));
        Assert.That(exception.Message, Does.Contain("multiple handlers"));
    }

    private static LayerContractContribution LayerContract<TLayer>()
    {
        return new LayerContractContribution(typeof(TLayer).TypeHandle);
    }

    private static ScopeDefinitionContribution ScopeDefinition<TScope>()
    {
        return new ScopeDefinitionContribution(
            typeof(TScope).TypeHandle,
            ScopeThreadingMode.Inline,
            ScopeClockMode.EngineDriven,
            0,
            ScopeStopPolicy.Drain);
    }

    private static ScopeMessageContractContribution CallContract<TMessage, TScope, TResult>()
    {
        return new ScopeMessageContractContribution(
            typeof(TMessage).TypeHandle,
            typeof(TScope).TypeHandle,
            typeof(TResult).TypeHandle,
            ScopeMessageKind.Call);
    }

    private static ScopeMessageContractContribution EventContract<TMessage, TScope>()
    {
        return new ScopeMessageContractContribution(
            typeof(TMessage).TypeHandle,
            typeof(TScope).TypeHandle,
            typeof(object).TypeHandle,
            ScopeMessageKind.Event);
    }

    private static ServiceContribution Service<TService>(
        Type ownerScope,
        Type[] ownerLayers)
        where TService : IService, new()
    {
        return new ServiceContribution(
            typeof(TService).TypeHandle,
            ownerLayers.Select(static layer => layer.TypeHandle).ToArray(),
            ownerScope.TypeHandle,
            static () => new TService(),
            static (_, _, _) => { },
            moduleLocalServiceId: 0);
    }

    private static ScopeHandlerContribution Handler<TMessage, TService, TScope>(ScopeMessageKind kind)
    {
        return new ScopeHandlerContribution(
            typeof(TMessage).TypeHandle,
            typeof(TService).TypeHandle,
            typeof(TScope).TypeHandle,
            moduleLocalHandlerId: 0,
            kind);
    }

    private sealed class TestModule : ILayerBaseModule, IDisposable
    {
        public TestModule(
            IReadOnlyList<LayerContractContribution>? layerContracts = null,
            IReadOnlyList<ScopeDefinitionContribution>? scopeDefinitions = null,
            IReadOnlyList<ScopeMessageContractContribution>? messageContracts = null,
            IReadOnlyList<ServiceContribution>? services = null,
            IReadOnlyList<ContextContribution>? contexts = null,
            IReadOnlyList<ScopeHandlerContribution>? handlers = null)
        {
            Manifest = new ModuleManifest(
                layerContracts ?? Array.Empty<LayerContractContribution>(),
                scopeDefinitions ?? Array.Empty<ScopeDefinitionContribution>(),
                messageContracts ?? Array.Empty<ScopeMessageContractContribution>(),
                services ?? Array.Empty<ServiceContribution>(),
                contexts ?? Array.Empty<ContextContribution>(),
                handlers ?? Array.Empty<ScopeHandlerContribution>());
        }

        public ModuleManifest Manifest { get; }

        public void Dispose()
        {
        }
    }

    private sealed class TestLayer : Layer
    {
    }

    private sealed class TestScope
    {
    }

    private sealed class OtherScope
    {
    }

    private readonly struct TestCall
    {
    }

    private readonly struct TestResult
    {
    }

    private readonly struct TestEvent
    {
    }

    private sealed class TestService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class OtherService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class OtherScopeService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }
}
