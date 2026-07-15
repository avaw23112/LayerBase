using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;

namespace EventsTest;

public partial class ServiceRegistrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Singleton_service_is_resolved_correctly()
    {
        var layer = new DemoLayer();
        layer.RegisterService(new DemoServiceModule());
        LayerHub.CreateLayers().Push(layer).Build();

        var service = layer.GetService<IDemoService>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<DemoService>());
    }

    [Test]
    public void Multiple_layers_have_isolated_services()
    {
        var layer1 = new DemoLayer();
        layer1.RegisterService(new DemoServiceModule());

        var layer2 = new DemoLayer();
        layer2.RegisterService(new DemoServiceModule());

        LayerHub.CreateLayers().Push(layer1).Push(layer2).Build();

        var s1 = layer1.GetService<IDemoService>();
        var s2 = layer2.GetService<IDemoService>();

        Assert.That(s1, Is.Not.Null);
        Assert.That(s2, Is.Not.Null);
        Assert.That(ReferenceEquals(s1, s2), Is.False);
    }

    [Test]
    public void Singleton_service_is_isolated_per_layer_provider()
    {
        var layer1 = new DemoLayer();
        layer1.RegisterService(new SingletonServiceModule());

        var layer2 = new DemoLayer();
        layer2.RegisterService(new SingletonServiceModule());

        LayerHub.CreateLayers().Push(layer1).Push(layer2).Build();

        Assert.That(layer1.GetService<IDemoService>(), Is.Not.SameAs(layer2.GetService<IDemoService>()));
    }

    [Test]
    public void Instance_service_cannot_be_reused_by_multiple_layer_providers()
    {
        var instance = new DemoService();

        var layer1 = new DemoLayer();
        layer1.RegisterService(new InstanceServiceModule(instance));

        var layer2 = new DemoLayer();
        layer2.RegisterService(new InstanceServiceModule(instance));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            LayerHub.CreateLayers().Push(layer1).Push(layer2).Build());

        Assert.That(ex!.Message, Does.Contain("already bound"));
    }

    [Test]
    public void Runtime_root_service_container_is_removed()
    {
        Assert.That(typeof(LayerRuntime).Assembly.GetType("LayerBase.DI.WorldServiceRoot"), Is.Null);
    }

    [Test]
    public void Concurrent_access_to_GetService_is_thread_safe()
    {
        var layer = new DemoLayer();
        layer.RegisterService(new ConcurrentServiceModule());
        LayerHub.CreateLayers().Push(layer).Build();

        const int threadCount = 10;
        var results = new IDemoService[threadCount];
        var threads = new Thread[threadCount];

        for (var i = 0; i < threadCount; i++)
        {
            var index = i;
            threads[i] = new Thread(() => { results[index] = layer.GetService<IDemoService>(); });
            threads[i].Start();
        }

        foreach (var t in threads) t.Join();

        for (var i = 1; i < threadCount; i++)
        {
            Assert.That(results[i], Is.Not.Null);
            Assert.That(ReferenceEquals(results[0], results[i]), Is.True);
        }
    }

    [Test]
    public void IService_that_implements_IUpdate_is_pumped()
    {
        var layer = new DemoLayer();
        var module = new UpdatingServiceModule();
        layer.RegisterService(module);
        LayerHub.CreateLayers().Push(layer).Build();

        var service = layer.GetService<UpdatingService>();
        Assert.That(service.TickCount, Is.EqualTo(0));

        LayerHub.Pump(0.02f);
        Assert.That(service.TickCount, Is.EqualTo(1));

        LayerHub.Pump(0.02f);
        Assert.That(service.TickCount, Is.EqualTo(2));
    }

    private partial class InstanceServiceModule : IService
    {
        private readonly object _instance;

        public InstanceServiceModule(object instance)
        {
            _instance = instance;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (_instance is IDemoService ds) services.AddSingleton<IDemoService>(ds);
        }
    }

    private interface IDemoService
    {
    }

    private class DemoService : IDemoService
    {
    }

    private class DemoLayer : Layer
    {
    }

    public partial class DemoServiceModule : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDemoService, DemoService>();
        }
    }

    public partial class SingletonServiceModule : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDemoService, DemoService>();
        }
    }

    public partial class ConcurrentServiceModule : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDemoService, DemoService>();
        }
    }

    public struct ServiceTestEvent
    {
    }


    internal struct ServiceRaisedEvent(int Id)
    {
        public int Id { get; } = Id;
    }

    public partial class UpdatingServiceModule : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<UpdatingService, UpdatingService>();
        }
    }

    public partial class UpdatingService : IService, IUpdate
    {
        public int TickCount { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Update()
        {
            TickCount++;
        }
    }
}
