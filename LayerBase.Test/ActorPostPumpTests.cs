using LayerBase.Actor;
using LayerBase.Core.Event;

namespace LayerBase.Test;

public struct ActorDamageEvent
{
    public int Value;

    public ActorDamageEvent(int value)
    {
        Value = value;
    }
}

public struct ActorHealEvent
{
    public int Value;

    public ActorHealEvent(int value)
    {
        Value = value;
    }
}

[TestFixture]
public partial class ActorPostPumpTests
{
    [SetUp]
    public void SetUp()
    {
        RecordingActor.Trace.Clear();
        SecondaryRecordingActor.Trace.Clear();
        DualRecordingActor.Trace.Clear();
        ThrowingActor.Invocations = 0;
    }

    [Test]
    public void Post_does_not_invoke_until_pump()
    {
        var world = new ActorWorld();
        RecordingActor actor = world.CreateActor<RecordingActor>();

        actor.PostInside(new ActorDamageEvent(7));

        Assert.That(RecordingActor.Trace, Is.Empty);

        var budget = new RuntimeFrameBudget(maxEvents: 8, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(RecordingActor.Trace, Is.EqualTo(new[] { "R:7" }));
        Assert.That(budget.UsedEvents, Is.EqualTo(1));
    }

    [Test]
    public void Same_actor_same_event_is_processed_fifo()
    {
        var world = new ActorWorld();
        RecordingActor actor = world.CreateActor<RecordingActor>();

        actor.PostInside(new ActorDamageEvent(1));
        actor.PostInside(new ActorDamageEvent(2));
        actor.PostInside(new ActorDamageEvent(3));

        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(RecordingActor.Trace, Is.EqualTo(new[] { "R:1", "R:2", "R:3" }));
    }

    [Test]
    public void Invalid_physical_targets_fail_and_stale_generation_is_rejected()
    {
        var world = new ActorWorld();
        RecordingActor actor = world.CreateActor<RecordingActor>();
        ActorId validId = actor.GetActorId();

        // PostTo with invalid archetype should not throw
        world.PostTo(new ActorId(validId.ArchetypeId + 99, validId.SlotIndex, validId.Generation),
            new ActorDamageEvent(1));
        // PostTo with stale generation should not throw
        world.PostTo(new ActorId(validId.ArchetypeId, validId.SlotIndex, validId.Generation + 1),
            new ActorDamageEvent(1));
        // PostTo with unsupported event should not throw
        world.PostTo(validId, new ActorHealEvent(2));

        // Pump should not process any of the invalid posts
        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(RecordingActor.Trace, Is.Empty);
    }

    [Test]
    public void Pump_respects_budget_and_retains_remaining_mail()
    {
        var world = new ActorWorld();
        world.MailPumpOptions = ActorMailPumpOptions.Fair;
        RecordingActor actor = world.CreateActor<RecordingActor>();

        actor.PostInside(new ActorDamageEvent(10));
        actor.PostInside(new ActorDamageEvent(11));

        var budget = new RuntimeFrameBudget(maxEvents: 1, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(RecordingActor.Trace, Is.EqualTo(new[] { "R:10" }));
        Assert.That(budget.UsedEvents, Is.EqualTo(1));

        budget = new RuntimeFrameBudget(maxEvents: 1, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(RecordingActor.Trace, Is.EqualTo(new[] { "R:10", "R:11" }));
    }

    [Test]
    public void Event_bucket_round_robins_across_columns()
    {
        var world = new ActorWorld();
        world.MailPumpOptions  = ActorMailPumpOptions.Fair;
        RecordingActor actorA = world.CreateActor<RecordingActor>();
        SecondaryRecordingActor actorB = world.CreateActor<SecondaryRecordingActor>();

        actorA.PostInside(new ActorDamageEvent(1));
        actorA.PostInside(new ActorDamageEvent(2));
        actorB.PostInside(new ActorDamageEvent(100));

        var budget = new RuntimeFrameBudget(maxEvents: 2, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);

        string[] combined = RecordingActor.Trace.Concat(SecondaryRecordingActor.Trace).ToArray();
        Assert.That(combined, Has.Length.EqualTo(2));
        Assert.That(RecordingActor.Trace.Count, Is.EqualTo(1));
        Assert.That(SecondaryRecordingActor.Trace.Count, Is.EqualTo(1));
    }

    [Test]
    public void Actor_behaviour_exceptions_are_not_swallowed()
    {
        var world = new ActorWorld();
        ThrowingActor actor = world.CreateActor<ThrowingActor>();
        actor.PostInside(new ActorDamageEvent(5));

        Assert.Throws<InvalidOperationException>(() => PumpOnce(world));
        Assert.That(ThrowingActor.Invocations, Is.EqualTo(1));
    }

    [Test]
    public void Reject_new_is_used_when_mailbox_is_full()
    {
        var world = new ActorWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.RejectNew,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 4,
            maxCapacity: 4,
            growFactor: 2,
            releaseWhenEmpty: true));
        RecordingActor actor = world.CreateActor<RecordingActor>();

        // These should succeed without throwing
        actor.PostInside(new ActorDamageEvent(1));
        actor.PostInside(new ActorDamageEvent(2));
        actor.PostInside(new ActorDamageEvent(3));
        actor.PostInside(new ActorDamageEvent(4));

        // This should be rejected silently (mailbox full)
        actor.PostInside(new ActorDamageEvent(5));

        // Pump should only process the first 4 events
        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(RecordingActor.Trace, Is.EqualTo(new[] { "R:1", "R:2", "R:3", "R:4" }));
    }

    [Test]
    public void Query_postall_with_multiple_events_delivers_all_events_to_matching_actor()
    {
        var world = new ActorWorld();
        world.CreateActor<DualRecordingActor>();
        world.CreateActor<RecordingActor>();

        ActorQueryResult query = world.QueryActor<ActorDamageEvent, ActorHealEvent>();
        query.PostAll(new ActorDamageEvent(7), new ActorHealEvent(9));

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(DualRecordingActor.Trace, Is.EqualTo(new[] { "D:7", "H:9" }));
        Assert.That(RecordingActor.Trace, Is.Empty);
    }

    public sealed partial class RecordingActor : IActor
    {
        public static List<string> Trace { get; } = new();

        [ActorBehaviour]
        private void OnDamage(in ActorDamageEvent value)
        {
            Trace.Add($"R:{value.Value}");
        }
    }

    public sealed partial class SecondaryRecordingActor : IActor
    {
        public static List<string> Trace { get; } = new();

        [ActorBehaviour]
        private void OnDamage(in ActorDamageEvent value)
        {
            Trace.Add($"S:{value.Value}");
        }
    }

    public sealed partial class DualRecordingActor : IActor
    {
        public static List<string> Trace { get; } = new();

        [ActorBehaviour]
        private void OnDamage(in ActorDamageEvent value)
        {
            Trace.Add($"D:{value.Value}");
        }

        [ActorBehaviour]
        private void OnHeal(in ActorHealEvent value)
        {
            Trace.Add($"H:{value.Value}");
        }
    }

    public sealed partial class ThrowingActor : IActor
    {
        public static int Invocations { get; set; }

        [ActorBehaviour]
        private void OnDamage(in ActorDamageEvent value)
        {
            Invocations++;
            throw new InvalidOperationException("Boom");
        }
    }

    private static void PumpOnce(ActorWorld world)
    {
        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);
    }
}