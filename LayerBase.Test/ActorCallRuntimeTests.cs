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

public struct ActorCallRuntimeBlockingRequest
{
    public int Value;
}

public struct ActorCallRuntimeBlockingResponse
{
    public int Value;
}

public struct ActorCallRuntimeTokenRequest
{
    public int Value;
}

public struct ActorCallRuntimeTokenResponse
{
    public int Value;
}

internal static class ActorCallRuntimeTrace
{
    public static List<string> Entries { get; } = new();
    public static LBTaskCompletionSource<ActorCallRuntimeBlockingResponse>? BlockingSource { get; set; }
    public static LBTaskCompletionSource<ActorCallRuntimeTokenResponse>? TokenSource { get; set; }
    public static CancellationToken CapturedToken { get; set; }

    public static void Reset()
    {
        Entries.Clear();
        BlockingSource?.Dispose();
        BlockingSource = null;
        TokenSource?.Dispose();
        TokenSource = null;
        CapturedToken = default;
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

    [ActorCallBehaviour]
    private LBTask<ActorCallRuntimeBlockingResponse> OnBlockingAsk(
        in ActorCallRuntimeBlockingRequest request,
        CancellationToken                 cancellationToken)
    {
        ActorCallRuntimeTrace.Entries.Add($"blocking:{request.Value}");
        return ActorCallRuntimeTrace.BlockingSource!.Task;
    }

    [ActorCallBehaviour]
    private LBTask<ActorCallRuntimeTokenResponse> OnTokenAsk(
        in ActorCallRuntimeTokenRequest request,
        CancellationToken              cancellationToken)
    {
        ActorCallRuntimeTrace.Entries.Add($"token:{request.Value}");
        ActorCallRuntimeTrace.CapturedToken = cancellationToken;
        return ActorCallRuntimeTrace.TokenSource!.Task;
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
    public void Actor_destroy_cancels_active_call()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();
        ActorId actorId = actor.GetActorId();
        ActorCallRuntimeTrace.BlockingSource =
            new LBTaskCompletionSource<ActorCallRuntimeBlockingResponse>();

        LBTask<ActorCallRuntimeBlockingResponse> task =
            world.Ask<ActorCallRuntimeBlockingRequest, ActorCallRuntimeBlockingResponse>(
                actorId,
                new ActorCallRuntimeBlockingRequest { Value = 7 });

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(task.GetAwaiter().IsCompleted, Is.False);
        ActorDebugInfo runningInfo = world.GetDebugInfo(actorId);
        Assert.That(runningInfo.ActiveOperations, Is.EqualTo(1));

        Assert.That(world.DestroyActor(actorId), Is.True);
        budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(task.GetAwaiter().IsCompleted, Is.True);
        ActorDebugInfo cancelledInfo = world.GetDebugInfo(actorId);
        Assert.That(cancelledInfo.ActiveOperations, Is.EqualTo(0));

        ActorCallRuntimeTrace.BlockingSource.SetResult(
            new ActorCallRuntimeBlockingResponse { Value = 14 });

        Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());

        budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        ActorDebugInfo staleInfo = world.GetDebugInfo(actorId);
        Assert.That(staleInfo.IsValid, Is.False);
    }

    [Test]
    public void Runtime_dispose_completes_active_actor_call_with_object_disposed()
    {
        var world = new ActorWorld();
        int runtimeIndex = world.RuntimeIndex;
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();
        ActorCallRuntimeTrace.BlockingSource =
            new LBTaskCompletionSource<ActorCallRuntimeBlockingResponse>();

        LBTask<ActorCallRuntimeBlockingResponse> task =
            world.Ask<ActorCallRuntimeBlockingRequest, ActorCallRuntimeBlockingResponse>(
                actor.GetActorId(),
                new ActorCallRuntimeBlockingRequest { Value = 9 });

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        world.Dispose();

        Assert.That(task.GetAwaiter().IsCompleted, Is.True);

        ActorCallRuntimeTrace.BlockingSource.SetResult(
            new ActorCallRuntimeBlockingResponse { Value = 18 });

        var pumpBudget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref pumpBudget);
        Assert.Throws<ObjectDisposedException>(() => task.GetAwaiter().GetResult());

        using var recycledWorld = new ActorWorld();
        Assert.That(recycledWorld.RuntimeIndex, Is.EqualTo(runtimeIndex));
    }

    [Test]
    public void Actor_lifetime_token_reaches_call_handler()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();
        ActorCallRuntimeTrace.TokenSource =
            new LBTaskCompletionSource<ActorCallRuntimeTokenResponse>();

        LBTask<ActorCallRuntimeTokenResponse> task =
            world.Ask<ActorCallRuntimeTokenRequest, ActorCallRuntimeTokenResponse>(
                actor.GetActorId(),
                new ActorCallRuntimeTokenRequest { Value = 11 });

        var budget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(ActorCallRuntimeTrace.CapturedToken.CanBeCanceled, Is.True);
        Assert.That(ActorCallRuntimeTrace.CapturedToken.IsCancellationRequested, Is.False);

        world.Dispose();

        Assert.That(
            SpinWait.SpinUntil(
                () => ActorCallRuntimeTrace.CapturedToken.IsCancellationRequested,
                TimeSpan.FromMilliseconds(1000)),
            Is.True);

        ActorCallRuntimeTrace.TokenSource.SetResult(
            new ActorCallRuntimeTokenResponse { Value = 22 });

        var pumpBudget = new RuntimeFrameBudget(16, 0, 0);
        world.Pump(0f, 0f, false, ref pumpBudget);
        Assert.Throws<ObjectDisposedException>(() => task.GetAwaiter().GetResult());
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
    public void Dispose_completes_queued_ask_with_object_disposed_before_handler_starts()
    {
        var world = new ActorWorld();
        ActorCallRuntimeActor actor = world.CreateActor<ActorCallRuntimeActor>();

        LBTask<ActorCallRuntimeResponse> task = world.Ask<ActorCallRuntimeRequest, ActorCallRuntimeResponse>(
            actor.GetActorId(),
            new ActorCallRuntimeRequest(10));

        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => task.GetAwaiter().GetResult());
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
