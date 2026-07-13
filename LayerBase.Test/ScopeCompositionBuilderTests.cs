using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;
using LayerBase.Scope.Resources;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeCompositionBuilderTests
{
    [Test]
    public void Build_rejects_scope_definition_without_scope_id()
    {
        ModuleRuntimeCatalog catalog = CreateCatalog(
            scopeDefinitions: new Dictionary<RuntimeTypeHandle, ScopeDefinitionContribution>
            {
                [typeof(TestScope).TypeHandle] = ScopeDefinition<TestScope>()
            },
            scopeIds: new Dictionary<RuntimeTypeHandle, int>());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ScopeCompositionBuilder.Build(catalog))!;

        Assert.That(exception.Message, Does.Contain("missing scope id"));
    }

    [Test]
    public void Build_rejects_service_factory_returning_null()
    {
        ServiceContribution service = new(
            typeof(TestService).TypeHandle,
            new[] { typeof(TestLayer).TypeHandle },
            typeof(TestScope).TypeHandle,
            static () => null,
            static (_, _, _) => { },
            moduleLocalServiceId: 0);

        ModuleRuntimeCatalog catalog = CreateCatalog(
            scopeDefinitions: new Dictionary<RuntimeTypeHandle, ScopeDefinitionContribution>
            {
                [typeof(TestScope).TypeHandle] = ScopeDefinition<TestScope>()
            },
            services: new[] { service },
            scopeIds: new Dictionary<RuntimeTypeHandle, int>
            {
                [typeof(TestScope).TypeHandle] = 1
            },
            serviceSlots: new Dictionary<RuntimeTypeHandle, int>
            {
                [typeof(TestService).TypeHandle] = 0
            });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ScopeCompositionBuilder.Build(catalog))!;

        Assert.That(exception.Message, Does.Contain("factory returned null"));
    }

    private static ModuleRuntimeCatalog CreateCatalog(
        IReadOnlyDictionary<RuntimeTypeHandle, ScopeDefinitionContribution>? scopeDefinitions = null,
        IReadOnlyList<ServiceContribution>? services = null,
        IReadOnlyDictionary<RuntimeTypeHandle, int>? scopeIds = null,
        IReadOnlyDictionary<RuntimeTypeHandle, int>? serviceSlots = null)
    {
        return new ModuleRuntimeCatalog(
            Array.Empty<ILayerBaseModule>(),
            new Dictionary<ILayerBaseModule, int>(),
            new Dictionary<RuntimeTypeHandle, LayerContractContribution>(),
            scopeDefinitions ?? new Dictionary<RuntimeTypeHandle, ScopeDefinitionContribution>(),
            new Dictionary<RuntimeTypeHandle, ScopeMessageContractContribution>(),
            services ?? Array.Empty<ServiceContribution>(),
            Array.Empty<ContextContribution>(),
            Array.Empty<ScopeHandlerContribution>(),
            Array.Empty<ScopeResourceExportContribution>(),
            Array.Empty<ScopeResourceImportContribution>(),
            scopeIds ?? new Dictionary<RuntimeTypeHandle, int>(),
            serviceSlots ?? new Dictionary<RuntimeTypeHandle, int>(),
            new Dictionary<RuntimeTypeHandle, int>(),
            Array.Empty<ScopeCallRoute>(),
            Array.Empty<ScopeEventRoute>(),
            Array.Empty<ScopeEventHandlerRoute>());
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

    private sealed class TestScope
    {
    }

    private sealed class TestLayer : Layer
    {
    }

    private sealed class TestService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }
}
