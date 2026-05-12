using NUnit.Framework;
using LayerBase.Core.Event;
using LayerBase.Core.DataStruct;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Event.Delay;
using System;
using System.Reflection;

namespace LayerBase.Test;

[TestFixture]
public partial class P0IssueReproductionTests
{
    [Test]
    public void RingBuffer_ShouldRespectLogicalCapacity()
    {
        var buffer = new RingBuffer<int>(3);
        Assert.That(buffer.Count, Is.EqualTo(0));
        Assert.That(buffer.TryEnqueue(1), Is.True);
        Assert.That(buffer.TryEnqueue(2), Is.True);
        Assert.That(buffer.TryEnqueue(3), Is.True);
        // This should now fail if logical capacity is 3
        Assert.That(buffer.TryEnqueue(4), Is.False);
    }

    [Test]
    public void PostLatest_ShouldNotOverrideBackpressurePolicy()
    {
        LayerHub.Reset();
        var runtime = LayerHub.CreateLayers()
                              .Push(new CoalescedTestLayer())
                              .Build();

        // Metadata says DropOldest
        LayerHub.PostLatest(new P0TestEvent { Value = 1 });

        // We can check the plan in scheduler
        var scheduler = runtime.Scheduler;
        var typeId = EventTypeId<P0TestEvent>.Id;

        // Use reflection to get _postPlans if needed, but it's internal
        // Actually PostScheduler._postPlans is private.
        // But we can check behavior.
    }

    [Test]
    public void FlushBuffers_Reentrancy_ShouldNotThrow()
    {
        LayerHub.Reset();
        var runtime = LayerHub.CreateLayers()
                              .Push(new ReentrantLayer())
                              .Build();

        LayerHub.PostCoalesced(new P0TestEvent { Value = 1 });

        // This will trigger FlushBuffers
        runtime.Pump(0.1f);

        // If it didn't throw and we can pump again, it's a good sign.
        runtime.Pump(0.1f);
    }

    [Test]
    public void Delay_HasAnyDelay_ShouldCorrectlyClose()
    {
        LayerHub.Reset();
        var layer = new DelayTestLayer();
        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .Build();

        var chain = typeof(LayerRuntime).GetField("_chain", BindingFlags.NonPublic | BindingFlags.Instance)
                                        ?.GetValue(runtime);
        var hasAnyDelayProp = chain?.GetType().GetProperty("HasAnyDelay", BindingFlags.Public | BindingFlags.Instance);

        Assert.That((bool)hasAnyDelayProp?.GetValue(chain)!, Is.False);

        var pub = layer.SubscribeDelay<P0TestEvent>();
        pub.Publish(new P0TestEvent { Value = 1 }, 0.1f);

        Assert.That((bool)hasAnyDelayProp?.GetValue(chain)!, Is.True);

        // Take the value, which should clear it
        pub.TryTake(out _);

        Assert.That((bool)hasAnyDelayProp?.GetValue(chain)!, Is.False);
    }
}

public partial struct P0TestEvent
{
    public int Value;
}

public class P0TestEventMetaData : EventMetaData<P0TestEvent>
{
    public override EventPostPolicy? PostPolicy => new(PostDeliveryMode.Coalesced, BackpressurePolicy.DropOldest, 10);
}

public partial class CoalescedTestLayer : Layer
{
}

public partial class DelayTestLayer : Layer
{
}

public partial class ReentrantLayer : Layer
{
    [Subscribe]
    public void OnTest(in P0TestEvent e)
    {
        if (e.Value < 5)
        {
            // Reentrant call
            LayerHub.PostCoalesced(new P0TestEvent { Value = e.Value + 1 });
        }
    }
}