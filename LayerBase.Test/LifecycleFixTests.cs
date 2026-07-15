using System.Reflection;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.Delay;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class LifecycleFixTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    #region P0-1 DelayPublisher

    [Test]
    public void DelayPublisher_Deactivates_On_Layer_Dispose()
    {
        var layer = new TestLayer();
        using var runtime = LayerHub.CreateLayers().Push(layer).Build();

        var publisher = (IDelayPublisherInternal)layer.SubscribeDelay<TestEvent>();
        int publisherId = publisher.PublisherId;
        Assert.That(publisherId, Is.GreaterThanOrEqualTo(0));

        layer.Dispose();

        Assert.That(publisher.PublisherId, Is.EqualTo(-1));

        var layer2 = new TestLayer();
        LayerHub.Reset();
        using var runtime2 = LayerHub.CreateLayers().Push(layer2).Build();
        var publisher2 = (IDelayPublisherInternal)layer2.SubscribeDelay<TestEvent>();

        Assert.That(publisher2.PublisherId, Is.EqualTo(0));
    }

    [Test]
    public void DelayPublisher_Deactivates_On_Layer_PrepareBuild()
    {
        var layer = new TestLayer();
        var builder = LayerHub.CreateLayers().Push(layer);
        using var runtime = builder.Build();

        var publisher = (IDelayPublisherInternal)layer.SubscribeDelay<TestEvent>();
        int oldId = publisher.PublisherId;

        var builder2 = LayerHub.CreateLayers().Push(layer);
        using var runtime2 = builder2.Build();

        Assert.That(publisher.PublisherId, Is.EqualTo(-1));

        var newPublisher = (IDelayPublisherInternal)layer.SubscribeDelay<TestEvent>();
        Assert.That(newPublisher, Is.Not.SameAs(publisher));
        Assert.That(newPublisher.PublisherId, Is.EqualTo(oldId));
    }

    #endregion

    #region P0-2 ServiceLayerBinding Detach

    [Test]
    public void Service_Binding_Is_Cleared_On_Layer_Dispose()
    {
        var service = new TestService();
        var layer = new TestLayer();
        layer.RegisterService(service);

        using var runtime = LayerHub.CreateLayers().Push(layer).Build();

        Assert.That(ServiceLayerBinder.GetBinding(service), Is.Not.Null);

        layer.Dispose();

        Assert.That(ServiceLayerBinder.GetBinding(service), Is.Null);
    }

    [Test]
    public void Runtime_And_Layer_Can_Be_GCed_After_Dispose()
    {
        WeakReference runtimeRef;
        WeakReference layerRef;

        (runtimeRef, layerRef) = ExecuteAndDispose();

        for (int i = 0; i < 5; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.That(runtimeRef.IsAlive, Is.False, "Runtime should be GCed");
        Assert.That(layerRef.IsAlive, Is.False, "Layer should be GCed");
    }

    private (WeakReference, WeakReference) ExecuteAndDispose()
    {
        var layer = new TestLayer();
        var runtime = LayerHub.CreateLayers().Push(layer).Build();
        var runtimeRef = new WeakReference(runtime);
        var layerRef = new WeakReference(layer);
        runtime.Dispose();
        return (runtimeRef, layerRef);
    }

    #endregion

    #region P0-4 Singleton Binding

    [Test]
    public void Singleton_Service_Is_Bound_To_Layer_Provider()
    {
        var layer = new TestLayer(services => { services.AddSingleton<GlobalSingleton, GlobalSingleton>(); });
        using var runtime = LayerHub.CreateLayers().Push(layer).Build();

        var singleton = layer.GetService<GlobalSingleton>();
        var binding = ServiceLayerBinder.GetBinding(singleton);

        Assert.That(binding, Is.Not.Null);
        Assert.That(binding.Layer, Is.SameAs(layer), "Singleton should be bound to its Layer provider.");
        Assert.That(binding.RuntimeId, Is.EqualTo(runtime.Id));
    }

    [Test]
    public void Instance_Reused_By_Another_Runtime_Provider_Throws()
    {
        var singleton = new GlobalSingleton();

        var layer1 = new TestLayer(services => { services.AddSingleton(singleton); });
        using var runtime1 = LayerHub.CreateLayers().Push(layer1).Build();
        _ = layer1.GetService<GlobalSingleton>();

        var layer2 = new TestLayer(services => { services.AddSingleton(singleton); });
        var builder2 = LayerHub.CreateLayers().Push(layer2);

        Assert.Throws<InvalidOperationException>(() => builder2.Build());
    }

    #endregion

    #region P1-2 EventStore Dispose

    [Test]
    public void EventStore_Dispose_Clears_Buffers()
    {
        var store = new EventStore<TestEvent>(10);
        store.Add(new TestEvent { Value = 123 });

        store.Dispose();

        var bufferField =
            typeof(EventStore<TestEvent>).GetField("_buffer", BindingFlags.NonPublic | BindingFlags.Instance);
        var buffer = (TestEvent[]?)bufferField?.GetValue(store);

        Assert.That(buffer?.Length, Is.EqualTo(0));
    }

    [Test]
    public void EventStore_Add_After_Dispose_Throws()
    {
        var store = new EventStore<TestEvent>();
        store.Dispose();
        Assert.Throws<ObjectDisposedException>(() => store.Add(default));
    }

    #endregion

    #region P1-3 DI Constructor Selection

    [Test]
    public void DI_Selects_Public_Constructor_With_Most_Parameters()
    {
        var layer = new TestLayer(services =>
        {
            services.AddTransient<MultiCtorService, MultiCtorService>();
            services.AddSingleton<GlobalSingleton, GlobalSingleton>();
        });
        using var runtime = LayerHub.CreateLayers().Push(layer).Build();

        var service = layer.GetService<MultiCtorService>();
        Assert.That(service.UsedCtor, Is.EqualTo(1), "Should select public constructor with param");
    }

    [Test]
    public void DI_Selects_Mount_Constructor_Even_If_NonPublic()
    {
        var layer = new TestLayer(services =>
        {
            services.AddTransient<MountCtorService, MountCtorService>();
            services.AddSingleton<GlobalSingleton, GlobalSingleton>();
        });
        using var runtime = LayerHub.CreateLayers().Push(layer).Build();

        var service = layer.GetService<MountCtorService>();
        Assert.That(service.UsedCtor, Is.EqualTo(2), "Should select [Mount] constructor");
    }

    [Test]
    public void DI_Throws_On_Ambiguous_Public_Constructors()
    {
        var layer = new TestLayer(services => { services.AddTransient<AmbiguousCtorService, AmbiguousCtorService>(); });
        var builder = LayerHub.CreateLayers().Push(layer);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    #endregion

    #region Helpers

    private struct TestEvent
    {
        public int Value;
    }

    private class TestLayer : Layer
    {
        private readonly Action<IServiceCollection>? _configAction;

        public TestLayer(Action<IServiceCollection>? configAction = null)
        {
            _configAction = configAction;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<TestService, TestService>();
            _configAction?.Invoke(services);
        }
    }

    private class TestService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private class GlobalSingleton : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private class MultiCtorService
    {
        public int UsedCtor;

        public MultiCtorService()
        {
            UsedCtor = 0;
        }

        public MultiCtorService(GlobalSingleton g)
        {
            UsedCtor = 1;
        }

        private MultiCtorService(GlobalSingleton g, int x)
        {
            UsedCtor = 2;
        }
    }

    private class MountCtorService
    {
        public int UsedCtor;

        public MountCtorService()
        {
            UsedCtor = 1;
        }

        [Mount]
        private MountCtorService(GlobalSingleton g)
        {
            UsedCtor = 2;
        }
    }

    private class AmbiguousCtorService
    {
        public AmbiguousCtorService(int x)
        {
        }

        public AmbiguousCtorService(string s)
        {
        }
    }

    #endregion
}
