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
            key: "Inventory",
            path: "UI/Inventory",
            cache: false,
            factory: static _ => new TestTool());

        var concrete = registry.Create<TestTool>();
        var contract = registry.Create<ITestTool>("Inventory");
        var entry = registry.GetEntry<ITestTool>("Inventory");

        Assert.That(concrete, Is.TypeOf<TestTool>());
        Assert.That(contract, Is.TypeOf<TestTool>());
        Assert.That(entry.ContractType, Is.EqualTo(typeof(ITestTool)));
        Assert.That(entry.ImplementationType, Is.EqualTo(typeof(TestTool)));
        Assert.That(entry.Key, Is.EqualTo("Inventory"));
        Assert.That(entry.Path, Is.EqualTo("UI/Inventory"));
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
    }

    [Test]
    public void TryCreate_returns_false_when_key_is_missing()
    {
        var registry = new LayerToolRegistry();

        var created = registry.TryCreate<ITestTool>("Missing", out var value);
        var cached = registry.TryGetOrCreate<ITestTool>("Missing", out var cachedValue);
        var entry = registry.TryGetEntry<ITestTool>("Missing", out var missingEntry);

        Assert.That(created, Is.False);
        Assert.That(value, Is.Null);
        Assert.That(cached, Is.False);
        Assert.That(cachedValue, Is.Null);
        Assert.That(entry, Is.False);
        Assert.That(missingEntry, Is.Null);
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

    private interface ITestTool
    {
    }

    private sealed class TestTool : ITestTool
    {
    }

    private sealed class ToolTestLayer : Layer
    {
    }
}
