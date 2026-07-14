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
    public void Runtime_dispose_clears_payload_and_layer_target_cache_slots()
    {
        _ = EventTypeId<RuntimeCachePayloadEvent>.Id; // Ensure known before build
        var layer = new CacheCleanupLayer();
        var runtime = LayerHub.CreateLayers().Push(layer).Build();
        var runtimeId = runtime.Id;

        Assert.That(runtime.Scheduler.TryPost(new RuntimeCachePayloadEvent()).IsSuccess, Is.True);

        Assert.That(runtime.TryResolveLayerTarget<CacheCleanupLayer>(out var resolved, out var error), Is.True);
        Assert.That(resolved, Is.SameAs(layer));
        Assert.That(error, Is.Null);

        var version = runtime.GetLayerTypeBindingsVersion();

        runtime.Dispose();

        Assert.That(LayerHub.TryGetCachedTarget<CacheCleanupLayer>(
            runtimeId,
            version,
            out _,
            out _), Is.False);
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
    public void Duplicate_singleton_registration_with_different_implementation_fails()
    {
        var layerA = new DuplicateSingletonLayerA();
        var layerB = new DuplicateSingletonLayerB();

        Assert.That(
            () => LayerHub.CreateLayers().Push(layerA).Push(layerB).Build(),
            Throws.TypeOf<InvalidOperationException>()
                  .With.Message.Contains("Duplicate singleton registration"));
    }

    [Test]
    public void Singleton_is_runtime_bound_and_layer_only_api_fails_clearly()
    {
        var layerA = new SameSingletonLayer();
        var layerB = new SameSingletonLayer();

        LayerHub.CreateLayers().Push(layerA).Push(layerB).Build();

        var fromA = layerA.GetService<RuntimeBoundSingletonService>();
        var fromB = layerB.GetService<RuntimeBoundSingletonService>();

        Assert.That(fromB, Is.SameAs(fromA));

        // GetService should now work because it falls back to world provider
        Assert.That(fromA.GetService<RuntimeBoundSingletonService>(), Is.SameAs(fromA));

        // But Layer-only API like Delay should still fail clearly
        Assert.That(
            () => fromA.Delay(new RuntimeCachePayloadEvent(), 1.0f),
            Throws.TypeOf<InvalidOperationException>()
                  .With.Message.Contains("bound to Runtime"));
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

        var result = runtime.Scheduler.MarkDirty<UnknownAfterBuildDirtyEvent>();

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void TryPost_reports_missing_primary_runtime()
    {
        LayerHub.Reset();

        var result = LayerHub.TryPost(new RuntimeCachePayloadEvent());

        Assert.That(result.IsSuccess, Is.False);
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
            RegisterService(new RuntimeBoundSingletonRegistrar());
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

    private sealed class RuntimeBoundSingletonRegistrar : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<RuntimeBoundSingletonService, RuntimeBoundSingletonService>();
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

    private sealed class RuntimeBoundSingletonService : IService
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
