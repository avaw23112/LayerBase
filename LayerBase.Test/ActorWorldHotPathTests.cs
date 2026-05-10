using LayerBase.Actor;

namespace LayerBase.Test;

[TestFixture]
public sealed partial class ActorWorldHotPathTests
{
    [Test]
    public void Default_pump_options_prioritize_throughput()
    {
        ActorMailPumpOptions options = ActorMailPumpOptions.Default;

        Assert.That(options.MaxMailsPerBucketPerPump, Is.EqualTo(0));
        Assert.That(options.MaxMailsPerActorPerPump, Is.EqualTo(0));
        Assert.That(options.TimeCheckInterval, Is.EqualTo(64));
    }

    [Test]
    public void Fair_pump_options_preserve_old_limits()
    {
        ActorMailPumpOptions options = ActorMailPumpOptions.Fair;

        Assert.That(options.MaxMailsPerBucketPerPump, Is.EqualTo(128));
        Assert.That(options.MaxMailsPerActorPerPump, Is.EqualTo(8));
        Assert.That(options.TimeCheckInterval, Is.EqualTo(16));
    }

    [Test]
    public void Pump_option_time_check_interval_is_clamped()
    {
        ActorMailPumpOptions options = new(
            maxTotalMailsPerPump: 1,
            maxMailsPerBucketPerPump: 0,
            maxMailsPerActorPerPump: 0,
            maxEmptyBucketChecksPerPump: 1,
            timeCheckInterval: 0);

        Assert.That(options.TimeCheckInterval, Is.EqualTo(1));
    }

    [Test]
    public void Default_pump_options_do_not_apply_actor_or_bucket_limits()
    {
        var world = new ActorWorld();
        HotPathProbeActor actorA = world.CreateActor<HotPathProbeActor>();
        HotPathProbeActor actorB = world.CreateActor<HotPathProbeActor>();

        HotPathTrace.Values.Clear();
        actorA.PostInside(new HotPathEvent(1));
        actorA.PostInside(new HotPathEvent(2));
        actorB.PostInside(new HotPathEvent(10));
        actorB.PostInside(new HotPathEvent(11));

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(HotPathTrace.Values, Is.EqualTo(new[] { 1, 10, 2, 11 }));
        Assert.That(world.LastMailPumpStats.ProcessedTotal, Is.EqualTo(4));
        Assert.That(world.LastMailPumpStats.ActorLimitHits, Is.EqualTo(0));
        Assert.That(world.LastMailPumpStats.BucketLimitHits, Is.EqualTo(0));
    }

    [Test]
    public void Actor_behaviour_registers_archetype_row_during_creation()
    {
        var world = new ActorWorld();
        ActorId actorId = world.CreateActor<RowBoundProbeActor>().GetActorId();

        EventPostRow<RowBoundEvent> row = GetBoundRow<RowBoundEvent>(world, actorId.ArchetypeId);

        Assert.That(row.IsValid, Is.True);
        Assert.That(row.Mails.Length, Is.GreaterThan(actorId.SlotIndex));
    }
    
    [Test]
    public void Same_signature_different_actor_types_use_distinct_archetype_rows_and_share_world_pool()
    {
        var world = new ActorWorld();
        ActorId actorA = world.CreateActor<SharedPoolActorA>().GetActorId();
        ActorId actorB = world.CreateActor<SharedPoolActorB>().GetActorId();

        EventPostRow<SharedPoolEvent> rowA = GetBoundRow<SharedPoolEvent>(world, actorA.ArchetypeId);
        EventPostRow<SharedPoolEvent> rowB = GetBoundRow<SharedPoolEvent>(world, actorB.ArchetypeId);
        EventPostState<SharedPoolEvent>? state = EventPostRuntime<SharedPoolEvent>.GetState(world);

        Assert.That(actorA.ArchetypeId, Is.Not.EqualTo(actorB.ArchetypeId));
        Assert.That(state, Is.Not.Null);
        Assert.That(rowA.IsValid && rowB.IsValid, Is.True);
        Assert.That(state!.Pool, Is.Not.Null);
    }

    private static EventPostRow<TEvent> GetBoundRow<TEvent>(ActorWorld world, int archetypeId)
        where TEvent : struct
    {
        EventPostState<TEvent>? state = EventPostRuntime<TEvent>.GetState(world);
        Assert.That(state, Is.Not.Null);
        EventPostRow<TEvent>[]? rows = state!.RowsByArchetype;
        Assert.That((uint)archetypeId, Is.LessThan((uint)rows!.Length));
        return rows[archetypeId];
    }

    private readonly struct HotPathEvent
    {
        public HotPathEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private static class HotPathTrace
    {
        public static List<int> Values { get; } = new();
    }

    private sealed partial class HotPathProbeActor : IActor
    {
        [ActorBehaviour]
        private void OnEvent(in HotPathEvent value)
        {
            HotPathTrace.Values.Add(value.Value);
        }
    }

    private readonly struct RowBoundEvent
    {
        public RowBoundEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private readonly struct FastPostEvent
    {
        public FastPostEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private readonly struct SharedPoolEvent
    {
    }

    private sealed partial class RowBoundProbeActor : IActor
    {
        [ActorBehaviour]
        private void OnEvent(in RowBoundEvent value)
        {
        }
    }

    private sealed partial class FastPostProbeActor : IActor
    {
        [ActorBehaviour]
        private void OnEvent(in FastPostEvent value)
        {
        }
    }

    private sealed partial class SharedPoolActorA : IActor
    {
        [ActorBehaviour]
        private void OnEvent(in SharedPoolEvent value)
        {
        }
    }

    private sealed partial class SharedPoolActorB : IActor
    {
        [ActorBehaviour]
        private void OnEvent(in SharedPoolEvent value)
        {
        }
    }
}
