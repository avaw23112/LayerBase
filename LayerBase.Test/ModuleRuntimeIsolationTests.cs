using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;
using System.Reflection;

namespace LayerBase.Test;

[TestFixture]
public sealed class ModuleRuntimeIsolationTests
{
    [Test]
    public void Scope_host_factory_is_resolved_per_runtime_without_global_static_registry()
    {
        string root = FindRepositoryRoot();
        string factoryPath = Path.Combine(root, "LayerBase", "Scope", "ScopeHostFactory.cs");
        string runtimeSource = File.ReadAllText(Path.Combine(root, "LayerBase", "Application", "LayerRuntime.cs"));
        string hubSource = File.ReadAllText(Path.Combine(root, "LayerBase", "Application", "LayerHub.cs"));
        string generatorSource = File.ReadAllText(Path.Combine(
            root,
            "LayerBase.Generator",
            "LayerBase.Generator",
            "ScopeRuntimeHostGenerator.cs"));

        Assert.That(File.Exists(factoryPath), Is.False);
        Assert.That(runtimeSource, Does.Not.Contain("ScopeHostFactory.TryCreate"));
        Assert.That(hubSource, Does.Not.Contain("ScopeHostFactory.Reset"));
        Assert.That(generatorSource, Does.Not.Contain("ScopeHostFactory.Register"));
        Assert.That(generatorSource, Does.Contain("CreateScopeHostFactory"));
    }

    [Test]
    public void Module_dispatchers_are_not_resolved_from_global_static_registry()
    {
        string root = FindRepositoryRoot();
        string dispatchRegistryPath = Path.Combine(root, "LayerBase", "Scope", "ModuleDispatchRegistry.cs");
        string runtimeSource = File.ReadAllText(Path.Combine(root, "LayerBase", "Application", "LayerRuntime.cs"));

        Assert.That(File.Exists(dispatchRegistryPath), Is.False);
        Assert.That(runtimeSource, Does.Not.Contain("ModuleDispatchRegistry"));
        Assert.That(runtimeSource, Does.Not.Contain("TryGetCallDispatchers"));
        Assert.That(runtimeSource, Does.Not.Contain("TryGetEventDispatchers"));
    }

    [Test]
    public void Module_catalog_is_created_explicitly_without_global_static_registry()
    {
        string root = FindRepositoryRoot();
        string catalogRegistryPath = Path.Combine(root, "LayerBase", "Scope", "ModuleCatalogRegistry.cs");
        string runtimeSource = File.ReadAllText(Path.Combine(root, "LayerBase", "Application", "LayerRuntime.cs"));
        string generatorSource = File.ReadAllText(Path.Combine(
            root,
            "LayerBase.Generator",
            "LayerBase.Generator",
            "ModuleCatalogGenerator.cs"));

        Assert.That(File.Exists(catalogRegistryPath), Is.False);
        Assert.That(runtimeSource, Does.Not.Contain("ModuleCatalogRegistry"));
        Assert.That(generatorSource, Does.Not.Contain("ModuleCatalogRegistry"));
        Assert.That(generatorSource, Does.Not.Contain(".Register(Create())"));
        Assert.That(generatorSource, Does.Contain("GeneratedModuleCatalog"));
        Assert.That(generatorSource, Does.Contain("Create()"));
    }

    [Test]
    public void Layer_runtime_business_subsystems_are_not_public_api()
    {
        const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        Assert.That(typeof(LayerRuntime).GetProperty("EventCenter", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetProperty("ServiceProvider", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetMethod("GetService", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetProperty("Scheduler", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetProperty("Timer", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetProperty("EcsWorld", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetProperty("EcsQueryRegistry", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetProperty("EcsScheduler", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetMethod("Send", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetMethod("Post", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetMethod("TryPost", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetMethod("MarkDirty", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetMethod("PostLatest", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetMethod("PostFromAnyThread", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetMethod("TryPostFromAnyThread", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetMethod("PostCoalesced", PublicInstance), Is.Null);
        Assert.That(typeof(LayerRuntime).GetMethod("SchedulePost", PublicInstance), Is.Null);
    }

    [Test]
    public void Layer_business_service_and_event_methods_are_not_public_api()
    {
        const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        Assert.That(typeof(Layer).GetMethod("RegisterService", PublicInstance, [typeof(IService)]), Is.Null);
        Assert.That(typeof(Layer).GetMethod("RegisterService", PublicInstance, [typeof(Type), typeof(IService)]), Is.Null);
        Assert.That(typeof(Layer).GetMethod("GetService", PublicInstance), Is.Null);
        Assert.That(typeof(Layer).GetMethod("Send", PublicInstance), Is.Null);
        Assert.That(typeof(Layer).GetMethod("Post", PublicInstance), Is.Null);
        Assert.That(typeof(Layer).GetMethod("TryPost", PublicInstance), Is.Null);
        Assert.That(typeof(Layer).GetMethod("RecordSubscribedEvent", PublicInstance), Is.Null);
        Assert.That(typeof(Layer).GetMethod("RecordProducedEvent", PublicInstance), Is.Null);
    }

    [Test]
    public void Layer_does_not_reflectively_bind_interface_event_handlers()
    {
        string root = FindRepositoryRoot();
        string layerSource = File.ReadAllText(Path.Combine(root, "LayerBase", "Layer", "Layer.cs"));

        Assert.That(layerSource, Does.Not.Contain("BindInterfaceEventHandlers"));
        Assert.That(layerSource, Does.Not.Contain("GetInterfaces()"));
        Assert.That(layerSource, Does.Not.Contain("GetGenericTypeDefinition"));
        Assert.That(layerSource, Does.Contain("IAutoSubscribe"));
    }

    [Test]
    public void Module_catalog_rejects_service_when_scope_definition_module_is_not_installed()
    {
        using var module = new IsolationModule(
            layerContracts: [LayerContract<IsolationLayer>()],
            services:
            [
                Service<IsolationService>(
                    ownerScope: typeof(IsolationScope),
                    ownerLayers: [typeof(IsolationLayer)])
            ]);

        ModuleBuildException ex = Assert.Throws<ModuleBuildException>(() => ModuleRuntimeBuilder.Build(module))!;

        Assert.That(ex.Code, Is.EqualTo(ModuleBuildErrorCodes.MissingScopeDefinition));
    }

    [Test]
    public void Layer_runtime_does_not_fallback_after_module_build_failure()
    {
        using var module = new IsolationModule(
            layerContracts: [LayerContract<IsolationLayer>()],
            services:
            [
                Service<IsolationService>(
                    ownerScope: typeof(IsolationScope),
                    ownerLayers: [typeof(IsolationLayer)])
            ]);

        ModuleBuildException ex = Assert.Throws<ModuleBuildException>(() =>
            LayerHub.CreateLayers()
                .Push(new IsolationLayer())
                .Install(module)
                .Build())!;

        Assert.That(ex.Code, Is.EqualTo(ModuleBuildErrorCodes.MissingScopeDefinition));
    }

    [Test]
    public void Module_catalog_rejects_handler_when_message_targets_different_scope()
    {
        using var module = new IsolationModule(
            layerContracts: [LayerContract<IsolationLayer>()],
            scopeDefinitions:
            [
                ScopeDefinition<IsolationScope>(),
                ScopeDefinition<OtherIsolationScope>()
            ],
            messageContracts: [EventContract<IsolationEvent, IsolationScope>()],
            services:
            [
                Service<IsolationService>(
                    ownerScope: typeof(OtherIsolationScope),
                    ownerLayers: [typeof(IsolationLayer)])
            ],
            handlers:
            [
                Handler<IsolationEvent, IsolationService, OtherIsolationScope>(ScopeMessageKind.Event)
            ]);

        ModuleBuildException ex = Assert.Throws<ModuleBuildException>(() => ModuleRuntimeBuilder.Build(module))!;

        Assert.That(ex.Code, Is.EqualTo(ModuleBuildErrorCodes.HandlerScopeMismatch));
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

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "LayerBase.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed class IsolationModule : ILayerBaseModule, IDisposable
    {
        public IsolationModule(
            IReadOnlyList<LayerContractContribution>? layerContracts = null,
            IReadOnlyList<ScopeDefinitionContribution>? scopeDefinitions = null,
            IReadOnlyList<ScopeMessageContractContribution>? messageContracts = null,
            IReadOnlyList<ServiceContribution>? services = null,
            IReadOnlyList<ScopeHandlerContribution>? handlers = null)
        {
            Manifest = new ModuleManifest(
                layerContracts ?? Array.Empty<LayerContractContribution>(),
                scopeDefinitions ?? Array.Empty<ScopeDefinitionContribution>(),
                messageContracts ?? Array.Empty<ScopeMessageContractContribution>(),
                services ?? Array.Empty<ServiceContribution>(),
                Array.Empty<ContextContribution>(),
                handlers ?? Array.Empty<ScopeHandlerContribution>());
        }

        public ModuleManifest Manifest { get; }

        public void Dispose()
        {
        }
    }

    private sealed class IsolationLayer : Layer
    {
    }

    private sealed class IsolationScope
    {
    }

    private sealed class OtherIsolationScope
    {
    }

    private readonly struct IsolationEvent
    {
    }

    private sealed class IsolationService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }
}
