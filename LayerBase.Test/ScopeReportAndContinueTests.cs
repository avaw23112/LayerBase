using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Scope;

namespace EventsTest.ProductionHardening;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeReportAndContinueTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Report_and_continue_scope_keeps_running_after_fault()
    {
        using var runtime = LayerHub.CreateLayers()
            .Push(new FaultyServiceLayer())
            .Build();

        runtime.Pump(0.016f);

        Assert.That(runtime.ScopeHost.MainScope.State, Is.Not.EqualTo(ScopeRuntimeState.Faulted));
        Assert.That(runtime.State, Is.EqualTo(RuntimeState.Running));
    }

    [Test]
    public void Report_and_continue_scope_keeps_business_admission_open()
    {
        using var runtime = LayerHub.CreateLayers()
            .Push(new FaultyServiceLayer())
            .Build();

        runtime.Pump(0.016f);

        var result = runtime.Main.Post(default(ProbeEvent));
        Assert.That(result.IsAccepted, Is.True);
    }

    [Test]
    public void Stop_scope_policy_stops_only_source_scope()
    {
        var runtime = new LayerRuntime(9301);
        var stopScopeOptions = new ScopeOptions(
            ScopeThreadingMode.Inline,
            ScopeClockMode.RuntimePump,
            tickRateHz: 0,
            ScopeFaultPolicy.StopScope);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateThrowingScopePlan(scopeId: 1, stopScopeOptions)
            },
            runtimeId: 9301,
            generation: 1);

        var sourceScope = host.Scopes[1];
        sourceScope.PumpUpdate(0.016f);

        Assert.That(sourceScope.State, Is.Not.EqualTo(ScopeRuntimeState.Faulted));
        Assert.That(sourceScope.Transport.CallInbox.TryDequeue(out var envelope), Is.True);
        Assert.That(envelope.Class, Is.EqualTo(ScopeCallClass.Control));
        Assert.That(envelope.RouteId, Is.EqualTo(ScopeLifecycleRouteIds.Stop));
    }

    [Test]
    public void Safe_subscriber_still_runs_after_throwing_subscriber()
    {
        var center = new EventCenter();
        var hitCount = 0;

        center.Subscribe<TestEvent>(0, (in TestEvent _) =>
            throw new InvalidOperationException("first subscriber fails"));
        center.Subscribe<TestEvent>(0, (in TestEvent _) => hitCount++);

        Assert.DoesNotThrow(() => center.Send(new TestEvent()));

        Assert.That(hitCount, Is.EqualTo(1));
    }

    [Test]
    public void Flow_handler_still_runs_after_throwing_handler()
    {
        var center = new EventCenter();
        var hitCount = 0;

        EventHandleDelegate<TestEvent> throwing = (in TestEvent _) =>
        {
            throw new InvalidOperationException("first handler fails");
        };
        EventHandleDelegate<TestEvent> counting = (in TestEvent _) =>
        {
            hitCount++;
            return EventHandledState.Continue;
        };

        center.SubscribeFlow(0, throwing);
        center.SubscribeFlow(0, counting);

        Assert.DoesNotThrow(() => center.Send(new TestEvent()));

        Assert.That(hitCount, Is.EqualTo(1));
    }

    private sealed class FaultyServiceLayer : Layer
    {
        public override bool HasActiveLogic => true;

        public override void Pump(float deltaTime)
        {
            throw new InvalidOperationException("service update failed");
        }
    }

    private static ScopeExecutionPlan CreateThrowingScopePlan(int scopeId, ScopeOptions options)
    {
        var update = new UpdateInvoker[]
        {
            _ => throw new InvalidOperationException("update failed")
        };
        var layers = new[]
        {
            new ScopeLayerLifecycleSlice(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0)
        };

        return new ScopeExecutionPlan(
            new ScopeDescriptor(scopeId, "FaultScope", typeof(FaultScope)),
            options,
            lifecyclePlan: new ScopeLifecyclePlan(
                layers,
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                update,
                Array.Empty<FixedUpdateInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>()));
    }

    private sealed class FaultScope : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Inline;
    }

    private readonly struct TestEvent;

    private readonly struct ProbeEvent;
}
