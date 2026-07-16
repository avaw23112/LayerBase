using LayerBase;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;

namespace EventsTest.Safety;

[TestFixture]
public sealed class EventCenterSafetyTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(1, 7)]
    [TestCase(4, 4)]
    [TestCase(2, 0)]
    [TestCase(8, 0)]
    [TestCase(9, 0)]
    public void MixedNotifyAndSubscribe_DispatchesAllHandlers(int notifyCount, int subscribeCount)
    {
        var center = new EventCenter();
        var notifyHits = 0;
        var subscribeHits = 0;

        for (var i = 0; i < notifyCount; i++)
            center.SubscribeNotify<SafetyEvent>(0, (in SafetyEvent _) => notifyHits++);

        for (var i = 0; i < subscribeCount; i++)
            center.Subscribe<SafetyEvent>(0, (in SafetyEvent _) => subscribeHits++);

        Assert.DoesNotThrow(() => center.Send(new SafetyEvent()));
        Assert.That(notifyHits, Is.EqualTo(notifyCount));
        Assert.That(subscribeHits, Is.EqualTo(subscribeCount));
    }

    [Test]
    public void AsyncHandlerSynchronousThrow_IsIsolated()
    {
        var center = new EventCenter();
        var errorReported = new ManualResetEventSlim();
        var aRuns = 0;
        var bRuns = 0;
        var cRuns = 0;

        Action<LayerEventInfo> onInfo = info =>
        {
            if (info.Type == LayerEventInfoType.Error)
                errorReported.Set();
        };

        LayerHub.OnLayerEventInfo += onInfo;
        try
        {
            center.SubscribeAsync<SafetyEvent>(0, _ =>
            {
                aRuns++;
                return LBTask.CompletedTask;
            });
            center.SubscribeAsync<SafetyEvent>(0, _ =>
            {
                bRuns++;
                throw new InvalidOperationException("sync async failure");
            });
            center.SubscribeAsync<SafetyEvent>(0, _ =>
            {
                cRuns++;
                return LBTask.CompletedTask;
            });

            Assert.DoesNotThrow(() => center.Send(new SafetyEvent()));
            Assert.That(errorReported.Wait(TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(aRuns, Is.EqualTo(1));
            Assert.That(bRuns, Is.EqualTo(1));
            Assert.That(cRuns, Is.EqualTo(1));

            errorReported.Reset();
            center.Send(new SafetyEvent());

            Assert.That(aRuns, Is.EqualTo(2));
            Assert.That(bRuns, Is.EqualTo(1));
            Assert.That(cRuns, Is.EqualTo(2));
            Assert.That(errorReported.IsSet, Is.False);
        }
        finally
        {
            LayerHub.OnLayerEventInfo -= onInfo;
        }
    }

    [Test]
    public void AsyncFaultAfterBucketRebuild_DisablesOriginalHandler()
    {
        var center = new EventCenter();
        using var firstFault = new LBTaskCompletionSource();
        var errorReported = new ManualResetEventSlim();
        var aRuns = 0;
        var bRuns = 0;
        var cRuns = 0;

        EventHandleDelegateAsync<SafetyEvent> handlerB = _ =>
        {
            bRuns++;
            return LBTask.CompletedTask;
        };

        EventHandleDelegateAsync<SafetyEvent> handlerA = _ =>
        {
            aRuns++;
            return aRuns == 1 ? firstFault.Task : LBTask.CompletedTask;
        };

        EventHandleDelegateAsync<SafetyEvent> handlerC = _ =>
        {
            cRuns++;
            return LBTask.CompletedTask;
        };

        Action<LayerEventInfo> onInfo = info =>
        {
            if (info.Type == LayerEventInfoType.Error)
                errorReported.Set();
        };

        LayerHub.OnLayerEventInfo += onInfo;
        try
        {
            center.SubscribeAsync(0, handlerB);
            center.SubscribeAsync(0, handlerA);

            center.Send(new SafetyEvent());
            Assert.That(aRuns, Is.EqualTo(1));
            Assert.That(bRuns, Is.EqualTo(1));

            center.UnsubscribeAsync(0, handlerB);
            center.SubscribeAsync(0, handlerC);
            center.PrewarmEvent<SafetyEvent>(new LayerPrewarmOptions(LayerPrewarmTargets.DispatchTable));

            firstFault.SetException(new InvalidOperationException("delayed async failure"));
            Assert.That(errorReported.Wait(TimeSpan.FromSeconds(2)), Is.True);

            center.Send(new SafetyEvent());

            Assert.That(aRuns, Is.EqualTo(1), "The original delayed-fault handler should be disabled.");
            Assert.That(cRuns, Is.EqualTo(1), "A handler that reused the old fault-table index must remain enabled.");
        }
        finally
        {
            LayerHub.OnLayerEventInfo -= onInfo;
        }
    }

    private readonly struct SafetyEvent;
}
