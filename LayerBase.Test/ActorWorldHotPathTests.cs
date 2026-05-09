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
        actorA.Post(new HotPathEvent(1));
        actorA.Post(new HotPathEvent(2));
        actorB.Post(new HotPathEvent(10));
        actorB.Post(new HotPathEvent(11));

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(HotPathTrace.Values, Is.EqualTo(new[] { 1, 10, 2, 11 }));
        Assert.That(world.LastMailPumpStats.ProcessedTotal, Is.EqualTo(4));
        Assert.That(world.LastMailPumpStats.ActorLimitHits, Is.EqualTo(0));
        Assert.That(world.LastMailPumpStats.BucketLimitHits, Is.EqualTo(0));
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
}
