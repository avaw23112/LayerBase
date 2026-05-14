using LayerBase.Actor;
using LayerBase.Core.Event;

namespace LayerBase.Test;

public struct ActorMailPolicyEvent
{
    public int Value;

    public ActorMailPolicyEvent(int value)
    {
        Value = value;
    }
}

internal static class ActorMailPolicyTrace
{
    public static List<int> Values { get; } = new();
}

internal sealed partial class ActorMailPolicyActor : IActor
{
    [ActorBehaviour]
    private void OnPolicy(in ActorMailPolicyEvent value)
    {
        ActorMailPolicyTrace.Values.Add(value.Value);
    }
}

internal sealed partial class ActorMailPolicyOtherActor : IActor
{
    [ActorBehaviour]
    private void OnPolicy(in ActorMailPolicyEvent value)
    {
        ActorMailPolicyTrace.Values.Add(value.Value + 1000);
    }
}

[TestFixture]
public class ActorMailPolicyTests
{
    [SetUp]
    public void SetUp()
    {
        ActorMailPolicyTrace.Values.Clear();
    }

    [Test]
    public void Queued_posts_are_processed_in_fifo_order()
    {
        var world = new ActorWorld();
        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();

        actor.PostInside(new ActorMailPolicyEvent(1));
        actor.PostInside(new ActorMailPolicyEvent(2));
        actor.PostInside(new ActorMailPolicyEvent(3));

        Pump(world);

        Assert.That(ActorMailPolicyTrace.Values, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Multiple_posts_to_same_actor_all_get_processed()
    {
        var world = new ActorWorld();
        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();

        actor.PostInside(new ActorMailPolicyEvent(10));
        actor.PostInside(new ActorMailPolicyEvent(20));
        actor.PostInside(new ActorMailPolicyEvent(30));
        actor.PostInside(new ActorMailPolicyEvent(40));

        Pump(world);

        Assert.That(ActorMailPolicyTrace.Values, Is.EqualTo(new[] { 10, 20, 30, 40 }));
    }

    [Test]
    public void Multiple_actors_receive_their_own_events()
    {
        var world = new ActorWorld();
        ActorMailPolicyActor actorA = world.CreateActor<ActorMailPolicyActor>();
        ActorMailPolicyOtherActor actorB = world.CreateActor<ActorMailPolicyOtherActor>();

        actorA.PostInside(new ActorMailPolicyEvent(1));
        actorB.PostInside(new ActorMailPolicyEvent(2));

        Pump(world);

        Assert.That(ActorMailPolicyTrace.Values, Does.Contain(1));
        Assert.That(ActorMailPolicyTrace.Values, Does.Contain(1002));
    }

    [Test]
    public void Events_are_not_processed_until_pump()
    {
        var world = new ActorWorld();
        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();

        actor.PostInside(new ActorMailPolicyEvent(42));

        Assert.That(ActorMailPolicyTrace.Values, Is.Empty);

        Pump(world);

        Assert.That(ActorMailPolicyTrace.Values, Is.EqualTo(new[] { 42 }));
    }

    [Test]
    public void Pump_respects_budget()
    {
        var world = new ActorWorld();
        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();

        actor.PostInside(new ActorMailPolicyEvent(1));
        actor.PostInside(new ActorMailPolicyEvent(2));
        actor.PostInside(new ActorMailPolicyEvent(3));

        // Only process 2 events
        var budget = new RuntimeFrameBudget(maxEvents: 2, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(ActorMailPolicyTrace.Values, Is.EqualTo(new[] { 1, 2 }));

        // Process remaining
        budget = new RuntimeFrameBudget(maxEvents: 10, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(ActorMailPolicyTrace.Values, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    private static void Pump(ActorWorld world)
    {
        var budget = new RuntimeFrameBudget(maxEvents: 64, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);
    }
}
