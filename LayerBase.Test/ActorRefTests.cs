using LayerBase.Actor;
using LayerBase.Core.Event;

namespace LayerBase.Test;

[TestFixture]
public sealed partial class ActorRefTests
{
    [SetUp]
    public void SetUp()
    {
        ActorRefTrace.Values.Clear();
    }

    [Test]
    public void ActorRef_post_routes_to_target_actor()
    {
        var world = new ActorWorld();
        ActorRefProbeActor actor = world.CreateActor<ActorRefProbeActor>();

        ActorRef<ActorRefProbeActor> actorRef = world.GetActorRef<ActorRefProbeActor>(actor.GetActorId());
        PostResult result = actorRef.Post(new ActorRefEvent(7));

        Assert.That(result.IsSuccess, Is.True);

        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(ActorRefTrace.Values, Is.EqualTo(new[] { 7 }));
    }

    [Test]
    public void ActorEventRef_post_routes_without_column_lookup()
    {
        var world = new ActorWorld();
        ActorRefProbeActor actor = world.CreateActor<ActorRefProbeActor>();

        ActorEventRef<ActorRefProbeActor, ActorRefEvent> actorEventRef =
            world.GetActorEventRef<ActorRefProbeActor, ActorRefEvent>(actor.GetActorId());

        PostResult result = actorEventRef.Post(new ActorRefEvent(9));
        Assert.That(result.IsSuccess, Is.True);

        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(ActorRefTrace.Values, Is.EqualTo(new[] { 9 }));
    }

    [Test]
    public void Stale_actor_ref_does_not_hit_reused_slot()
    {
        var world = new ActorWorld();
        ActorRefProbeActor first = world.CreateActor<ActorRefProbeActor>();
        ActorId firstId = first.GetActorId();
        ActorRef<ActorRefProbeActor> actorRef = world.GetActorRef<ActorRefProbeActor>(firstId);
        ActorEventRef<ActorRefProbeActor, ActorRefEvent> actorEventRef =
            world.GetActorEventRef<ActorRefProbeActor, ActorRefEvent>(firstId);

        Assert.That(world.DestroyActor(firstId), Is.True);
        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        ActorRefProbeActor second = world.CreateActor<ActorRefProbeActor>();
        Assert.That(second.GetActorId().SlotIndex, Is.EqualTo(firstId.SlotIndex));

        PostResult staleActorRefResult = actorRef.Post(new ActorRefEvent(1));
        PostResult staleActorEventRefResult = actorEventRef.Post(new ActorRefEvent(2));

        Assert.That(staleActorRefResult.IsSuccess, Is.False);
        Assert.That(staleActorEventRefResult.IsSuccess, Is.False);

        second.Post(new ActorRefEvent(3));
        budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(ActorRefTrace.Values, Is.EqualTo(new[] { 3 }));
    }

    private readonly struct ActorRefEvent
    {
        public ActorRefEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private static class ActorRefTrace
    {
        public static List<int> Values { get; } = new();
    }

    private sealed partial class ActorRefProbeActor : IActor
    {
        [ActorBehaviour]
        private void OnEvent(in ActorRefEvent value)
        {
            ActorRefTrace.Values.Add(value.Value);
        }
    }
}
