using LayerBase.Actor;
using LayerBase.Core.Event;

namespace LayerBase.Test;

public struct ActorLifecycleEvent
{
    public int Value;

    public ActorLifecycleEvent(int value)
    {
        Value = value;
    }
}

internal static class ActorLifecycleTrace
{
    public static List<string> Entries { get; } = new();

    public static void Reset()
    {
        Entries.Clear();
        LifecycleProbeActor.BehaviourHook = null;
        LifecycleProbeActor.UpdateHook = null;
        LifecycleProbeActor.DestroyCount = 0;
    }
}

internal sealed partial class LifecycleProbeActor : IActor, IStart, LayerBase.Actor.IUpdate, ILateUpdate, LayerBase.Actor.IFixedUpdate, IDestroy
{
    public static Action<LifecycleProbeActor>? BehaviourHook { get; set; }
    public static Action<LifecycleProbeActor>? UpdateHook { get; set; }
    public static int DestroyCount { get; set; }

    [ActorBehaviour]
    private void OnEvent(in ActorLifecycleEvent value)
    {
        ActorLifecycleTrace.Entries.Add($"behaviour:{value.Value}");
        BehaviourHook?.Invoke(this);
    }

    public void Start()
    {
        ActorLifecycleTrace.Entries.Add("start");
    }

    void LayerBase.Actor.IUpdate.Update(float deltaTime)
    {
        ActorLifecycleTrace.Entries.Add($"update:{deltaTime:0.###}");
        UpdateHook?.Invoke(this);
    }

    public void LateUpdate(float deltaTime)
    {
        ActorLifecycleTrace.Entries.Add($"late:{deltaTime:0.###}");
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
        ActorLifecycleTrace.Entries.Add($"fixed:{fixedDeltaTime:0.###}");
    }

    public void Destroy()
    {
        DestroyCount++;
        ActorLifecycleTrace.Entries.Add("destroy");
    }
}

[TestFixture]
public class ActorLifecycleTests
{
    [SetUp]
    public void SetUp()
    {
        ActorLifecycleTrace.Reset();
    }

    [Test]
    public void Pump_orders_behaviour_start_fixed_update_and_late_update()
    {
        var world = new ActorWorld();
        LifecycleProbeActor actor = world.CreateActor<LifecycleProbeActor>();

        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "start" }));
        ActorLifecycleTrace.Entries.Clear();

        actor.PostInside(new ActorLifecycleEvent(1));

        var budget = new RuntimeFrameBudget(32, 0, 0);
        world.Pump(
            deltaTime: 0.5f,
            fixedDeltaTime: 0.25f,
            pumpFixedUpdate: true,
            budget: ref budget);

        Assert.That(
            ActorLifecycleTrace.Entries,
            Is.EqualTo(new[] { "behaviour:1", "fixed:0.25", "update:0.5", "late:0.5" }));

        ActorLifecycleTrace.Entries.Clear();
        budget = new RuntimeFrameBudget(32, 0, 0);
        world.Pump(
            deltaTime: 0.25f,
            fixedDeltaTime: 0.125f,
            pumpFixedUpdate: true,
            budget: ref budget);

        Assert.That(
            ActorLifecycleTrace.Entries,
            Is.EqualTo(new[] { "fixed:0.125", "update:0.25", "late:0.25" }));
    }

    [Test]
    public void SetEnable_controls_update_like_lifecycles_but_not_start_or_behaviour()
    {
        var world = new ActorWorld();
        LifecycleProbeActor actor = world.CreateActor<LifecycleProbeActor>();

        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "start" }));
        ActorLifecycleTrace.Entries.Clear();

        Assert.That(actor.GetEnable(), Is.True);
        Assert.That(actor.SetEnable(false), Is.True);
        Assert.That(actor.GetEnable(), Is.False);

        actor.PostInside(new ActorLifecycleEvent(3));

        var budget = new RuntimeFrameBudget(32, 0, 0);
        world.Pump(
            deltaTime: 1f,
            fixedDeltaTime: 0.5f,
            pumpFixedUpdate: true,
            budget: ref budget);

        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "behaviour:3" }));

        Assert.That(actor.SetEnable(true), Is.True);
        Assert.That(actor.GetEnable(), Is.True);

        ActorLifecycleTrace.Entries.Clear();
        budget = new RuntimeFrameBudget(32, 0, 0);
        world.Pump(
            deltaTime: 1f,
            fixedDeltaTime: 0.5f,
            pumpFixedUpdate: true,
            budget: ref budget);

        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "fixed:0.5", "update:1", "late:1" }));
    }

    [Test]
    public void DestroyActor_marks_pending_destroy_filters_queries_and_reuses_slot()
    {
        var world = new ActorWorld();
        LifecycleProbeActor actor = world.CreateActor<LifecycleProbeActor>();
        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "start" }));
        ActorLifecycleTrace.Entries.Clear();

        ActorId oldId = actor.GetActorId();

        Assert.That(world.DestroyActor(oldId), Is.True);
        Assert.That(world.IsAlive(oldId), Is.False);
        Assert.That(world.PostTo(oldId, new ActorLifecycleEvent(8)).IsSuccess, Is.True);

        ActorQueryResult query = world.QueryActor<ActorLifecycleEvent>();
        Assert.That(query.DebugActors, Is.Empty);

        var budget = new RuntimeFrameBudget(32, 0, 0);
        world.Pump(
            deltaTime: 0.1f,
            fixedDeltaTime: 0f,
            pumpFixedUpdate: false,
            budget: ref budget);

        Assert.That(LifecycleProbeActor.DestroyCount, Is.EqualTo(1));

        LifecycleProbeActor replacement = world.CreateActor<LifecycleProbeActor>();
        ActorId newId = replacement.GetActorId();

        Assert.That(newId.SlotIndex, Is.EqualTo(oldId.SlotIndex));
        Assert.That(newId.Generation, Is.GreaterThan(oldId.Generation));
        Assert.That(world.PostTo(oldId, new ActorLifecycleEvent(9)).IsSuccess, Is.True);
        Assert.That(world.IsAlive(newId), Is.True);
    }

    [Test]
    public void DestroyActor_clears_mail_and_dirty_slot_never_invokes_destroyed_behaviour()
    {
        var world = new ActorWorld();
        LifecycleProbeActor actor = world.CreateActor<LifecycleProbeActor>();
        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "start" }));
        ActorLifecycleTrace.Entries.Clear();

        actor.PostInside(new ActorLifecycleEvent(5));
        Assert.That(world.DestroyActor(actor.GetActorId()), Is.True);

        var budget = new RuntimeFrameBudget(32, 0, 0);
        world.Pump(
            deltaTime: 0.1f,
            fixedDeltaTime: 0f,
            pumpFixedUpdate: false,
            budget: ref budget);

        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "destroy" }));
        Assert.That(LifecycleProbeActor.DestroyCount, Is.EqualTo(1));
    }

    [Test]
    public void Destroy_requested_during_behaviour_skips_remaining_lifecycle_for_that_frame()
    {
        var world = new ActorWorld();
        LifecycleProbeActor actor = world.CreateActor<LifecycleProbeActor>();
        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "start" }));
        ActorLifecycleTrace.Entries.Clear();

        ActorId actorId = actor.GetActorId();
        LifecycleProbeActor.BehaviourHook = _ => world.DestroyActor(actorId);

        actor.PostInside(new ActorLifecycleEvent(11));

        var budget = new RuntimeFrameBudget(32, 0, 0);
        world.Pump(
            deltaTime: 0.2f,
            fixedDeltaTime: 0.1f,
            pumpFixedUpdate: true,
            budget: ref budget);

        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "behaviour:11", "destroy" }));
    }

    [Test]
    public void Destroy_requested_during_update_skips_late_update_in_same_frame()
    {
        var world = new ActorWorld();
        LifecycleProbeActor actor = world.CreateActor<LifecycleProbeActor>();
        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "start" }));
        ActorLifecycleTrace.Entries.Clear();

        ActorId actorId = actor.GetActorId();

        var budget = new RuntimeFrameBudget(32, 0, 0);
        world.Pump(
            deltaTime: 0.1f,
            fixedDeltaTime: 0f,
            pumpFixedUpdate: false,
            budget: ref budget);

        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "update:0.1", "late:0.1" }));
        ActorLifecycleTrace.Entries.Clear();
        LifecycleProbeActor.UpdateHook = _ => world.DestroyActor(actorId);

        budget = new RuntimeFrameBudget(32, 0, 0);
        world.Pump(
            deltaTime: 0.2f,
            fixedDeltaTime: 0f,
            pumpFixedUpdate: false,
            budget: ref budget);

        Assert.That(ActorLifecycleTrace.Entries, Is.EqualTo(new[] { "update:0.2", "destroy" }));
    }
}
