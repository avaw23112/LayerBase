using System.Reflection;
using LayerBase;
using LayerBase.Core.Event;

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
        LayerHub.Send(new ResetProbeEvent());

        var cacheField = GetBucketCacheField(typeof(ResetProbeEvent));
        Assert.That(cacheField.GetValue(null), Is.Not.Null, "Expected the generic bucket cache to be populated after first send.");

        LayerHub.Reset();

        Assert.That(cacheField.GetValue(null), Is.Null, "Reset should clear static generic bucket caches so old buckets can be collected.");
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
}
