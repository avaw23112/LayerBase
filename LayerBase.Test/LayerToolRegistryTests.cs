using LayerBase.Tooling;
using LayerBase.Layers;

namespace LayerBase.Test;

[TestFixture]
public sealed class LayerToolRegistryTests
{
    [Test]
    public void Create_resolves_by_implementation_and_contract_key()
    {
        var registry = new LayerToolRegistry();

        registry.Register<ITestTool, TestTool>(
            toolId: "ui.view",
            key: "Inventory",
            path: "UI/Inventory",
            cache: false,
            ownerLayerType: typeof(ToolTestLayer),
            ownerServiceType: null,
            ownerManagerType: typeof(ToolManager),
            factory: static _ => new TestTool());

        var concrete = registry.Create<TestTool>();
        var contract = registry.Create<ITestTool>("Inventory");
        var entry = registry.GetEntry<ITestTool>("Inventory");

        Assert.That(concrete, Is.TypeOf<TestTool>());
        Assert.That(contract, Is.TypeOf<TestTool>());
        Assert.That(entry.ContractType, Is.EqualTo(typeof(ITestTool)));
        Assert.That(entry.ImplementationType, Is.EqualTo(typeof(TestTool)));
        Assert.That(entry.ToolId, Is.EqualTo("ui.view"));
        Assert.That(entry.Key, Is.EqualTo("Inventory"));
        Assert.That(entry.Path, Is.EqualTo("UI/Inventory"));
        Assert.That(entry.OwnerLayerType, Is.EqualTo(typeof(ToolTestLayer)));
        Assert.That(entry.OwnerServiceType, Is.Null);
        Assert.That(entry.OwnerManagerType, Is.EqualTo(typeof(ToolManager)));
    }

    [Test]
    public void GetOrCreate_returns_cached_instance_when_cache_is_enabled()
    {
        var registry = new LayerToolRegistry();

        registry.Register<ITestTool, TestTool>(
            key: "Inventory",
            path: null,
            cache: true,
            factory: static _ => new TestTool());

        var first = registry.GetOrCreate<ITestTool>("Inventory");
        var second = registry.GetOrCreate<TestTool>();

        Assert.That(second, Is.SameAs(first));
        Assert.That(registry.GetEntry<TestTool>().HasCache, Is.True);
        Assert.That(registry.GetEntry<TestTool>().HasCachedValue, Is.True);
    }

    [Test]
    public void GetOrCreate_creates_new_instance_when_cache_is_disabled()
    {
        var registry = new LayerToolRegistry();

        registry.Register<ITestTool, TestTool>(
            key: "Inventory",
            path: null,
            cache: false,
            factory: static _ => new TestTool());

        var first = registry.GetOrCreate<ITestTool>("Inventory");
        var second = registry.GetOrCreate<ITestTool>("Inventory");

        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(registry.GetEntry<TestTool>().HasCache, Is.False);
        Assert.That(registry.GetCachedEntries(), Is.Empty);
    }

    [Test]
    public void TryCreate_returns_false_when_key_is_missing()
    {
        var registry = new LayerToolRegistry();

        var created = registry.TryCreate<ITestTool>("Missing", out var value);
        var cached = registry.TryGetOrCreate<ITestTool>("Missing", out var cachedValue);
        var entry = registry.TryGetEntry<ITestTool>("Missing", out var missingEntry);
        var implementationEntry = registry.TryGetEntry<TestTool>(out var missingImplementationEntry);

        Assert.That(created, Is.False);
        Assert.That(value, Is.Null);
        Assert.That(cached, Is.False);
        Assert.That(cachedValue, Is.Null);
        Assert.That(entry, Is.False);
        Assert.That(missingEntry, Is.Null);
        Assert.That(implementationEntry, Is.False);
        Assert.That(missingImplementationEntry, Is.Null);
    }

    [Test]
    public void Create_throws_layer_tool_exception_when_entry_is_missing()
    {
        var registry = new LayerToolRegistry();

        Assert.Throws<LayerToolException>(() => registry.Create<TestTool>());
        Assert.Throws<LayerToolException>(() => registry.Create<ITestTool>("Missing"));
    }

    [Test]
    public void Layer_runtime_builder_applies_tool_configuration_during_build()
    {
        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(new ToolTestLayer())
            .ConfigureTools(static registry =>
            {
                registry.Register<ITestTool, TestTool>(
                    key: "Inventory",
                    path: null,
                    cache: true,
                    factory: static _ => new TestTool());
            })
            .Build();

        var value = runtime.Tools.GetOrCreate<ITestTool>("Inventory");

        Assert.That(value, Is.TypeOf<TestTool>());
    }

    [Test]
    public void Query_and_cache_management_apis_return_expected_entries()
    {
        var registry = new LayerToolRegistry();

        registry.Register<ITestTool, TestTool>(
            toolId: "ui.view",
            key: "Inventory",
            path: "UI/Inventory",
            cache: true,
            ownerLayerType: typeof(ToolTestLayer),
            ownerServiceType: null,
            ownerManagerType: typeof(ToolManager),
            factory: static _ => new TestTool());

        registry.Register<ITestTool, SecondaryTool>(
            toolId: "ui.view",
            key: "Settings",
            path: null,
            cache: true,
            ownerLayerType: null,
            ownerServiceType: typeof(ToolService),
            ownerManagerType: null,
            factory: static _ => new SecondaryTool());

        _ = registry.GetOrCreate<ITestTool>("Inventory");
        _ = registry.GetOrCreate<ITestTool>("Settings");

        Assert.That(registry.GetEntries(), Has.Count.EqualTo(2));
        Assert.That(registry.GetEntries<ITestTool>(), Has.Count.EqualTo(2));
        Assert.That(registry.GetEntriesByToolId("ui.view"), Has.Count.EqualTo(2));
        Assert.That(registry.GetCachedEntries(), Has.Count.EqualTo(2));
        Assert.That(registry.TryGetEntry<TestTool>(out var entry), Is.True);
        Assert.That(entry, Is.Not.Null);

        registry.ClearCache<ITestTool>("Inventory");

        Assert.That(registry.GetEntry<TestTool>().HasCachedValue, Is.False);
        Assert.That(registry.GetEntry<SecondaryTool>().HasCachedValue, Is.True);

        registry.ClearAllCaches();

        Assert.That(registry.GetCachedEntries(), Is.Empty);
    }

    [Test]
    public void Diagnostics_report_contains_registered_entry_metadata()
    {
        var registry = new LayerToolRegistry();

        registry.Register<ITestTool, TestTool>(
            toolId: "ui.view",
            key: "Inventory",
            path: "UI/Inventory",
            cache: true,
            ownerLayerType: typeof(ToolTestLayer),
            ownerServiceType: typeof(ToolService),
            ownerManagerType: typeof(ToolManager),
            factory: static _ => new TestTool());

        _ = registry.GetOrCreate<TestTool>();

        var report = registry.CreateDiagnosticsReport();

        Assert.That(report.Entries, Has.Count.EqualTo(1));
        Assert.That(report.CachedEntryCount, Is.EqualTo(1));
        Assert.That(report.Entries[0].ToolId, Is.EqualTo("ui.view"));
        Assert.That(report.Entries[0].ImplementationType, Is.EqualTo(typeof(TestTool)));
        Assert.That(report.Entries[0].OwnerLayerType, Is.EqualTo(typeof(ToolTestLayer)));
        Assert.That(report.Warnings, Is.Empty);
    }

    private interface ITestTool
    {
    }

    private sealed class TestTool : ITestTool
    {
    }

    private sealed class SecondaryTool : ITestTool
    {
    }

    private sealed class ToolTestLayer : Layer
    {
    }

    private sealed class ToolService
    {
    }

    private sealed class ToolManager
    {
    }
}
