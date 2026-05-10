using System.Reflection;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;

namespace LayerBase.Test;

public partial struct ActorMetaConfiguredEvent
{
    public int Value;

    public ActorMetaConfiguredEvent(int value)
    {
        Value = value;
    }
}

public partial struct ActorMetaDefaultEvent
{
    public int Value;

    public ActorMetaDefaultEvent(int value)
    {
        Value = value;
    }
}

public sealed class ActorMetaConfiguredEventMetaData : EventMetaData<ActorMetaConfiguredEvent>
{
    public override ActorMailOptions? ActorMailOptions => new ActorMailOptions(
        postPolicy: ActorPostPolicy.Latest,
        fullPolicy: ActorMailFullPolicy.DropNewest,
        growFailurePolicy: ActorMailFullPolicy.DropOldest,
        initialCapacity: 2,
        maxCapacity: 8,
        growFactor: 2,
        releaseWhenEmpty: false);
}

internal static class ActorMetaDataTrace
{
    public static List<int> Values { get; } = new();
}

internal sealed partial class ActorMetaDataActor : IActor
{
    [ActorBehaviour]
    private void OnConfigured(in ActorMetaConfiguredEvent value)
    {
        ActorMetaDataTrace.Values.Add(value.Value);
    }

    [ActorBehaviour]
    private void OnDefault(in ActorMetaDefaultEvent value)
    {
        ActorMetaDataTrace.Values.Add(value.Value);
    }
}

[TestFixture]
public class ActorMetaDataIntegrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
        ActorMetaDataTrace.Values.Clear();
    }

    [Test]
    public void Event_meta_data_actor_mail_options_are_loaded_into_policy_table()
    {
        EventMetaDataHandler.RegisterMetaData<ActorMetaConfiguredEvent>(new ActorMetaConfiguredEventMetaData());

        LayerRuntime runtime = BuildRuntime();
        ActorMailOptions options = runtime.PolicyTable.GetActorMailOptions(EventTypeId<ActorMetaConfiguredEvent>.Id);

        Assert.That(options.PostPolicy, Is.EqualTo(ActorPostPolicy.Latest));
        Assert.That(options.FullPolicy, Is.EqualTo(ActorMailFullPolicy.DropNewest));
        Assert.That(options.GrowFailurePolicy, Is.EqualTo(ActorMailFullPolicy.DropOldest));
        Assert.That(options.InitialCapacity, Is.EqualTo(2));
        Assert.That(options.MaxCapacity, Is.EqualTo(8));
        Assert.That(options.ReleaseWhenEmpty, Is.False);
    }

    [Test]
    public void Actor_world_reads_mail_options_when_creating_event_column()
    {
        EventMetaDataHandler.RegisterMetaData<ActorMetaConfiguredEvent>(new ActorMetaConfiguredEventMetaData());

        LayerRuntime runtime = BuildRuntime();
        ActorMetaDataActor actor = runtime.Actors.CreateActor<ActorMetaDataActor>();
        ActorId actorId = actor.GetActorId();

        ActorMailOptions configured = GetColumnOptions<ActorMetaConfiguredEvent>(runtime.Actors, actorId);
        ActorMailOptions fallback = GetColumnOptions<ActorMetaDefaultEvent>(runtime.Actors, actorId);

        Assert.That(configured.PostPolicy, Is.EqualTo(ActorPostPolicy.Latest));
        Assert.That(configured.FullPolicy, Is.EqualTo(ActorMailFullPolicy.DropNewest));
        Assert.That(fallback.PostPolicy, Is.EqualTo(ActorMailOptions.Default.PostPolicy));
        Assert.That(fallback.FullPolicy, Is.EqualTo(ActorMailOptions.Default.FullPolicy));
    }

    [Test]
    public void Post_and_pump_hot_path_do_not_requery_event_meta_data_after_column_creation()
    {
        EventMetaDataHandler.RegisterMetaData<ActorMetaConfiguredEvent>(new ActorMetaConfiguredEventMetaData());

        LayerRuntime runtime = BuildRuntime();
        ActorMetaDataActor actor = runtime.Actors.CreateActor<ActorMetaDataActor>();

        runtime.PolicyTable.SetActorMailOptions(EventTypeId<ActorMetaConfiguredEvent>.Id, new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.RejectNew,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 4,
            maxCapacity: 4,
            growFactor: 2,
            releaseWhenEmpty: true));

        actor.PostInside(new ActorMetaConfiguredEvent(1));
        actor.PostInside(new ActorMetaConfiguredEvent(2));

        runtime.Pump(0.016f);

        Assert.That(ActorMetaDataTrace.Values, Is.EqualTo(new[] { 2 }));
        Assert.That(GetColumnOptions<ActorMetaConfiguredEvent>(runtime.Actors, actor.GetActorId()).PostPolicy, Is.EqualTo(ActorPostPolicy.Latest));
    }

    private static LayerRuntime BuildRuntime()
    {
        var runtime = new LayerRuntime(1);
        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(new ActorMetaLayer());
        return builder.Build();
    }

    private static ActorMailOptions GetColumnOptions<TEvent>(ActorWorld world, ActorId actorId)
        where TEvent : struct
    {
        object column = GetColumn(world, actorId, EventTypeId<TEvent>.Id);
        FieldInfo optionsField = column.GetType().GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (ActorMailOptions)optionsField.GetValue(column)!;
    }

    private static object GetColumn(ActorWorld world, ActorId actorId, int eventTypeId)
    {
        FieldInfo archetypesField = typeof(ActorWorld).GetField("_archetypes", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array archetypes = (Array)archetypesField.GetValue(world)!;
        object archetype = archetypes.GetValue(actorId.ArchetypeId)!;

        FieldInfo storagesField = archetype.GetType().GetField("_storages", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array storages = (Array)storagesField.GetValue(archetype)!;
        object storage = storages.GetValue(0)!;

        FieldInfo columnsField = storage.GetType().GetField("_columnsByEventId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array columns = (Array)columnsField.GetValue(storage)!;
        return columns.GetValue(eventTypeId)!;
    }

    private sealed class ActorMetaLayer : Layer
    {
    }
}
