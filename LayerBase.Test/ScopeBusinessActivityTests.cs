using LayerBase.Lifetime;
using LayerBase.Async;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Scope;

namespace EventsTest;

public sealed class ScopeBusinessActivityTests
{
    [Test]
    public void Lifetime_operation_lease_completes_only_once()
    {
        var tracker = new LifetimeOperationTracker();

        Assert.That(tracker.TryBegin(out var first), Is.True);
        Assert.That(tracker.TryBegin(out var second), Is.True);

        Assert.That(first.TryComplete(), Is.True);
        Assert.That(first.TryComplete(), Is.False);
        Assert.That(tracker.ActiveCount, Is.EqualTo(1));

        Assert.That(second.TryComplete(), Is.True);
        Assert.That(tracker.ActiveCount, Is.EqualTo(0));
    }

    [Test]
    public void Async_event_handler_completion_is_returned_to_owner_thread()
    {
        var tracker = new LifetimeOperationTracker();
        using var transport = new ScopeTransport(new ScopeAddress(1, 1, 0));
        var eventCenter = new EventCenter();
        using var source = new LBTaskCompletionSource();

        eventCenter.BindBusinessOperations(tracker, transport);
        eventCenter.SubscribeAsync<TestBusinessEvent>(0, _ => source.Task);

        eventCenter.Send(new TestBusinessEvent());

        Assert.That(tracker.ActiveCount, Is.EqualTo(1));

        source.SetResult();

        SpinWait.SpinUntil(
            () => transport.CompletionInbox.Count == 1,
            TimeSpan.FromSeconds(2));

        Assert.That(tracker.ActiveCount, Is.EqualTo(1));
        Assert.That(transport.CompletionInbox.TryDequeue(out var completion), Is.True);
        Assert.That(completion.Kind, Is.EqualTo(ScopeCompletionKind.LifetimeOperationCompleted));
        Assert.That(completion.OperationLease.TryComplete(), Is.True);
        Assert.That(completion.OperationLease.TryComplete(), Is.False);
        Assert.That(tracker.ActiveCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Dispose_control_waits_until_async_business_operations_drain()
    {
        using var runtime = new LayerRuntime(9701);
        using var host = ScopeRuntimeHost.CreateMain(
            runtime,
            runtimeId: 9701,
            generation: 1);

        ScopeRuntime scope = host.MainScope;
        Assert.That(scope.AsyncCallOperations.TryBegin(out var lease), Is.True);

        var disposeTask = scope.RequestDisposeAsync();

        scope.PumpIngress();

        Assert.That(disposeTask.GetAwaiter().IsCompleted, Is.False);
        Assert.That(scope.State, Is.Not.EqualTo(ScopeRuntimeState.Disposed));

        scope.Transport.EnqueueCompletion(
            ScopeCompletionEnvelope.LifetimeOperationCompleted(lease));
        scope.PumpIngress();

        ScopeDisposeResponse response = await disposeTask;
        Assert.That(response.State, Is.EqualTo(ScopeControlResult.Succeeded));
        Assert.That(scope.State, Is.EqualTo(ScopeRuntimeState.Disposed));
    }

    private readonly struct TestBusinessEvent
    {
    }
}
