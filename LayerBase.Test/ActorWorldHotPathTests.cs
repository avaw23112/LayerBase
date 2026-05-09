using LayerBase.Actor;
using LayerBase.Core.Event;
using System.Reflection;

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
    public void Prewarm_hot_actor_binds_fast_cache_during_creation()
    {
        var world = new ActorWorld();
        PrewarmHotProbeActor actor = world.CreateActor<PrewarmHotProbeActor>();

        Assert.That(IsFastCacheBound<PrewarmHotEvent>(world, actor.GetActorId().FastIndex), Is.True);
    }

    [Test]
    public void Hot_actor_binds_fast_cache_on_first_post()
    {
        var world = new ActorWorld();
        HotOnlyProbeActor actor = world.CreateActor<HotOnlyProbeActor>();
        int fastIndex = actor.GetActorId().FastIndex;

        Assert.That(IsFastCacheBound<HotOnlyEvent>(world, fastIndex), Is.False);

        Assert.That(actor.PostInside(new HotOnlyEvent(3)).IsSuccess, Is.True);

        Assert.That(IsFastCacheBound<HotOnlyEvent>(world, fastIndex), Is.True);
    }

    [Test]
    public void Same_event_columns_share_world_level_mail_pool()
    {
        var world = new ActorWorld();
        SharedPoolActorA actorA = world.CreateActor<SharedPoolActorA>();
        SharedPoolActorB actorB = world.CreateActor<SharedPoolActorB>();

        object columnA = GetEventColumn(world, actorA.GetActorId(), EventTypeId<SharedPoolEvent>.Id);
        object columnB = GetEventColumn(world, actorB.GetActorId(), EventTypeId<SharedPoolEvent>.Id);

        object? poolA = columnA.GetType().GetField("_mailPool", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(columnA);
        object? poolB = columnB.GetType().GetField("_mailPool", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(columnB);

        Assert.That(poolA, Is.SameAs(poolB));
    }

    [Test]
    public void Destroy_and_recreate_reuses_fast_index_without_leaking_old_cache()
    {
        var world = new ActorWorld();
        PrewarmHotProbeActor actor = world.CreateActor<PrewarmHotProbeActor>();
        ActorId oldId = actor.GetActorId();

        Assert.That(world.DestroyActor(oldId), Is.True);
        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        PrewarmHotProbeActor replacement = world.CreateActor<PrewarmHotProbeActor>();
        ActorId newId = replacement.GetActorId();

        Assert.That(newId.FastIndex, Is.EqualTo(oldId.FastIndex));
        Assert.That(newId.Generation, Is.GreaterThan(oldId.Generation));
        Assert.That(world.TryPostTo(oldId, new PrewarmHotEvent(9)).IsSuccess, Is.False);
        Assert.That(IsFastCacheBound<PrewarmHotEvent>(world, newId.FastIndex), Is.True);
    }

    private static bool IsFastCacheBound<TEvent>(ActorWorld world, int fastIndex)
        where TEvent : struct
    {
        FieldInfo fastCachesField = typeof(ActorWorld).GetField("_fastCachesByEventId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array caches = (Array)fastCachesField.GetValue(world)!;
        int eventId = EventTypeId<TEvent>.Id;
        if ((uint)eventId >= (uint)caches.Length)
        {
            return false;
        }

        object? cache = caches.GetValue(eventId);
        if (cache == null)
        {
            return false;
        }

        byte[] states = (byte[])cache.GetType().GetField("_states", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cache)!;
        return (uint)fastIndex < (uint)states.Length && states[fastIndex] == 1;
    }

    private static object GetEventColumn(ActorWorld world, ActorId actorId, int eventTypeId)
    {
        FieldInfo archetypesField = typeof(ActorWorld).GetField("_archetypes", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array archetypes = (Array)archetypesField.GetValue(world)!;
        object archetype = archetypes.GetValue(actorId.ArchetypeId)!;

        FieldInfo storagesField = archetype.GetType().GetField("_storages", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array storages = (Array)storagesField.GetValue(archetype)!;
        object storage = storages.GetValue(actorId.TypeStorageIndex)!;

        FieldInfo columnsField = storage.GetType().GetField("_columnsByEventId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array columns = (Array)columnsField.GetValue(storage)!;
        return columns.GetValue(eventTypeId)!;
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

    private readonly struct PrewarmHotEvent
    {
        public PrewarmHotEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private readonly struct HotOnlyEvent
    {
        public HotOnlyEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private readonly struct SharedPoolEvent
    {
    }

    private sealed partial class PrewarmHotProbeActor : IActor
    {
        [ActorBehaviour(BehaviourType.PrewarmHot)]
        private void OnEvent(in PrewarmHotEvent value)
        {
        }
    }

    private sealed partial class HotOnlyProbeActor : IActor
    {
        [ActorBehaviour(BehaviourType.Hot)]
        private void OnEvent(in HotOnlyEvent value)
        {
        }
    }

    private sealed partial class SharedPoolActorA : IActor
    {
        [ActorBehaviour(BehaviourType.PrewarmHot)]
        private void OnEvent(in SharedPoolEvent value)
        {
        }
    }

    private sealed partial class SharedPoolActorB : IActor
    {
        [ActorBehaviour(BehaviourType.PrewarmHot)]
        private void OnEvent(in SharedPoolEvent value)
        {
        }
    }
}
