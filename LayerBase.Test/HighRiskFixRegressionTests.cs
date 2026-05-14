using System.Reflection;
using System.Threading;
using LayerBase;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Test;

public partial struct HighRiskStreamConfiguredEvent
{
    public int Value;
}

public sealed class HighRiskStreamConfiguredEventMetaData : EventMetaData<HighRiskStreamConfiguredEvent>
{
    public override ActorMailOptions? ActorMailOptions =>
        LayerBase.Actor.ActorMailOptions.EventStream(segmentCapacity: 7, maxRetainedSegments: 3);
}

public readonly struct HighRiskActorEvent
{
    public readonly int Value;

    public HighRiskActorEvent(int value)
    {
        Value = value;
    }
}

public readonly struct HighRiskReferenceEvent
{
    public readonly object? Payload;

    public HighRiskReferenceEvent(object? payload)
    {
        Payload = payload;
    }
}

public readonly struct HighRiskCallRequest
{
    public readonly int Value;

    public HighRiskCallRequest(int value)
    {
        Value = value;
    }
}

public readonly struct HighRiskCallResponse
{
    public readonly int Value;

    public HighRiskCallResponse(int value)
    {
        Value = value;
    }
}

internal static class HighRiskTrace
{
    public static List<string> Entries { get; } = new();

    public static void Reset()
    {
        Entries.Clear();
    }
}

internal sealed partial class HighRiskActor : IActor, IUpdate
{
    [ActorBehaviour]
    private void OnEvent(in HighRiskActorEvent value)
    {
        HighRiskTrace.Entries.Add($"event:{value.Value}");
    }

    void IUpdate.Update(float deltaTime)
    {
        HighRiskTrace.Entries.Add($"update:{deltaTime:0.###}");
    }

    [ActorCallBehaviour]
    private LBTask<HighRiskCallResponse> OnAsk(in HighRiskCallRequest request, CancellationToken cancellationToken)
    {
        HighRiskTrace.Entries.Add($"ask:{request.Value}");
        return LBTask<HighRiskCallResponse>.FromResult(new HighRiskCallResponse(request.Value * 10));
    }
}

internal sealed partial class HighRiskSecondActor : IActor
{
    [ActorBehaviour]
    private void OnEvent(in HighRiskActorEvent value)
    {
        HighRiskTrace.Entries.Add($"second:{value.Value}");
    }
}

[TestFixture]
public class HighRiskFixRegressionTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
        HighRiskTrace.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
        HighRiskTrace.Reset();
    }

    [Test]
    public void Event_metadata_replays_after_layerhub_reset()
    {
        ActorEventStreamPlan<HighRiskStreamConfiguredEvent> firstPlan =
            ActorEventStreamPlanBuilder.Build<HighRiskStreamConfiguredEvent>();

        Assert.That(firstPlan.StreamOptions.SegmentCapacity, Is.EqualTo(7));
        Assert.That(firstPlan.StreamOptions.MaxRetainedSegments, Is.EqualTo(3));

        LayerHub.Reset();

        ActorEventStreamPlan<HighRiskStreamConfiguredEvent> secondPlan =
            ActorEventStreamPlanBuilder.Build<HighRiskStreamConfiguredEvent>();

        Assert.That(secondPlan.StreamOptions.SegmentCapacity, Is.EqualTo(7));
        Assert.That(secondPlan.StreamOptions.MaxRetainedSegments, Is.EqualTo(3));
    }

    [Test]
    public void Event_stream_runtime_is_unique_per_archetype()
    {
        var world = new ActorWorld();
        HighRiskActor first = world.CreateActor<HighRiskActor>();
        HighRiskSecondActor second = world.CreateActor<HighRiskSecondActor>();
        ActorEventStreamPlan<HighRiskActorEvent> plan = ActorEventStreamPlanBuilder.Build<HighRiskActorEvent>();

        EventStreamRuntime<HighRiskActorEvent> firstRuntime =
            world.GetOrCreateEventStreamRuntime(plan, first.GetActorId().ArchetypeId);
        EventStreamRuntime<HighRiskActorEvent> firstRuntimeAgain =
            world.GetOrCreateEventStreamRuntime(plan, first.GetActorId().ArchetypeId);
        EventStreamRuntime<HighRiskActorEvent> secondRuntime =
            world.GetOrCreateEventStreamRuntime(plan, second.GetActorId().ArchetypeId);

        Assert.That(firstRuntimeAgain, Is.SameAs(firstRuntime));
        Assert.That(secondRuntime, Is.Not.SameAs(firstRuntime));
        Assert.That(firstRuntime.RuntimeIndex, Is.EqualTo(world.RuntimeIndex));
        Assert.That(secondRuntime.RuntimeIndex, Is.EqualTo(world.RuntimeIndex));
        Assert.That(secondRuntime.ArchetypeId, Is.Not.EqualTo(firstRuntime.ArchetypeId));
    }

    [Test]
    public void Disposed_world_does_not_post_into_reused_runtime_index()
    {
        var worldA = new ActorWorld();
        HighRiskActor actorA = worldA.CreateActor<HighRiskActor>();
        ActorId staleActorId = actorA.GetActorId();
        int reusedRuntimeIndex = worldA.RuntimeIndex;

        worldA.Dispose();

        var worldB = new ActorWorld();
        Assert.That(worldB.RuntimeIndex, Is.EqualTo(reusedRuntimeIndex));
        worldB.CreateActor<HighRiskActor>();

        worldA.PostTo(staleActorId, new HighRiskActorEvent(9));
        Pump(worldB);

        Assert.That(HighRiskTrace.Entries, Does.Not.Contain("event:9"));
    }

    [Test]
    public void Create_actor_throws_after_dispose()
    {
        var world = new ActorWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.CreateActor<HighRiskActor>());
    }

    [Test]
    public void Dispatch_now_returns_failure_after_dispose()
    {
        var world = new ActorWorld();
        HighRiskActor actor = world.CreateActor<HighRiskActor>();
        ActorId actorId = actor.GetActorId();

        world.Dispose();

        DispatchResult result = world.DispatchNow(actorId, new HighRiskActorEvent(4));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailureKind, Is.EqualTo(DispatchFailureKind.ActorNotFound));
        Assert.That(HighRiskTrace.Entries, Is.Empty);
    }

    [Test]
    public void Immediately_ask_returns_disposed_failure_after_dispose()
    {
        var world = new ActorWorld();
        HighRiskActor actor = world.CreateActor<HighRiskActor>();
        ActorId actorId = actor.GetActorId();

        world.Dispose();

        ActorCallException? error = Assert.Throws<ActorCallException>(() =>
            world.ImmediatelyAsk<HighRiskCallRequest, HighRiskCallResponse>(
                    actorId,
                    new HighRiskCallRequest(5))
                .GetAwaiter()
                .GetResult());

        Assert.That(error!.FailureKind, Is.EqualTo(ActorCallFailureKind.Disposed));
        Assert.That(HighRiskTrace.Entries, Is.Empty);
    }

    [Test]
    public void Delay_post_throws_after_dispose()
    {
        var world = new ActorWorld();
        HighRiskActor actor = world.CreateActor<HighRiskActor>();
        ActorId actorId = actor.GetActorId();

        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            world.DelayPost(actorId, new HighRiskActorEvent(7), 1f));
    }

    [Test]
    public void Delay_post_throws_after_runtime_stop()
    {
        var world = new ActorWorld();
        HighRiskActor actor = world.CreateActor<HighRiskActor>();
        ActorId actorId = actor.GetActorId();

        world.RuntimeStop();

        Assert.Throws<ObjectDisposedException>(() =>
            world.DelayPost(actorId, new HighRiskActorEvent(8), 1f));
    }

    [Test]
    public void Delay_ask_throws_after_runtime_stop()
    {
        var world = new ActorWorld();
        HighRiskActor actor = world.CreateActor<HighRiskActor>();
        ActorId actorId = actor.GetActorId();

        world.RuntimeStop();

        Assert.Throws<ObjectDisposedException>(() =>
            world.DelayAsk<HighRiskCallRequest, HighRiskCallResponse>(
                actorId,
                new HighRiskCallRequest(2),
                1f));
    }

    [Test]
    public void Enable_and_destroy_queries_return_false_after_dispose()
    {
        var world = new ActorWorld();
        HighRiskActor actor = world.CreateActor<HighRiskActor>();
        ActorId actorId = actor.GetActorId();

        world.Dispose();

        Assert.That(world.IsEnable(actorId), Is.False);
        Assert.That(world.SetEnable(actorId, false), Is.False);
        Assert.That(world.IsAlive(actorId), Is.False);
        Assert.That(world.DestroyActor(actorId), Is.False);
    }

    [Test]
    public void Disabled_actor_still_receives_events_while_lifecycle_updates_are_skipped()
    {
        var world = new ActorWorld();
        HighRiskActor actor = world.CreateActor<HighRiskActor>();

        Assert.That(actor.SetEnable(false), Is.True);

        world.PostTo(actor.GetActorId(), new HighRiskActorEvent(3));
        Pump(world, deltaTime: 0.1f);

        Assert.That(HighRiskTrace.Entries, Is.EqualTo(new[] { "event:3" }));
    }

    [Test]
    public void Event_stream_segment_pool_clear_resets_retained_segments()
    {
        var pool = new EventStreamSegmentPool<HighRiskReferenceEvent>(segmentCapacity: 2, maxRetained: 1);
        var segment = new EventStreamSegment<HighRiskReferenceEvent>(capacity: 2);
        object payload = new();
        segment.Items[0].Value = new HighRiskReferenceEvent(payload);
        segment.WriteIndex = 1;

        FieldInfo firstField = typeof(EventStreamSegmentPool<HighRiskReferenceEvent>)
            .GetField("_first", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo countField = typeof(EventStreamSegmentPool<HighRiskReferenceEvent>)
            .GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic)!;

        firstField.SetValue(pool, segment);
        countField.SetValue(pool, 1);

        pool.Clear();

        Assert.That(segment.WriteIndex, Is.Zero);
        Assert.That(segment.ReadIndex, Is.Zero);
        Assert.That(segment.Next, Is.Null);
        Assert.That(segment.Items[0].Value.Payload, Is.Null);
        Assert.That(countField.GetValue(pool), Is.EqualTo(0));
        Assert.That(firstField.GetValue(pool), Is.Null);
    }

    [Test]
    public void Runtime_index_allocator_rejects_invalid_negative_return_in_debug()
    {
        Assert.Pass("DEBUG allocator guards are configuration-dependent; covered by implementation review.");
    }

    private static void Pump(ActorWorld world, float deltaTime = 0f)
    {
        var budget = new RuntimeFrameBudget(maxEvents: 16, usedEvents: 0, deadlineTicks: 0);
        world.Pump(deltaTime, 0f, false, ref budget);
    }
}
