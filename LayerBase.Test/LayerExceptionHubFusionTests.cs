using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class LayerExceptionHubFusionTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Runtime_legacy_error_report_should_flow_through_exception_hub_and_old_events()
    {
        LayerRuntime runtime = LayerHub.CreateLayers()
                                       .Push(new ExceptionFusionLayer())
                                       .Build();
        var exception = new InvalidOperationException("legacy route failed");
        var records = new List<LayerExceptionRecord>();
        var runtimeEvents = new List<LayerEventInfo>();
        var hubEvents = new List<LayerEventInfo>();

        runtime.ExceptionCallbacks.OnExceptionRecord += records.Add;
        runtime.OnLayerEventInfo += runtimeEvents.Add;
        LayerHub.OnLayerEventInfo += hubEvents.Add;

        runtime.ReportLayerEventError(3, "LegacySource", "LegacyEvent", exception);

        Assert.That(records, Has.Count.EqualTo(1));
        Assert.That(records[0].Exception, Is.SameAs(exception));
        Assert.That(runtimeEvents, Has.Count.EqualTo(1));
        Assert.That(hubEvents, Has.Count.EqualTo(1));
        Assert.That(runtimeEvents[0].Exception, Is.SameAs(exception));
        Assert.That(hubEvents[0].Exception, Is.SameAs(exception));
    }

    [Test]
    public void Scope_owner_thread_exception_should_be_immediately_visible_on_old_events()
    {
        LayerRuntime runtime = LayerHub.CreateLayers()
                                       .Push(new ExceptionFusionLayer())
                                       .Build();
        var exception = new InvalidOperationException("scope post failed");
        var records = new List<LayerExceptionRecord>();
        var runtimeEvents = new List<LayerEventInfo>();
        var hubEvents = new List<LayerEventInfo>();

        runtime.ExceptionCallbacks.OnExceptionRecord += records.Add;
        runtime.OnLayerEventInfo += runtimeEvents.Add;
        LayerHub.OnLayerEventInfo += hubEvents.Add;

        using var scope = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 31,
                name: "FusionScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>(),
            owningRuntime: runtime,
            postDispatcher: (_, _) => throw exception);

        Assert.That(scope.TryPost(new ScopePostMessage(9, "payload")), Is.True);
        scope.Pump(0.016f);

        Assert.That(records, Has.Count.EqualTo(1));
        Assert.That(records[0].Exception, Is.SameAs(exception));
        Assert.That(records[0].ScopeId, Is.EqualTo(31));
        Assert.That(records[0].Phase, Is.EqualTo(LayerExceptionPhase.PostDispatch));
        Assert.That(runtimeEvents, Has.Count.EqualTo(1));
        Assert.That(hubEvents, Has.Count.EqualTo(1));
        Assert.That(runtimeEvents[0].Exception, Is.SameAs(exception));
        Assert.That(hubEvents[0].Exception, Is.SameAs(exception));
    }

    [Test]
    public void Static_legacy_error_report_inside_runtime_pump_should_use_current_runtime_not_primary()
    {
        LayerRuntime primary = LayerHub.CreateLayers()
                                       .Push(new ExceptionFusionLayer())
                                       .Build();
        var secondaryLayer = new StaticErrorLayer();
        LayerRuntime secondary = LayerHub.CreateLayers()
                                         .Push(secondaryLayer)
                                         .Build();
        var primaryEvents = new List<LayerEventInfo>();
        var secondaryEvents = new List<LayerEventInfo>();
        var hubEvents = new List<LayerEventInfo>();

        primary.OnLayerEventInfo += primaryEvents.Add;
        secondary.OnLayerEventInfo += secondaryEvents.Add;
        LayerHub.OnLayerEventInfo += hubEvents.Add;

        secondary.Pump(0.016f);

        Assert.That(primaryEvents, Is.Empty);
        Assert.That(secondaryEvents, Has.Count.EqualTo(1));
        Assert.That(secondaryEvents[0].Exception, Is.SameAs(secondaryLayer.Exception));
        Assert.That(hubEvents, Has.Count.EqualTo(1));
        Assert.That(hubEvents[0].Exception, Is.SameAs(secondaryLayer.Exception));
    }

    [Test]
    public void Runtime_exception_sink_should_isolate_detailed_callback_failure()
    {
        LayerRuntime runtime = LayerHub.CreateLayers()
                                       .Push(new ExceptionFusionLayer())
                                       .Build();
        var callbackException = new InvalidOperationException("callback failed");
        var reportedException = new InvalidOperationException("reported failed");
        var runtimeEvents = new List<LayerEventInfo>();
        var hubEvents = new List<LayerEventInfo>();

        runtime.ExceptionCallbacks.OnExceptionRecord += _ => throw callbackException;
        runtime.OnLayerEventInfo += runtimeEvents.Add;
        LayerHub.OnLayerEventInfo += hubEvents.Add;

        runtime.ReportLayerEventError(7, "LegacySource", "LegacyEvent", reportedException);

        Assert.That(runtimeEvents, Has.Count.EqualTo(1));
        Assert.That(runtimeEvents[0].Exception, Is.SameAs(reportedException));
        Assert.That(hubEvents.Any(info =>
            info.Source == "LayerExceptionHub" &&
            info.EventName == "CallbackFailure" &&
            ReferenceEquals(info.Exception, callbackException)), Is.True);
        Assert.That(hubEvents.Any(info => ReferenceEquals(info.Exception, reportedException)), Is.True);
    }

    [Test]
    public void LayerExceptionHub_should_dispatch_overflow_after_sink_exception()
    {
        var hub = new LayerExceptionHub(capacity: 1);
        var sink = new ThrowingExceptionSink();

        hub.Report(CreateRecord(new InvalidOperationException("first")));
        hub.Report(CreateRecord(new InvalidOperationException("overflow")));

        hub.DrainAndDispatch(sink);

        Assert.That(sink.ExceptionAttempts, Is.EqualTo(1));
        Assert.That(sink.OverflowAttempts, Is.EqualTo(1));
        Assert.That(sink.OverflowCount, Is.EqualTo(1));
    }

    private sealed class ExceptionFusionLayer : Layer
    {
    }

    private static LayerExceptionRecord CreateRecord(Exception exception)
    {
        return new LayerExceptionRecord(
            exception: exception,
            scopeId: 1,
            serviceId: -1,
            phase: LayerExceptionPhase.EventDispatch,
            queueKind: LayerQueueKind.None,
            messageId: -1,
            trace: ScopeTrace.Empty,
            threadId: Environment.CurrentManagedThreadId,
            tick: 0,
            queueCapacity: 1,
            queueCount: 1);
    }

    private sealed class ThrowingExceptionSink : ILayerExceptionSink
    {
        public int ExceptionAttempts { get; private set; }

        public int OverflowAttempts { get; private set; }

        public int OverflowCount { get; private set; }

        public void OnException(in LayerExceptionRecord record)
        {
            ExceptionAttempts++;
            throw new InvalidOperationException("sink failed");
        }

        public void OnExceptionQueueOverflow(int droppedCount, in LayerExceptionRecord lastRecord)
        {
            OverflowAttempts++;
            OverflowCount = droppedCount;
        }
    }

    private sealed class StaticErrorLayer : Layer
    {
        private bool _reported;

        public Exception Exception { get; } = new InvalidOperationException("secondary runtime failed");

        public override bool HasActiveLogic => true;

        public override void Pump(float deltaTime)
        {
            if (_reported)
            {
                return;
            }

            _reported = true;
            LayerHub.ReportLayerEventError(RouteIndex, "StaticErrorLayer", "Pump", Exception);
        }
    }
}
