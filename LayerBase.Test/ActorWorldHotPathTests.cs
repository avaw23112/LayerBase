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
        Assert.That(row.Generations[actorId.SlotIndex], Is.EqualTo(actorId.Generation));
    }

    [Test]
    public void Actor_behaviour_can_use_post_fast_without_extra_binding_step()
    {
        var world = new ActorWorld();
        FastPostProbeActor actor = world.CreateActor<FastPostProbeActor>();

        Assert.That(actor.PostFastInside(new FastPostEvent(3)), Is.True);
    }

    [Test]
    public void Same_signature_different_actor_types_use_distinct_archetype_rows_and_share_world_pool()
    {
        var world = new ActorWorld();
        ActorId actorA = world.CreateActor<SharedPoolActorA>().GetActorId();
        ActorId actorB = world.CreateActor<SharedPoolActorB>().GetActorId();

        EventPostRow<SharedPoolEvent> rowA = GetBoundRow<SharedPoolEvent>(world, actorA.ArchetypeId);
        EventPostRow<SharedPoolEvent> rowB = GetBoundRow<SharedPoolEvent>(world, actorB.ArchetypeId);

        Assert.That(actorA.ArchetypeId, Is.Not.EqualTo(actorB.ArchetypeId));
        Assert.That(rowA.Pool, Is.SameAs(rowB.Pool));
    }

    [Test]
    public void Destroy_and_recreate_reuses_slot_generation_guard_without_leaking_old_row_target()
    {
        var world = new ActorWorld();
        RowBoundProbeActor actor = world.CreateActor<RowBoundProbeActor>();
        ActorId oldId = actor.GetActorId();

        Assert.That(world.DestroyActor(oldId), Is.True);
        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        RowBoundProbeActor replacement = world.CreateActor<RowBoundProbeActor>();
        ActorId newId = replacement.GetActorId();
        EventPostRow<RowBoundEvent> row = GetBoundRow<RowBoundEvent>(world, newId.ArchetypeId);

        Assert.That(newId.Generation, Is.GreaterThan(oldId.Generation));
        Assert.That(world.TryPostTo(oldId, new RowBoundEvent(9)).IsSuccess, Is.False);
        Assert.That(row.Generations[newId.SlotIndex], Is.EqualTo(newId.Generation));
        Assert.That(world.PostFast(newId, new RowBoundEvent(10)), Is.True);
    }

    [Test]
    public void Event_post_rows_refresh_after_storage_growth()
    {
        var world = new ActorWorld();
        RowBoundProbeActor[] actors = new RowBoundProbeActor[8];
        for (int i = 0; i < actors.Length; i++)
        {
            actors[i] = world.CreateActor<RowBoundProbeActor>();
        }

        ActorId lastId = actors[^1].GetActorId();
        EventPostRow<RowBoundEvent> row = GetBoundRow<RowBoundEvent>(world, lastId.ArchetypeId);

        Assert.That(row.Mails.Length, Is.GreaterThanOrEqualTo(actors.Length));
        Assert.That(row.Generations.Length, Is.GreaterThanOrEqualTo(actors.Length));
        Assert.That(world.PostFast(lastId, new RowBoundEvent(11)), Is.True);
    }

    private static EventPostRow<TEvent> GetBoundRow<TEvent>(ActorWorld world, int archetypeId)
        where TEvent : struct
    {
        Assert.That(EventPostRuntime<TEvent>.TryGetRows(world, out EventPostRow<TEvent>[]? rows), Is.True);
        Assert.That(rows, Is.Not.Null);
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
