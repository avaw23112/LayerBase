using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeServiceIsolationTests
{
    private readonly List<string> _disposeLog = new();

    [SetUp]
    public void SetUp()
    {
        _disposeLog.Clear();
        LayerHub.Reset();
    }

    [Test]
    public void Cross_scope_constructor_dependency_is_rejected()
    {
        var layer = new CrossScopeDependencyLayer();

        Assert.That(
            () => LayerHub.CreateLayers()
                          .Push(layer)
                          .Build(),
            Throws.InvalidOperationException
                  .With.Message.Contains(nameof(IMainOnlyService)));
    }

    [Test]
    public void Service_provider_contains_no_shared_scope_instance_cache()
    {
        string source = File.ReadAllText(FindRepositoryFile("LayerBase", "DI", "ServiceProvider.cs"));

        Assert.That(source, Does.Not.Contain("ConcurrentDictionary<ServiceKey, Lazy<object>>"));
        Assert.That(source, Does.Not.Contain("OwnedDisposableRegistry"));
        Assert.That(source, Does.Not.Contain("_lifetimeGate"));
        Assert.That(source, Does.Not.Contain("ScopeDefinitionIds.Main, serviceType"));
    }

    [Test]
    public void Service_provider_root_does_not_depend_on_runtime_or_layer()
    {
        string source = File.ReadAllText(FindRepositoryFile("LayerBase", "DI", "ServiceProvider.cs"));

        Assert.That(source, Does.Not.Contain("LayerRuntime"));
        Assert.That(source, Does.Not.Contain("Layer _ownerLayer"));
        Assert.That(source, Does.Not.Contain("ScopeRuntimeHost"));
    }

    [Test]
    public void Scope_service_provider_is_not_child_of_root_provider()
    {
        string source = File.ReadAllText(FindRepositoryFile("LayerBase", "DI", "ScopeServiceProvider.cs"));

        Assert.That(source, Does.Not.Contain("ServiceProvider _root"));
        Assert.That(source, Does.Not.Contain("_root.Resolve"));
        Assert.That(source, Does.Not.Contain("_root.CreateInstance"));
    }

    [Test]
    public void Same_instance_cannot_be_owned_by_two_scopes()
    {
        var instance = new SharedOwnershipService();
        var layer = new SharedOwnershipLayer(instance);

        Assert.That(
            () => LayerHub.CreateLayers()
                          .Push(layer)
                          .Build(),
            Throws.InvalidOperationException
                  .With.Message.Contains("already bound"));
    }

    [Test]
    public void AsyncDisposable_only_service_is_rejected()
    {
        var layer = new AsyncDisposableOnlyLayer();

        Assert.That(
            () => LayerHub.CreateLayers()
                          .Push(layer)
                          .Build(),
            Throws.InvalidOperationException
                  .With.Message.Contains("IAsyncDisposable"));
    }

    [Test]
    public void Resources_release_in_reverse_creation_order()
    {
        var runtime = LayerHub.CreateLayers()
                              .Push(new ReverseDisposalLayer(_disposeLog))
                              .Build();

        Assert.That(runtime.ScopeHost.TryGetRuntime(SecondaryScope.ScopeId, out var secondary), Is.True);
        secondary.Dispose();

        Assert.That(_disposeLog, Is.EqualTo(new[] { "second", "first" }));
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        var current = TestContext.CurrentContext.TestDirectory;
        while (current != null)
        {
            var candidate = Path.Combine(new[] { current }.Concat(segments).ToArray());
            if (File.Exists(candidate))
                return candidate;

            current = Directory.GetParent(current)?.FullName;
        }

        throw new FileNotFoundException(string.Join(Path.DirectorySeparatorChar, segments));
    }

    private sealed class CrossScopeDependencyLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: SecondaryScope.ScopeId,
                    identity: "scope:test:CrossScopeSecondary",
                    scopeType: typeof(SecondaryScope),
                    factory: static () => new SecondaryScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMainOnlyService, MainOnlyService>();

            using var _ = ((ServiceCollection)services).PushRegistrationScope(
                registrationScopeId: 0,
                ownerScopeId: SecondaryScope.ScopeId);
            services.AddSingleton<SecondaryDependsOnMainService, SecondaryDependsOnMainService>();
        }
    }

    private interface IMainOnlyService
    {
    }

    private sealed class MainOnlyService : IService, IMainOnlyService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class SecondaryDependsOnMainService : IService
    {
        public SecondaryDependsOnMainService()
        {
        }

        public SecondaryDependsOnMainService(IMainOnlyService main)
        {
            _ = main;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class SecondaryScope : IScopeDefinition
    {
        public const int ScopeId = 818;
        public ScopeOptions Options => ScopeOptions.Inline;
    }

    private sealed class SharedOwnershipLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly SharedOwnershipService _instance;

        public SharedOwnershipLayer(SharedOwnershipService instance)
        {
            _instance = instance;
        }

        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: SecondaryScope.ScopeId,
                    identity: "scope:test:SharedOwnershipSecondary",
                    scopeType: typeof(SecondaryScope),
                    factory: static () => new SecondaryScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(typeof(SharedOwnershipService), _instance, typeof(MainScope));
            RegisterService(typeof(ISharedOwnershipAlias), _instance, typeof(SecondaryScope));
        }
    }

    private interface ISharedOwnershipAlias
    {
    }

    private sealed class SharedOwnershipService : IService, ISharedOwnershipAlias
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class AsyncDisposableOnlyLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: SecondaryScope.ScopeId,
                    identity: "scope:test:AsyncDisposableSecondary",
                    scopeType: typeof(SecondaryScope),
                    factory: static () => new SecondaryScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(
                typeof(AsyncDisposableOnlyService),
                new AsyncDisposableOnlyService(),
                typeof(SecondaryScope));
        }
    }

    private sealed class AsyncDisposableOnlyService : IService, IAsyncDisposable
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ReverseDisposalLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly List<string> _log;

        public ReverseDisposalLayer(List<string> log)
        {
            _log = log;
        }

        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: SecondaryScope.ScopeId,
                    identity: "scope:test:ReverseDisposalSecondary",
                    scopeType: typeof(SecondaryScope),
                    factory: static () => new SecondaryScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(
                typeof(FirstDisposableService),
                new FirstDisposableService(_log),
                typeof(SecondaryScope));
            RegisterService(
                typeof(SecondDisposableService),
                new SecondDisposableService(_log),
                typeof(SecondaryScope));
        }
    }

    private sealed class FirstDisposableService : IService, IDisposable
    {
        private readonly List<string> _log;

        public FirstDisposableService(List<string> log)
        {
            _log = log;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Dispose()
        {
            _log.Add("first");
        }
    }

    private sealed class SecondDisposableService : IService, IDisposable
    {
        private readonly List<string> _log;

        public SecondDisposableService(List<string> log)
        {
            _log = log;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Dispose()
        {
            _log.Add("second");
        }
    }
}
