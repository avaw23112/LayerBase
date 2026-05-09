using LayerBase.Actor;
using LayerBase.Core.Event;

namespace LayerBase.Test;

public struct ActorHardeningEvent
{
    public int Value;

    public ActorHardeningEvent(int value)
    {
        Value = value;
    }
}

public struct ActorHardeningMergeEvent : IActorMailMerge<ActorHardeningMergeEvent>
{
    public int Value;

    public ActorHardeningMergeEvent(int value)
    {
        Value = value;
    }

    public ActorHardeningMergeEvent Merge(in ActorHardeningMergeEvent oldValue, in ActorHardeningMergeEvent newValue)
    {
        return new ActorHardeningMergeEvent(oldValue.Value + newValue.Value);
    }
}

public readonly struct ActorHardeningTag : IActorTag
{
}

public readonly struct ActorHardeningGroup : IActorGroup
{
}

internal static class ActorHardeningTrace
{
    public static List<string> Entries { get; } = new();

    public static void Reset()
    {
        Entries.Clear();
        HardeningPooledActor.RentCount = 0;
        HardeningPooledActor.ReturnCount = 0;
    }
}

[Tag<ActorHardeningTag>]
[Group<ActorHardeningGroup>]
internal sealed partial class HardeningProbeActor : IActor, LayerBase.Actor.IUpdate, IDestroy
{
    [ActorBehaviour]
    private void OnEvent(in ActorHardeningEvent value)
    {
        ActorHardeningTrace.Entries.Add($"event:{value.Value}");
    }

    [ActorBehaviour]
    private void OnMerge(in ActorHardeningMergeEvent value)
    {
        ActorHardeningTrace.Entries.Add($"merge:{value.Value}");
    }

    void LayerBase.Actor.IUpdate.Update(float deltaTime)
    {
        ActorHardeningTrace.Entries.Add($"update:{deltaTime:0.###}");
    }

    public void Destroy()
    {
        ActorHardeningTrace.Entries.Add("destroy");
    }
}

[Tag<ActorHardeningTag>]
internal sealed partial class HardeningPooledActor : IPooledActor
{
    public static int RentCount { get; set; }
    public static int ReturnCount { get; set; }

    [ActorBehaviour]
    private void OnEvent(in ActorHardeningEvent value)
    {
    }

    public void OnRent()
    {
        RentCount++;
    }

    public void OnReturn()
    {
        ReturnCount++;
    }
}

[TestFixture]
public class ActorHardeningTests
{
    [SetUp]
    public void SetUp()
    {
        ActorHardeningTrace.Reset();
    }

    [Test]
    public void Debug_info_reports_pending_destroy_and_generation_mismatch()
    {
        var world = new ActorWorld();
        HardeningProbeActor actor = world.CreateActor<HardeningProbeActor>();
        ActorId actorId = actor.GetActorId();

        ActorDebugInfo aliveInfo = world.GetDebugInfo(actorId);
        Assert.That(aliveInfo.IsValid, Is.True);
        Assert.That(aliveInfo.IsAlive, Is.True);
        Assert.That(aliveInfo.Tags, Does.Contain(nameof(ActorHardeningTag)));
        Assert.That(aliveInfo.Groups, Does.Contain(nameof(ActorHardeningGroup)));

        Assert.That(world.DestroyActor(actorId), Is.True);

        ActorDebugInfo pendingInfo = world.GetDebugInfo(actorId);
        Assert.That(pendingInfo.IsValid, Is.True);
        Assert.That(pendingInfo.IsPendingDestroy, Is.True);

        PostResult pendingPost = world.TryPost(actorId, new ActorHardeningEvent(1));
        Assert.That(pendingPost.IsSuccess, Is.False);
        Assert.That(pendingPost.FailureKind, Is.EqualTo(PostFailureKind.PendingDestroy));

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        ActorDebugInfo staleInfo = world.GetDebugInfo(actorId);
        Assert.That(staleInfo.IsValid, Is.False);
        Assert.That(staleInfo.FailureReason, Does.Contain("Generation"));
    }

    [Test]
    public void Debug_dump_and_query_dump_include_actor_information()
    {
        var world = new ActorWorld();
        HardeningProbeActor actor = world.CreateActor<HardeningProbeActor>();
        ActorId actorId = actor.GetActorId();

        string description = world.DescribeActor(actorId);
        string worldDump = world.DumpActorWorld();
        string queryDump = world.DumpQuery(world.QueryActor<ActorHardeningEvent>());

        Assert.That(description, Does.Contain(nameof(HardeningProbeActor)));
        Assert.That(description, Does.Contain(nameof(ActorHardeningTag)));
        Assert.That(worldDump, Does.Contain(nameof(HardeningProbeActor)));
        Assert.That(queryDump, Does.Contain("AliveCount"));
    }

    [Test]
    public void Disabled_actor_mail_policy_can_reject_posts()
    {
        var world = new ActorWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 4,
            maxCapacity: 16,
            growFactor: 2,
            releaseWhenEmpty: true,
            disabledPolicy: ActorMailDisabledPolicy.Reject));

        HardeningProbeActor actor = world.CreateActor<HardeningProbeActor>();
        Assert.That(actor.SetEnable(false), Is.True);

        PostResult result = actor.TryPost(new ActorHardeningEvent(1));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailureKind, Is.EqualTo(PostFailureKind.DisabledActor));
    }

    [Test]
    public void Merge_policy_combines_values_into_single_delivery()
    {
        var world = new ActorWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Coalesced,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 4,
            maxCapacity: 16,
            growFactor: 2,
            releaseWhenEmpty: true));

        HardeningProbeActor actor = world.CreateActor<HardeningProbeActor>();
        actor.Post(new ActorHardeningMergeEvent(1));
        actor.Post(new ActorHardeningMergeEvent(2));
        actor.Post(new ActorHardeningMergeEvent(3));

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(ActorHardeningTrace.Entries, Is.EqualTo(new[] { "merge:6", "update:0" }));
    }

    [Test]
    public void Query_count_extensions_track_alive_enabled_and_empty()
    {
        var world = new ActorWorld();
        HardeningProbeActor actorA = world.CreateActor<HardeningProbeActor>();
        HardeningProbeActor actorB = world.CreateActor<HardeningProbeActor>();

        ActorQueryResult query = world.QueryActor<ActorHardeningEvent>();
        Assert.That(query.CountAlive(), Is.EqualTo(2));
        Assert.That(query.CountEnabled(), Is.EqualTo(2));
        Assert.That(query.IsEmpty(), Is.False);

        Assert.That(actorA.SetEnable(false), Is.True);
        Assert.That(query.CountEnabled(), Is.EqualTo(1));

        Assert.That(world.DestroyActor(actorA.GetActorId()), Is.True);
        Assert.That(world.DestroyActor(actorB.GetActorId()), Is.True);
        Assert.That(query.CountAlive(), Is.EqualTo(0));
        Assert.That(query.IsEmpty(), Is.True);
    }

    [Test]
    public void Pool_management_apis_expose_stats_and_limit_retention()
    {
        var world = new ActorWorld();

        world.SetPoolLimit<HardeningPooledActor>(2);
        world.PrewarmPool<HardeningPooledActor>(2);

        ActorPoolStats prewarmed = world.GetPoolStats<HardeningPooledActor>();
        Assert.That(prewarmed.AvailableCount, Is.EqualTo(2));

        HardeningPooledActor actor = world.CreatePooledActor<HardeningPooledActor>();
        ActorPoolStats rented = world.GetPoolStats<HardeningPooledActor>();
        Assert.That(rented.RentTotal, Is.EqualTo(1));

        Assert.That(world.DestroyActor(actor.GetActorId()), Is.True);
        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        ActorPoolStats returned = world.GetPoolStats<HardeningPooledActor>();
        Assert.That(returned.ReturnTotal, Is.EqualTo(1));
        Assert.That(returned.AvailableCount, Is.LessThanOrEqualTo(2));

        world.ClearPool<HardeningPooledActor>();
        Assert.That(world.GetPoolStats<HardeningPooledActor>().AvailableCount, Is.EqualTo(0));
    }

    [Test]
    public void Mail_pump_fairness_limits_single_actor_per_frame()
    {
        var world = new ActorWorld();
        world.MailPumpOptions = new ActorMailPumpOptions(
            maxTotalMailsPerPump: 8,
            maxMailsPerBucketPerPump: 8,
            maxMailsPerActorPerPump: 1,
            maxEmptyBucketChecksPerPump: 8);

        HardeningProbeActor actorA = world.CreateActor<HardeningProbeActor>();
        HardeningProbeActor actorB = world.CreateActor<HardeningProbeActor>();

        actorA.Post(new ActorHardeningEvent(1));
        actorA.Post(new ActorHardeningEvent(2));
        actorB.Post(new ActorHardeningEvent(10));
        actorB.Post(new ActorHardeningEvent(11));

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(ActorHardeningTrace.Entries, Does.Contain("event:1"));
        Assert.That(ActorHardeningTrace.Entries, Does.Contain("event:10"));
        Assert.That(ActorHardeningTrace.Entries, Does.Not.Contain("event:2"));
        Assert.That(ActorHardeningTrace.Entries, Does.Not.Contain("event:11"));
        Assert.That(world.LastMailPumpStats.ProcessedTotal, Is.EqualTo(2));
        Assert.That(world.LastMailPumpStats.ActorLimitHits, Is.GreaterThanOrEqualTo(1));
        Assert.That(world.LastMailPumpStats.RemainingDirtyBuckets, Is.GreaterThanOrEqualTo(1));
    }
}
