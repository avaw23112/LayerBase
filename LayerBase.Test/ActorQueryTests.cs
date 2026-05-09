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

public readonly struct ActorEnemyTag : IActorTag
{
}

public readonly struct ActorFriendlyTag : IActorTag
{
}

public readonly struct ActorDamageableTag : IActorTag
{
}

public readonly struct ActorBattleGroup : IActorGroup
{
}

public readonly struct ActorUiGroup : IActorGroup
{
}

internal static class ActorQueryTrace
{
    public static List<string> Entries { get; } = new();
}

[Tag<ActorEnemyTag>]
[Tag<ActorDamageableTag>]
[Group<ActorBattleGroup>]
internal sealed partial class ActorDamageOnly : IActor
{
    [ActorBehaviour]
    private void OnDamage(in ActorQueryDamageEvent value)
    {
        ActorQueryTrace.Entries.Add($"damage-only:{value.Value}");
    }
}

[Tag<ActorFriendlyTag>]
[Tag<ActorDamageableTag>]
[Group<ActorBattleGroup>]
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

[Group<ActorUiGroup>]
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
    public void Query_builder_filters_by_tags_and_groups()
    {
        var world = new ActorWorld();
        world.CreateActor<ActorDamageOnly>();
        world.CreateActor<ActorDamageAndDead>();
        world.CreateActor<ActorDeadOnly>();

        ActorQueryResult enemyBattleActors = world.Query()
            .AllBehaviours<ActorQueryDamageEvent>()
            .AllTags<ActorEnemyTag>()
            .AllGroups<ActorBattleGroup>()
            .Build();

        Type[] actorTypes = enemyBattleActors.DebugActors.Select(static actor => actor.GetType()).ToArray();
        Assert.That(actorTypes, Is.EqualTo(new[] { typeof(ActorDamageOnly) }));
    }

    [Test]
    public void Query_builder_supports_exclusion_filters()
    {
        var world = new ActorWorld();
        world.CreateActor<ActorDamageOnly>();
        world.CreateActor<ActorDamageAndDead>();
        world.CreateActor<ActorDeadOnly>();

        ActorQueryResult query = world.Query()
            .AllBehaviours<ActorQueryDamageEvent>()
            .NoneTags<ActorFriendlyTag>()
            .NoneGroups<ActorUiGroup>()
            .Build();

        Type[] actorTypes = query.DebugActors.Select(static actor => actor.GetType()).ToArray();
        Assert.That(actorTypes, Is.EqualTo(new[] { typeof(ActorDamageOnly) }));
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
    public void RefreshIfNeeded_rebuilds_stale_query_results_after_new_archetype_creation()
    {
        var world = new ActorWorld();
        world.CreateActor<ActorDamageOnly>();

        ActorQueryResult stale = world.Query().AllBehaviours<ActorQueryDamageEvent>().Build();
        Assert.That(stale.IsValid, Is.True);

        world.CreateActor<ActorDamageAndDead>();

        Assert.That(stale.IsValid, Is.False);

        ActorQueryResult refreshed = stale.RefreshIfNeeded();
        Assert.That(refreshed.IsValid, Is.True);
        Assert.That(refreshed.DebugActors.Count(), Is.EqualTo(2));
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

    [Test]
    public void ForEachActor_iterates_matching_actor_type_without_materializing_lists()
    {
        var world = new ActorWorld();
        world.CreateActor<ActorDamageOnly>();
        world.CreateActor<ActorDamageOnly>();
        world.CreateActor<ActorDamageAndDead>();

        ActorQueryResult query = world.Query().AllBehaviours<ActorQueryDamageEvent>().Build();
        int count = 0;

        query.ForEachActor<ActorDamageOnly, int>(
            ref count,
            static (ActorDamageOnly _, ref int state) => state++);

        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void ForEachStorage_exposes_storage_arrays_for_matching_actor_type()
    {
        var world = new ActorWorld();
        world.CreateActor<ActorDamageOnly>();
        world.CreateActor<ActorDamageOnly>();
        world.CreateActor<ActorDamageAndDead>();

        ActorQueryResult query = world.Query().AllBehaviours<ActorQueryDamageEvent>().Build();
        int aliveCount = 0;

        query.ForEachStorage<ActorDamageOnly, int>(
            ref aliveCount,
            static (ActorDamageOnly?[] actors, ActorSlotState[] states, bool[] _, int maxSlot, ref int state) =>
            {
                for (int i = 0; i < maxSlot; i++)
                {
                    if (states[i] == ActorSlotState.Alive && actors[i] != null)
                    {
                        state++;
                    }
                }
            });

        Assert.That(aliveCount, Is.EqualTo(2));
    }

    [Test]
    public void Pending_destroy_actor_is_filtered_from_query_traversal()
    {
        var world = new ActorWorld();
        ActorDamageOnly actorA = world.CreateActor<ActorDamageOnly>();
        world.CreateActor<ActorDamageOnly>();

        Assert.That(world.DestroyActor(actorA.GetActorId()), Is.True);

        ActorQueryResult query = world.Query().AllBehaviours<ActorQueryDamageEvent>().Build();
        int count = 0;
        query.ForEachActor<ActorDamageOnly>(_ => count++);

        Assert.That(count, Is.EqualTo(1));
    }
}
