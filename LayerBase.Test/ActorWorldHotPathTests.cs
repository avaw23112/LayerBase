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
            timeCheckInterval: 0,
            maxEventCountPerPump: 64);

        Assert.That(options.TimeCheckInterval, Is.EqualTo(1));
    }

    [Test]
    public void Default_pump_options_process_all_events()
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

        Assert.That(HotPathTrace.Values.Count, Is.EqualTo(4));
    }

    [Test]
    public void Actor_creation_and_post_works_correctly()
    {
        var world = new ActorWorld();
        HotPathProbeActor actor = world.CreateActor<HotPathProbeActor>();

        HotPathTrace.Values.Clear();
        actor.PostInside(new HotPathEvent(42));

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(HotPathTrace.Values, Is.EqualTo(new[] { 42 }));
    }

    [Test]
    public void Multiple_actor_types_handle_their_own_events()
    {
        var world = new ActorWorld();
        HotPathProbeActor actorA = world.CreateActor<HotPathProbeActor>();
        SharedPoolActorA actorB = world.CreateActor<SharedPoolActorA>();

        HotPathTrace.Values.Clear();
        SharedPoolTraceA.Values.Clear();

        actorA.PostInside(new HotPathEvent(1));
        actorB.PostInside(new SharedPoolEvent(100));

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(HotPathTrace.Values, Is.EqualTo(new[] { 1 }));
        Assert.That(SharedPoolTraceA.Values, Is.EqualTo(new[] { 100 }));
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

    private readonly struct SharedPoolEvent
    {
        public SharedPoolEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private static class SharedPoolTraceA
    {
        public static List<int> Values { get; } = new();
    }

    private static class SharedPoolTraceB
    {
        public static List<int> Values { get; } = new();
    }

    private sealed partial class SharedPoolActorA : IActor
    {
        [ActorBehaviour]
        private void OnEvent(in SharedPoolEvent value)
        {
            SharedPoolTraceA.Values.Add(value.Value);
        }
    }

    private sealed partial class SharedPoolActorB : IActor
    {
        [ActorBehaviour]
        private void OnEvent(in SharedPoolEvent value)
        {
            SharedPoolTraceB.Values.Add(value.Value);
        }
    }
}
