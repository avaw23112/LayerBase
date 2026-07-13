using LayerBase.DI;
using LayerBase.Scope;
using LayerBase.Scope.Resources;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeResourceGenerationTests
{
    [Test]
    public void Scope_runtime_must_not_reflectively_discover_generated_resource_contributions()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "LayerBase", "Scope", "ScopeRuntime.cs"));

        Assert.That(source, Does.Not.Contain("GeneratedScopeResourceContributions"));
        Assert.That(source, Does.Not.Contain("Assembly.GetType"));
        Assert.That(source, Does.Not.Contain("GetMethod("));
        Assert.That(source, Does.Not.Contain(".Invoke(null"));
        Assert.That(source, Does.Contain("ScopeResourceContributionRegistry.CollectFor"));
    }

    [Test]
    public void Scope_resources_must_not_fallback_to_reflection_binder()
    {
        string root = FindRepositoryRoot();
        string runtimeSource = File.ReadAllText(Path.Combine(root, "LayerBase", "Scope", "ScopeRuntime.cs"));
        string binderPath = Path.Combine(root, "LayerBase", "Scope", "ScopeResourceBinder.cs");

        Assert.That(File.Exists(binderPath), Is.False);
        Assert.That(runtimeSource, Does.Not.Contain("ScopeResourceBinder"));
    }

    [Test]
    public void Generated_resource_imports_require_provider_in_same_scope()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var runtime = new ScopeRuntime(
                ScopeDescriptors.Main,
                new IService[] { new GeneratedBoundaryResourceConsumer() });
            runtime.SetContexts(Array.Empty<ILayerContext>());
        })!;

        Assert.That(ex.Message, Does.Contain("could not find a published scope resource"));
    }

    [Test]
    public void Generated_resource_bindings_are_scope_local_and_clear_on_stop()
    {
        var provider = new GeneratedBoundaryResourceProvider();
        var consumer = new GeneratedBoundaryResourceConsumer();
        using var runtime = new ScopeRuntime(
            ScopeDescriptors.Main,
            new IService[] { provider, consumer });

        runtime.SetContexts(Array.Empty<ILayerContext>());

        Assert.That(provider, Is.InstanceOf<IGeneratedScopeResourcePublisher>());
        Assert.That(consumer, Is.InstanceOf<IGeneratedScopeResourceConsumer>());
        Assert.That(consumer.Values, Is.EqualTo(new[] { 3, 5, 8 }));

        runtime.Stop();

        Assert.That(consumer.HasBinding, Is.False);
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
}

internal sealed partial class GeneratedBoundaryResourceProvider : IService
{
    public const string ResourceKey = "boundary-values";

    [Provide(ResourceKey)]
    private readonly int[] _values = { 3, 5, 8 };

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

#pragma warning disable LBG403
internal sealed partial class GeneratedBoundaryResourceConsumer : IService
{
    [From(typeof(GeneratedBoundaryResourceProvider), GeneratedBoundaryResourceProvider.ResourceKey)]
    private IReadOnlyList<int>? _values;

    public IReadOnlyList<int> Values => _values ?? Array.Empty<int>();

    public bool HasBinding => _values != null;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}
#pragma warning restore LBG403
