using LayerBase.DI;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeResourceBindingTests
{
    [Test]
    public void Same_scope_publish_and_from_bind_scope_read_capability()
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
    public void From_requires_scope_read_and_rejects_direct_resource_access()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new ScopeRuntime(
            ScopeDescriptors.Main,
            new IService[]
            {
                new ScopeResourcePublisherService(),
                new DirectResourceConsumerService()
            }).SetContexts([
                new PlayerStorageContext(),
                new DirectConsumerContext()
            ]))!;

        Assert.That(ex.Message, Does.Contain("ScopeRead"));
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
    public void Scope_read_rejects_access_outside_owner_scope()
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
    public void Scope_read_rejects_access_after_scope_stops()
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

        using (ScopeExecution.Enter(runtime))
        {
            Assert.Throws<ScopeResourceClosedException>(() => consumer.Count());
        }

        runtime.Dispose();
    }

    [Test]
    public void Duplicate_publishers_in_same_scope_fail_build()
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
    public void Missing_publisher_fails_build()
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
        [Publish("players")]
        private readonly List<int> _players = new();

        public void Add(int playerId)
        {
            _players.Add(playerId);
        }
    }

    private sealed class PlayerQueryContext : ILayerContext
    {
        [From(typeof(PlayerStorageContext), "players")]
        private ScopeRead<IReadOnlyList<int>> _players = default!;

        public int Count()
        {
            return _players.Value.Count;
        }

        public bool Contains(int playerId)
        {
            return _players.Value.Contains(playerId);
        }
    }

    private sealed class DirectConsumerContext : ILayerContext
    {
        [From(typeof(PlayerStorageContext), "players")]
        private IReadOnlyList<int> _players = default!;
    }

    private sealed class DuplicatePublisherContext : ILayerContext
    {
        [Publish("duplicate")]
        private readonly Dictionary<int, int> _state = new();
    }

    private sealed class MissingPublisherConsumerContext : ILayerContext
    {
        [From(typeof(PlayerStorageContext), "missing")]
        private ScopeRead<IReadOnlyList<int>> _players = default!;
    }
}
