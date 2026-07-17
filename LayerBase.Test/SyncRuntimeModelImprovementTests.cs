using System.Reflection;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public sealed class SyncRuntimeModelImprovementTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Runtime_dispose_does_not_leak_stores()
    {
        _ = EventTypeId<RuntimeCachePayloadEvent>.Id; // Ensure known before build
        var runtime = LayerHub.CreateLayers().Push(new CacheCleanupLayer()).Build();

        Assert.That(runtime.Scheduler.TryPost(new RuntimeCachePayloadEvent()).IsSuccess, Is.True);

        runtime.Pump(0f);
        runtime.Dispose();
    }

    [Test]
    public void Runtime_id_is_reused_after_dispose()
    {
        var first = LayerHub.CreateLayers().Push(new CacheCleanupLayer()).Build();
        var firstId = first.Id;

        first.Dispose();

        var second = LayerHub.CreateLayers().Push(new CacheCleanupLayer()).Build();

        Assert.That(second.Id, Is.EqualTo(firstId));
    }

    [Test]
    public void Duplicate_singleton_registration_with_different_implementation_is_layer_isolated()
    {
        var layerA = new DuplicateSingletonLayerA();
        var layerB = new DuplicateSingletonLayerB();

        LayerHub.CreateLayers().Push(layerA).Push(layerB).Build();

        Assert.That(layerA.GetService<IDuplicateSingleton>(), Is.TypeOf<DuplicateSingletonA>());
        Assert.That(layerB.GetService<IDuplicateSingleton>(), Is.TypeOf<DuplicateSingletonB>());
    }

    [Test]
    public void Singleton_is_layer_provider_bound_and_layer_only_api_is_available()
    {
        var layerA = new SameSingletonLayer();
        var layerB = new SameSingletonLayer();

        LayerHub.CreateLayers().Push(layerA).Push(layerB).Build();

        var fromA = layerA.GetService<LayerBoundSingletonService>();
        var fromB = layerB.GetService<LayerBoundSingletonService>();

        Assert.That(fromB, Is.Not.SameAs(fromA));

        var binding = ServiceLayerBinder.GetBinding(fromA);
        Assert.That(binding, Is.Not.Null);
        Assert.That(binding!.Layer, Is.SameAs(layerA));

        Assert.That(fromA.GetService<LayerBoundSingletonService>(), Is.SameAs(fromA));
        Assert.DoesNotThrow(() => fromA.Delay(new RuntimeCachePayloadEvent(), 1.0f));
    }

    [Test]
    public void Factory_resolution_reuses_context_for_cycle_detection()
    {
        var layer = new FactoryCycleLayer();

        Assert.That(
            () => LayerHub.CreateLayers().Push(layer).Build(),
            Throws.TypeOf<InvalidOperationException>()
                  .With.Message.Contains("Circular dependency detected"));
    }

    [Test]
    public void Build_after_unknown_dirty_event_returns_failure()
    {
        var runtime = LayerHub.CreateLayers().Push(new CacheCleanupLayer()).Build();

        var result = runtime.Scheduler.TryPost(default(UnknownAfterBuildDirtyEvent));

        Assert.That(result.IsSuccess, Is.True,
            "Unregistered events should use default normal plan.");
    }

    [Test]
    public void Layer_dispose_clears_pending_subscription_operations()
    {
        var layer = new CacheCleanupLayer();
        layer.Subscribe<RuntimeCachePayloadEvent>((in RuntimeCachePayloadEvent _) => { });

        var pendingOps = GetPendingOps(layer);
        Assert.That(pendingOps.Count, Is.GreaterThan(0));

        layer.Dispose();

        Assert.That(pendingOps.Count, Is.EqualTo(0));
    }

    private static System.Collections.ICollection GetPendingOps(Layer layer)
    {
        var field = typeof(Layer).GetField("_pendingOps", BindingFlags.Instance | BindingFlags.NonPublic);
        return (System.Collections.ICollection)field!.GetValue(layer)!;
    }

    private sealed class CacheCleanupLayer : Layer
    {
    }

    private sealed class DuplicateSingletonLayerA : Layer
    {
        public DuplicateSingletonLayerA()
        {
            RegisterService(new DuplicateSingletonRegistrarA());
        }
    }

    private sealed class DuplicateSingletonLayerB : Layer
    {
        public DuplicateSingletonLayerB()
        {
            RegisterService(new DuplicateSingletonRegistrarB());
        }
    }

    private sealed class SameSingletonLayer : Layer
    {
        public SameSingletonLayer()
        {
            RegisterService(new LayerBoundSingletonRegistrar());
        }
    }

    private sealed class FactoryCycleLayer : Layer
    {
        public FactoryCycleLayer()
        {
            RegisterService(new FactoryCycleRegistrar());
        }
    }

    private sealed class DuplicateSingletonRegistrarA : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDuplicateSingleton, DuplicateSingletonA>();
        }
    }

    private sealed class DuplicateSingletonRegistrarB : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDuplicateSingleton, DuplicateSingletonB>();
        }
    }

    private sealed class LayerBoundSingletonRegistrar : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<LayerBoundSingletonService, LayerBoundSingletonService>();
        }
    }

    private sealed class FactoryCycleRegistrar : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<FactoryCycleA>(sp => new FactoryCycleA(sp.Get<FactoryCycleB>()));
            services.AddScoped<FactoryCycleB>(sp => new FactoryCycleB(sp.Get<FactoryCycleA>()));
        }
    }

    private interface IDuplicateSingleton
    {
    }

    private sealed class DuplicateSingletonA : IDuplicateSingleton
    {
    }

    private sealed class DuplicateSingletonB : IDuplicateSingleton
    {
    }

    private sealed class LayerBoundSingletonService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class FactoryCycleA
    {
        public FactoryCycleA(FactoryCycleB dependency)
        {
            Dependency = dependency;
        }

        public FactoryCycleB Dependency { get; }
    }

    private sealed class FactoryCycleB
    {
        public FactoryCycleB(FactoryCycleA dependency)
        {
            Dependency = dependency;
        }

        public FactoryCycleA Dependency { get; }
    }

    private struct RuntimeCachePayloadEvent
    {
    }

    private struct UnknownAfterBuildDirtyEvent
    {
    }
}
