using LayerBase.Core.Event;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class PostSchedulerHardeningTests
{
    private struct DynamicNormalEvent
    {
        public int Value;
    }

    private struct GlobalEventTypeA
    {
    }

    private struct GlobalEventTypeB
    {
    }

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Dynamic_normal_event_after_build_is_accepted()
    {
        var options = PostSchedulerOptions.Default;
        var eventCenter = new EventCenter();
        using var scheduler = new PostScheduler(
            0,
            eventCenter,
            options,
            new EventBuildPolicyTable(options.DefaultBackpressure));

        scheduler.BuildPlans(Array.Empty<PostTypePlan>());

        int received = 0;
        eventCenter.SubscribeNotify<DynamicNormalEvent>(0, (in DynamicNormalEvent e) => received += e.Value);

        var result = scheduler.TryPost(new DynamicNormalEvent { Value = 7 });

        Assert.That(result.IsSuccess, Is.True);
        scheduler.Pump();
        Assert.That(received, Is.EqualTo(7));
    }

    [Test]
    public void Global_event_type_id_behavior_is_unchanged()
    {
        int first = EventTypeId<GlobalEventTypeA>.Id;
        int firstAgain = EventTypeId<GlobalEventTypeA>.Id;
        int second = EventTypeId<GlobalEventTypeB>.Id;

        Assert.That(firstAgain, Is.EqualTo(first));
        Assert.That(second, Is.GreaterThan(first));
        Assert.That(EventTypeIdAllocator.MaxId, Is.GreaterThanOrEqualTo(second));
    }
}
