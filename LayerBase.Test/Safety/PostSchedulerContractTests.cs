using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;

namespace EventsTest.Safety;

[TestFixture]
public sealed class PostSchedulerContractTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void AllocatedButUnregisteredEvent_UsesDefaultNormalPlan()
    {
        _ = EventTypeId<UnregisteredEvent>.Id;
        var options = PostSchedulerOptions.Default;
        var center = new EventCenter();
        var scheduler = new PostScheduler(0, center, options,
            new EventBuildPolicyTable(options.DefaultBackpressure));

        int callCount = 0;
        center.SubscribeNotify<UnregisteredEvent>(0, (in UnregisteredEvent _) => callCount++);

        Assert.DoesNotThrow(() =>
        {
            var result = scheduler.TryPost(new UnregisteredEvent());
            Assert.That(result.IsSuccess, Is.True, "Unregistered event should use default normal plan.");
        });

        scheduler.Pump();
        Assert.That(callCount, Is.EqualTo(1), "The event must be dispatched when pumped.");
    }

    private struct UnregisteredEvent
    {
        public int Value;
    }
}
