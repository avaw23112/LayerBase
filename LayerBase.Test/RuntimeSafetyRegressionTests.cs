using System.Reflection;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class RuntimeSafetyRegressionTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Reset_clears_generic_bucket_cache_instances()
    {
        LayerHub.CreateLayers().Push(new DisposableProbeLayer()).Build();
        LayerHub.Send(new ResetProbeEvent());

        var cacheField = GetBucketCacheField(typeof(ResetProbeEvent));
        Assert.That(cacheField.GetValue(null), Is.Not.Null,
            "Expected the generic bucket cache to be populated after first send.");

        LayerHub.Reset();

        Assert.That(cacheField.GetValue(null), Is.Null,
            "Reset should clear static generic bucket caches so old buckets can be collected.");
    }

    [Test]
    public void Reset_disposes_existing_layer_scoped_services()
    {
        DisposableProbeService.DisposeCount = 0;

        var layer = new DisposableProbeLayer();
        layer.RegisterService(new DisposableProbeRegistrar());
        LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        Assert.That(DisposableProbeService.DisposeCount, Is.EqualTo(0));

        LayerHub.Reset();

        Assert.That(DisposableProbeService.DisposeCount, Is.EqualTo(1),
            "Reset should dispose the previous layer chain before clearing global state.");
    }

    private static FieldInfo GetBucketCacheField(Type eventType)
    {
        var nested = typeof(GlobalEventCenter).GetNestedType("BucketCache`1", BindingFlags.NonPublic);
        Assert.That(nested, Is.Not.Null, "Failed to locate GlobalEventCenter.BucketCache<T>.");

        var closed = nested!.MakeGenericType(eventType);
        var field = closed.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(field, Is.Not.Null, "Failed to locate BucketCache<T>.Instance field.");
        return field!;
    }

    private readonly struct ResetProbeEvent
    {
    }

    private sealed class DisposableProbeLayer : Layer
    {
    }

    private sealed class DisposableProbeRegistrar : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<DisposableProbeService, DisposableProbeService>();
        }
    }

    private sealed class DisposableProbeService : IDisposable
    {
        public static int DisposeCount;

        public void Dispose()
        {
            Interlocked.Increment(ref DisposeCount);
        }
    }
}
