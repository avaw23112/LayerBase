using System.Reflection;
using LayerBase.Core.Event;

namespace EventsTest;

public sealed class EventCenterGenericArchitectureTests
{
    [Test]
    public void Send_without_subscribers_does_not_allocate_event_bucket()
    {
        var center = new EventCenter();

        var state = center.Send(new GenericArchitectureEvent());

        Assert.That(state, Is.EqualTo(EventHandledState.Continue));
        Assert.That(GetBucketCount(center), Is.Zero);
    }

    [Test]
    public void EventCenter_keeps_only_generic_bucket_entry_points()
    {
        var type = typeof(EventCenter);

        Assert.That(type.GetField("_eventBuckets", BindingFlags.NonPublic | BindingFlags.Instance)!.FieldType.IsArray,
            Is.True);
        Assert.That(type.GetField("_bucketCacheResetters", BindingFlags.NonPublic | BindingFlags.Instance), Is.Null);
        Assert.That(type.GetField("_lock", BindingFlags.NonPublic | BindingFlags.Instance), Is.Null);
        Assert.That(type.GetField("_isResetting", BindingFlags.NonPublic | BindingFlags.Instance), Is.Null);
        Assert.That(type.GetNestedTypes(BindingFlags.NonPublic).Any(t => t.Name.StartsWith("BucketCache")),
            Is.False);
        Assert.That(type.Assembly.GetTypes().Any(t => t.Name == "IEventBucketNonGeneric"), Is.False);

        var instanceMethods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(instanceMethods.Any(m => m.Name == "GetBucket" &&
                                             m.GetParameters() is [{ ParameterType: var parameterType }] &&
                                             parameterType == typeof(Type)), Is.False);
        Assert.That(instanceMethods.Any(m => m.GetParameters().Any(p => p.ParameterType == typeof(Type)) &&
                                             m.GetParameters().Any(p => p.ParameterType == typeof(object))),
            Is.False);
    }

    private static int GetBucketCount(EventCenter center)
    {
        var storage = typeof(EventCenter)
            .GetField("_eventBuckets", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(center);

        return storage switch
        {
            Array array => array.Cast<object?>().Count(static item => item != null),
            System.Collections.ICollection collection => collection.Count,
            _ => throw new AssertionException("Unknown event bucket storage.")
        };
    }

    private readonly struct GenericArchitectureEvent;
}
