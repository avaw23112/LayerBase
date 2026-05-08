using System.Reflection;
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

[TestFixture]
public class ActorMailPolicyTests
{
    [SetUp]
    public void SetUp()
    {
        ActorMailPolicyTrace.Values.Clear();
    }

    [Test]
    public void Latest_multiple_posts_only_process_last_value()
    {
        var world = CreateWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Latest,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 4,
            maxCapacity: 16,
            growFactor: 2,
            releaseWhenEmpty: true));

        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();
        actor.Post(new ActorMailPolicyEvent(1));
        actor.Post(new ActorMailPolicyEvent(2));
        actor.Post(new ActorMailPolicyEvent(3));

        Pump(world);

        Assert.That(ActorMailPolicyTrace.Values, Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public void Dirty_multiple_posts_only_trigger_once()
    {
        var world = CreateWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Dirty,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 4,
            maxCapacity: 16,
            growFactor: 2,
            releaseWhenEmpty: true));

        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();
        actor.Post(new ActorMailPolicyEvent(10));
        actor.Post(new ActorMailPolicyEvent(11));
        actor.Post(new ActorMailPolicyEvent(12));

        Pump(world);

        Assert.That(ActorMailPolicyTrace.Values, Has.Count.EqualTo(1));
    }

    [Test]
    public void Grow_expands_capacity_from_4_to_8_to_16()
    {
        var world = CreateWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 4,
            maxCapacity: 16,
            growFactor: 2,
            releaseWhenEmpty: false));

        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();
        ActorId actorId = actor.GetActorId();

        PostValues(world, actorId, 1, 5);
        Assert.That(GetMailField<int>(world, actorId, nameof(EventMail<ActorMailPolicyEvent>.Capacity)), Is.EqualTo(8));

        PostValues(world, actorId, 6, 9);
        Assert.That(GetMailField<int>(world, actorId, nameof(EventMail<ActorMailPolicyEvent>.Capacity)), Is.EqualTo(16));
    }

    [Test]
    public void Grow_reaching_max_capacity_follows_grow_failure_policy()
    {
        var world = CreateWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.DropOldest,
            initialCapacity: 2,
            maxCapacity: 2,
            growFactor: 2,
            releaseWhenEmpty: true));

        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();
        ActorId actorId = actor.GetActorId();

        Assert.That(world.TryPost(actorId, new ActorMailPolicyEvent(1)).IsSuccess, Is.True);
        Assert.That(world.TryPost(actorId, new ActorMailPolicyEvent(2)).IsSuccess, Is.True);
        Assert.That(world.TryPost(actorId, new ActorMailPolicyEvent(3)).IsSuccess, Is.True);

        Pump(world);

        Assert.That(ActorMailPolicyTrace.Values, Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public void Reject_new_returns_failure()
    {
        var world = CreateWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.RejectNew,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 2,
            maxCapacity: 2,
            growFactor: 2,
            releaseWhenEmpty: true));

        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();
        ActorId actorId = actor.GetActorId();

        Assert.That(world.TryPost(actorId, new ActorMailPolicyEvent(1)).IsSuccess, Is.True);
        Assert.That(world.TryPost(actorId, new ActorMailPolicyEvent(2)).IsSuccess, Is.True);
        Assert.That(world.TryPost(actorId, new ActorMailPolicyEvent(3)).IsSuccess, Is.False);
    }

    [Test]
    public void Drop_oldest_discards_old_items_and_keeps_newer_ones()
    {
        var world = CreateWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.DropOldest,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 2,
            maxCapacity: 2,
            growFactor: 2,
            releaseWhenEmpty: true));

        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();
        ActorId actorId = actor.GetActorId();

        world.TryPost(actorId, new ActorMailPolicyEvent(1));
        world.TryPost(actorId, new ActorMailPolicyEvent(2));
        world.TryPost(actorId, new ActorMailPolicyEvent(3));

        Pump(world);

        Assert.That(ActorMailPolicyTrace.Values, Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public void Drop_newest_discards_new_items_and_keeps_existing_ones()
    {
        var world = CreateWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.DropNewest,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 2,
            maxCapacity: 2,
            growFactor: 2,
            releaseWhenEmpty: true));

        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();
        ActorId actorId = actor.GetActorId();

        world.TryPost(actorId, new ActorMailPolicyEvent(1));
        world.TryPost(actorId, new ActorMailPolicyEvent(2));
        world.TryPost(actorId, new ActorMailPolicyEvent(3));

        Pump(world);

        Assert.That(ActorMailPolicyTrace.Values, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Release_when_empty_returns_buffer_to_pool()
    {
        var world = CreateWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 4,
            maxCapacity: 16,
            growFactor: 2,
            releaseWhenEmpty: true));

        ActorMailPolicyActor actor = world.CreateActor<ActorMailPolicyActor>();
        ActorId actorId = actor.GetActorId();

        world.TryPost(actorId, new ActorMailPolicyEvent(1));
        Pump(world);

        Assert.That(GetMailField<int>(world, actorId, nameof(EventMail<ActorMailPolicyEvent>.BufferId)), Is.EqualTo(0));
        Assert.That(GetMailField<int>(world, actorId, nameof(EventMail<ActorMailPolicyEvent>.Count)), Is.EqualTo(0));
    }

    private static ActorWorld CreateWorld(ActorMailOptions options)
    {
        return new ActorWorld(options);
    }

    private static void PostValues(ActorWorld world, ActorId actorId, int fromInclusive, int toInclusive)
    {
        for (int i = fromInclusive; i <= toInclusive; i++)
        {
            PostResult result = world.TryPost(actorId, new ActorMailPolicyEvent(i));
            Assert.That(result.IsSuccess, Is.True, $"Failed to post value {i}.");
        }
    }

    private static void Pump(ActorWorld world)
    {
        var budget = new RuntimeFrameBudget(64, 0, 0);
        world.Pump(0f, 0f, false, ref budget);
    }

    private static TField GetMailField<TField>(ActorWorld world, ActorId actorId, string fieldName)
    {
        object mail = GetMailBoxed(world, actorId);
        FieldInfo field = mail.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (TField)field.GetValue(mail)!;
    }

    private static object GetMailBoxed(ActorWorld world, ActorId actorId)
    {
        FieldInfo archetypesField = typeof(ActorWorld).GetField("_archetypes", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array archetypes = (Array)archetypesField.GetValue(world)!;
        object archetype = archetypes.GetValue(actorId.ArchetypeId)!;

        FieldInfo storagesField = archetype.GetType().GetField("_storages", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array storages = (Array)storagesField.GetValue(archetype)!;
        object storage = storages.GetValue(actorId.TypeStorageIndex)!;

        int eventId = EventTypeId<ActorMailPolicyEvent>.Id;
        FieldInfo columnsField = storage.GetType().GetField("_columnsByEventId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array columns = (Array)columnsField.GetValue(storage)!;
        object column = columns.GetValue(eventId)!;

        FieldInfo mailsField = column.GetType().GetField("_mails", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array mails = (Array)mailsField.GetValue(column)!;
        return mails.GetValue(actorId.SlotIndex)!;
    }
}
