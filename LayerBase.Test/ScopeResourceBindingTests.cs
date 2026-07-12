using LayerBase.DI;
using LayerBase.Scope;
using LayerBase.Scope.Resources;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeResourceBindingTests
{
    [Test]
    public void Same_scope_provide_and_from_bind_direct_resource()
    {
        using var runtime = new ScopeRuntime(
            ScopeDescriptors.Main,
            new IService[]
            {
                new ScopeResourcePublisherService(),
                new ScopeResourceConsumerService()
            });
        runtime.SetContexts([
            new PlayerStorageContext(),
            new PlayerQueryContext()
        ]);

        var publisher = runtime.Contexts.OfType<PlayerStorageContext>().Single();
        var consumer = runtime.Contexts.OfType<PlayerQueryContext>().Single();

        publisher.Add(7);
        publisher.Add(9);

        using (ScopeExecution.Enter(runtime))
        {
            Assert.That(consumer.Count(), Is.EqualTo(2));
            Assert.That(consumer.Contains(9), Is.True);
        }
    }

    [Test]
    public void From_binds_direct_resource_type_successfully()
    {
        using var runtime = new ScopeRuntime(
            ScopeDescriptors.Main,
            new IService[]
            {
                new ScopeResourcePublisherService(),
                new DirectResourceConsumerService()
            });
        runtime.SetContexts([
            new PlayerStorageContext(),
            new DirectConsumerContext()
        ]);

        var consumer = runtime.Contexts.OfType<DirectConsumerContext>().Single();
        Assert.That(consumer.HasResource, Is.True);
    }

    [Test]
    public void Cross_scope_from_cannot_read_another_scope_resource()
    {
        using var publisher = new ScopeRuntime(
            ScopeDescriptors.Main,
            new IService[] { new ScopeResourcePublisherService() });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var consumer = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1,
                name: "CombatScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { new ScopeResourceConsumerService() });
            consumer.SetContexts([new PlayerQueryContext()]);
        })!;
        Assert.That(ex.Message, Does.Contain("could not find a published scope resource"));
    }

    [Test]
    public void Access_outside_owner_scope_throws()
    {
        using var runtime = new ScopeRuntime(
            ScopeDescriptors.Main,
            new IService[]
            {
                new ScopeResourcePublisherService(),
                new ScopeResourceConsumerService()
            });
        runtime.SetContexts([
            new PlayerStorageContext(),
            new PlayerQueryContext()
        ]);

        var consumer = runtime.Contexts.OfType<PlayerQueryContext>().Single();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => consumer.Count())!;
        Assert.That(ex.Message, Does.Contain("owner scope execution context"));
    }

    [Test]
    public void Resource_is_unbound_after_scope_stops()
    {
        var runtime = new ScopeRuntime(
            ScopeDescriptors.Main,
            new IService[]
            {
                new ScopeResourcePublisherService(),
                new ScopeResourceConsumerService()
            });
        runtime.SetContexts([
            new PlayerStorageContext(),
            new PlayerQueryContext()
        ]);

        var consumer = runtime.Contexts.OfType<PlayerQueryContext>().Single();
        runtime.Stop();

        Assert.That(consumer.HasResource, Is.False);

        runtime.Dispose();
    }

    [Test]
    public void Duplicate_providers_in_same_scope_fail_build()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new ScopeRuntime(
            ScopeDescriptors.Main,
            new IService[]
            {
                new DuplicatePublisherServiceA(),
                new DuplicatePublisherServiceB()
            }).SetContexts([
                new DuplicatePublisherContext(),
                new DuplicatePublisherContext()
            ]))!;

        Assert.That(ex.Message, Does.Contain("Scope resource provider conflict"));
    }

    [Test]
    public void Missing_provider_fails_build()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new ScopeRuntime(
            ScopeDescriptors.Main,
            new IService[] { new MissingPublisherConsumerService() }).SetContexts([new MissingPublisherConsumerContext()]))!;

        Assert.That(ex.Message, Does.Contain("could not find a published scope resource"));
    }

    private sealed class ScopeResourcePublisherService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<PlayerStorageContext, PlayerStorageContext>();
        }
    }

    private sealed class ScopeResourceConsumerService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<PlayerQueryContext, PlayerQueryContext>();
        }
    }

    private sealed class DirectResourceConsumerService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<DirectConsumerContext, DirectConsumerContext>();
        }
    }

    private sealed class DuplicatePublisherServiceA : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<DuplicatePublisherContext, DuplicatePublisherContext>();
        }
    }

    private sealed class DuplicatePublisherServiceB : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<DuplicatePublisherContext, DuplicatePublisherContext>();
        }
    }

    private sealed class MissingPublisherConsumerService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<MissingPublisherConsumerContext, MissingPublisherConsumerContext>();
        }
    }

    private sealed class PlayerStorageContext : ILayerContext
    {
        [Provide("players")]
        private readonly List<int> _players = new();

        public void Add(int playerId)
        {
            _players.Add(playerId);
        }
    }

    private sealed class PlayerQueryContext : ILayerContext, IScopeObjectBindingAccessor, IDisposable
    {
        public ScopeObjectBinding? __ScopeObjectBinding { get; set; }

        [From(typeof(PlayerStorageContext), "players")]
        private IReadOnlyList<int>? _players;

        public bool HasResource => _players != null;

        public int Count()
        {
            RequireAccess();
            return _players?.Count ?? 0;
        }

        public bool Contains(int playerId)
        {
            RequireAccess();
            return _players?.Contains(playerId) ?? false;
        }

        public void Dispose()
        {
            _players = null;
        }

        private void RequireAccess()
        {
            if (__ScopeObjectBinding == null)
                return;
            if (!ReferenceEquals(ScopeExecution.Current.Runtime, __ScopeObjectBinding.Scope))
                throw new InvalidOperationException(
                    $"Scope '{__ScopeObjectBinding.Scope.Descriptor.Name}' local API must be called from its owner scope execution context.");
        }
    }

    private sealed class DirectConsumerContext : ILayerContext, IScopeObjectBindingAccessor
    {
        public ScopeObjectBinding? __ScopeObjectBinding { get; set; }

        [From(typeof(PlayerStorageContext), "players")]
        private IReadOnlyList<int>? _players;

        public bool HasResource => _players != null;
    }

    private sealed class DuplicatePublisherContext : ILayerContext
    {
        [Provide("duplicate")]
        private readonly Dictionary<int, int> _state = new();
    }

    private sealed class MissingPublisherConsumerContext : ILayerContext
    {
        [From(typeof(PlayerStorageContext), "missing")]
        private IReadOnlyList<int>? _players;
    }
}
