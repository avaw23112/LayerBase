using LayerBase.Actor;

namespace LayerBase.Test;

public struct ActorQueryDamageEvent
{
    public int Value;

    public ActorQueryDamageEvent(int value)
    {
        Value = value;
    }
}

public struct ActorQueryDeadEvent
{
    public int Value;

    public ActorQueryDeadEvent(int value)
    {
        Value = value;
    }
}

internal static class ActorQueryTrace
{
    public static List<string> Entries { get; } = new();
}

internal sealed partial class ActorDamageOnly : IActor
{
    [ActorBehaviour]
    private void OnDamage(in ActorQueryDamageEvent value)
    {
        ActorQueryTrace.Entries.Add($"damage-only:{value.Value}");
    }
}

internal sealed partial class ActorDamageAndDead : IActor
{
    [ActorBehaviour]
    private void OnDamage(in ActorQueryDamageEvent value)
    {
        ActorQueryTrace.Entries.Add($"damage-dead:{value.Value}");
    }

    [ActorBehaviour]
    private void OnDead(in ActorQueryDeadEvent value)
    {
        ActorQueryTrace.Entries.Add($"dead:{value.Value}");
    }
}

internal sealed partial class ActorDeadOnly : IActor
{
    [ActorBehaviour]
    private void OnDead(in ActorQueryDeadEvent value)
    {
        ActorQueryTrace.Entries.Add($"dead-only:{value.Value}");
    }
}

[TestFixture]
public class ActorQueryTests
{
    [SetUp]
    public void SetUp()
    {
        ActorQueryTrace.Entries.Clear();
    }

    [Test]
    public void QueryActor_single_event_matches_subset_supersets()
    {
        var world = new ActorWorld();
        world.CreateActor<ActorDamageOnly>();
        world.CreateActor<ActorDamageAndDead>();
        world.CreateActor<ActorDeadOnly>();

        ActorQueryResult query = world.QueryActor<ActorQueryDamageEvent>();
        Type[] actorTypes = query.DebugActors.Select(static actor => actor.GetType()).OrderBy(static t => t.Name).ToArray();

        Assert.That(actorTypes, Is.EqualTo(new[] { typeof(ActorDamageAndDead), typeof(ActorDamageOnly) }));
    }

    [Test]
    public void QueryActor_two_events_matches_only_actors_supporting_both()
    {
        var world = new ActorWorld();
        world.CreateActor<ActorDamageOnly>();
        world.CreateActor<ActorDamageAndDead>();
        world.CreateActor<ActorDeadOnly>();

        ActorQueryResult query = world.QueryActor<ActorQueryDamageEvent, ActorQueryDeadEvent>();
        Type[] actorTypes = query.DebugActors.Select(static actor => actor.GetType()).ToArray();

        Assert.That(actorTypes, Is.EqualTo(new[] { typeof(ActorDamageAndDead) }));
    }

    [Test]
    public void PostAll_posts_without_materializing_actor_list()
    {
        var world = new ActorWorld();
        world.CreateActor<ActorDamageOnly>();
        world.CreateActor<ActorDamageAndDead>();
        world.CreateActor<ActorDeadOnly>();

        ActorQueryResult query = world.QueryActor<ActorQueryDamageEvent>();
        query.PostAll(new ActorQueryDamageEvent(7));

        var budget = new RuntimeFrameBudget(32, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(ActorQueryTrace.Entries, Is.EqualTo(new[] { "damage-only:7", "damage-dead:7" }));
    }

    [Test]
    public void QueryCache_is_invalidated_when_new_matching_archetype_is_created()
    {
        var world = new ActorWorld();
        world.CreateActor<ActorDamageOnly>();

        ActorQueryResult before = world.QueryActor<ActorQueryDamageEvent>();
        Assert.That(before.DebugActors.Count(), Is.EqualTo(1));

        world.CreateActor<ActorDamageAndDead>();

        ActorQueryResult after = world.QueryActor<ActorQueryDamageEvent>();
        Assert.That(after.DebugActors.Count(), Is.EqualTo(2));
    }

    [Test]
    public void DebugActors_enumerates_live_actors_for_debugging()
    {
        var world = new ActorWorld();
        ActorDamageOnly actorA = world.CreateActor<ActorDamageOnly>();
        ActorDamageAndDead actorB = world.CreateActor<ActorDamageAndDead>();

        ActorQueryResult query = world.QueryActor<ActorQueryDamageEvent>();
        IActor[] actors = query.DebugActors.ToArray();

        Assert.That(actors, Does.Contain(actorA));
        Assert.That(actors, Does.Contain(actorB));
    }
}
