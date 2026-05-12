using System.Threading;
using LayerBase.Actor;
using LayerBase.Async;

namespace LayerBase.Test;

public struct ActorCallRuntimeEvent
{
    public int Value;

    public ActorCallRuntimeEvent(int value)
    {
        Value = value;
    }
}

public struct ActorCallRuntimeRequest
{
    public int Value;

    public ActorCallRuntimeRequest(int value)
    {
        Value = value;
    }
}

public struct ActorCallRuntimeResponse
{
    public int Value;
}

public struct ActorCallRuntimeUnsupportedRequest
{
    public int Value;
}

public struct ActorCallRuntimeUnsupportedResponse
{
    public int Value;
}

internal static class ActorCallRuntimeTrace
{
    public static List<string> Entries { get; } = new();

    public static void Reset()
    {
        Entries.Clear();
    }
}

internal sealed partial class ActorCallRuntimeActor : IActor
{
    [ActorBehaviour]
    private void OnEvent(in ActorCallRuntimeEvent value)
    {
        ActorCallRuntimeTrace.Entries.Add($"event:{value.Value}");
    }

    [ActorCallBehaviour]
    private LBTask<ActorCallRuntimeResponse> OnAsk(
        in ActorCallRuntimeRequest request,
        CancellationToken          cancellationToken)
    {
        ActorCallRuntimeTrace.Entries.Add($"ask:{request.Value}");
        return LBTask<ActorCallRuntimeResponse>.FromResult(new ActorCallRuntimeResponse
        {
            Value = request.Value * 2
        });
    }
}

[TestFixture]
public class ActorCallRuntimeTests
{
    [SetUp]
    public void SetUp()
    {
        ActorCallRuntimeTrace.Reset();
    }

    [Test]
    public void ImmediatelyAsk_executes_without_pump()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();

        LBTask<ActorCallRuntimeResponse> task = world.ImmediatelyAsk<ActorCallRuntimeRequest, ActorCallRuntimeResponse>(
            actor.GetActorId(),
            new ActorCallRuntimeRequest(3));

        var awaiter = task.GetAwaiter();
        Assert.That(awaiter.IsCompleted, Is.True);
        Assert.That(awaiter.GetResult().Value, Is.EqualTo(6));
        Assert.That(ActorCallRuntimeTrace.Entries, Is.EqualTo(new[] { "ask:3" }));
    }

    [Test]
    public void Ask_waits_for_pump_then_completes()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();

        LBTask<ActorCallRuntimeResponse> task = world.Ask<ActorCallRuntimeRequest, ActorCallRuntimeResponse>(
            actor.GetActorId(),
            new ActorCallRuntimeRequest(4));

        Assert.That(task.GetAwaiter().IsCompleted, Is.False);
        Assert.That(ActorCallRuntimeTrace.Entries, Is.Empty);

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        var awaiter = task.GetAwaiter();
        Assert.That(awaiter.IsCompleted, Is.True);
        Assert.That(awaiter.GetResult().Value, Is.EqualTo(8));
        Assert.That(ActorCallRuntimeTrace.Entries, Is.EqualTo(new[] { "ask:4" }));
    }

    [Test]
    public void Ask_canceled_before_pump_does_not_invoke_handler()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();
        using var cancellation = new CancellationTokenSource();

        LBTask<ActorCallRuntimeResponse> task = world.Ask<ActorCallRuntimeRequest, ActorCallRuntimeResponse>(
            actor.GetActorId(),
            new ActorCallRuntimeRequest(5),
            cancellation.Token);

        cancellation.Cancel();

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
        Assert.That(ActorCallRuntimeTrace.Entries, Is.Empty);
    }

    [Test]
    public void ImmediatelyAsk_returns_unsupported_when_route_missing()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();

        LBTask<ActorCallRuntimeUnsupportedResponse> task =
            world.ImmediatelyAsk<ActorCallRuntimeUnsupportedRequest, ActorCallRuntimeUnsupportedResponse>(
                actor.GetActorId(),
                new ActorCallRuntimeUnsupportedRequest { Value = 1 });

        ActorCallException exception = Assert.Throws<ActorCallException>(() => task.GetAwaiter().GetResult())!;
        Assert.That(exception.FailureKind, Is.EqualTo(ActorCallFailureKind.UnsupportedRequest));
    }

    [Test]
    public void DispatchNow_invokes_actor_behaviour_immediately()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();

        DispatchResult result = world.DispatchNow(actor.GetActorId(), new ActorCallRuntimeEvent(9));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(ActorCallRuntimeTrace.Entries, Is.EqualTo(new[] { "event:9" }));
    }

    [Test]
    public void DelayAsk_completes_only_after_due_pump()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();

        LBTask<ActorCallRuntimeResponse> task = world.DelayAsk<ActorCallRuntimeRequest, ActorCallRuntimeResponse>(
            actor.GetActorId(),
            new ActorCallRuntimeRequest(6),
            1.0f);

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0.5f, 0f, false, ref budget);

        Assert.That(task.GetAwaiter().IsCompleted, Is.False);
        Assert.That(ActorCallRuntimeTrace.Entries, Is.Empty);

        budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0.5f, 0f, false, ref budget);

        Assert.That(task.GetAwaiter().GetResult().Value, Is.EqualTo(12));
        Assert.That(ActorCallRuntimeTrace.Entries, Is.EqualTo(new[] { "ask:6" }));
    }

    [Test]
    public void DelayPost_dispatches_after_due_pump()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();

        world.DelayPost(actor.GetActorId(), new ActorCallRuntimeEvent(11), 1.0f);

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0.5f, 0f, false, ref budget);
        Assert.That(ActorCallRuntimeTrace.Entries, Is.Empty);

        budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0.5f, 0f, false, ref budget);
        Assert.That(ActorCallRuntimeTrace.Entries, Is.EqualTo(new[] { "event:11" }));
    }

    [Test]
    public void Dispose_cancels_pending_delay_ask()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();

        LBTask<ActorCallRuntimeResponse> task = world.DelayAsk<ActorCallRuntimeRequest, ActorCallRuntimeResponse>(
            actor.GetActorId(),
            new ActorCallRuntimeRequest(8),
            5.0f);

        world.Dispose();

        Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
        Assert.That(ActorCallRuntimeTrace.Entries, Is.Empty);
    }

    [Test]
    public void Pending_destroy_blocks_dispatch_and_immediate_ask()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();
        ActorId actorId = actor.GetActorId();

        Assert.That(world.DestroyActor(actorId), Is.True);

        DispatchResult dispatchResult = world.DispatchNow(actorId, new ActorCallRuntimeEvent(1));
        Assert.That(dispatchResult.IsSuccess, Is.False);
        Assert.That(dispatchResult.FailureKind, Is.EqualTo(DispatchFailureKind.PendingDestroy));

        LBTask<ActorCallRuntimeResponse> task = world.ImmediatelyAsk<ActorCallRuntimeRequest, ActorCallRuntimeResponse>(
            actorId,
            new ActorCallRuntimeRequest(2));

        ActorCallException exception = Assert.Throws<ActorCallException>(() => task.GetAwaiter().GetResult())!;
        Assert.That(exception.FailureKind, Is.EqualTo(ActorCallFailureKind.PendingDestroy));
    }
}