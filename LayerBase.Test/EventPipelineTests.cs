using LayerBase;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class EventPipelineTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        _trace = new List<string>();
    }

    private List<string> _trace;

    [Test]
    public void Broadcast_hits_all_layers_when_not_handled()
    {
        var l1 = new TraceLayer("L1", _trace);
        var l2 = new TraceLayer("L2", _trace);
        var rt = LayerHub.CreateLayers().Push(l1).Push(l2).Build();

        rt.Send(new TestEvent { Value = 1 });
        Assert.That(_trace, Is.EqualTo(new[] { "L1_Recv", "L2_Recv" }));
    }

    [Test]
    public void Bubble_stops_at_lower_priority_layer_when_handled_by_higher_priority()
    {
        var l1 = new TraceLayer("L1", _trace, true);
        var l2 = new TraceLayer("L2", _trace);
        LayerHub.CreateLayers().Push(l1).Push(l2).Build();

        l2.SendBubble(new TestEvent { Value = 1 });
        Assert.That(_trace, Is.EqualTo(new[] { "L2_Recv", "L1_Recv" }));
    }

    [Test]
    public void Ordered_handlers_keep_registration_order_when_sync_and_async_are_mixed()
    {
        var layer = new TraceLayer("L1", _trace);

        var rt = LayerHub.CreateLayers().Push(layer).Build();

        layer.Subscribe((in TestEvent e) =>
        {
            _trace.Add("First");
            return EventHandledState.Continue;
        });
        layer.SubscribeAsync<TestEvent>(async e =>
        {
            _trace.Add("Second");
            await LBTask.Yield();
        });
        layer.Subscribe((in TestEvent e) =>
        {
            _trace.Add("Third");
            return EventHandledState.Continue;
        });

        rt.Send(new TestEvent());

        for (var i = 0; i < 50 && _trace.Count < 4; i++) Thread.Sleep(5);
        Assert.That(_trace, Is.EqualTo(new[] { "L1_Recv", "First", "Third", "Second" }));
    }

    [Test]
    public void Faulted_handler_is_disabled_and_reported_without_blocking_other_handlers()
    {
        var layer = new TraceLayer("L1", _trace);
        var errorCount = 0;
        Action<LayerEventInfo> handler = info =>
        {
            if (info.Type == LayerEventInfoType.Error) Interlocked.Increment(ref errorCount);
        };

        LayerHub.OnLayerEventInfo += handler;
        try
        {
            layer.Subscribe((in TestEvent e) => throw new Exception("Boom"));
            layer.Subscribe((in TestEvent e) =>
            {
                _trace.Add("Safe");
                return EventHandledState.Continue;
            });
            var rt = LayerHub.CreateLayers().Push(layer).Build();

            rt.Send(new TestEvent());
            Assert.That(errorCount, Is.EqualTo(1));

            Assert.That(_trace, Is.EquivalentTo(new[] { "L1_Recv" }));

            _trace.Clear();
            rt.Send(new TestEvent());
            Assert.That(errorCount, Is.EqualTo(1), "Should fuse and not report again");
            Assert.That(_trace, Is.EquivalentTo(new[] { "L1_Recv", "Safe" }));
        }
        finally
        {
            LayerHub.OnLayerEventInfo -= handler;
        }
    }

    [Test]
    public void Faulted_parallel_handler_is_disabled_and_reported_once()
    {
        LayerHub.InitializeJobScheduler(1);
        var layer = new TraceLayer("L1", _trace);
        var errorOccurred = new ManualResetEventSlim(false);
        Action<LayerEventInfo> handler = info =>
        {
            if (info.Type == LayerEventInfoType.Error) errorOccurred.Set();
        };

        LayerHub.OnLayerEventInfo += handler;
        try
        {
            layer.SubscribeParallel((in TestEvent e) => throw new Exception("ParallelBoom"));
            var rt = LayerHub.CreateLayers().Push(layer).Build();

            rt.Send(new TestEvent());
            Assert.That(errorOccurred.Wait(1000), Is.True, "Error signal timeout");

            errorOccurred.Reset();
            rt.Send(new TestEvent());
            Assert.That(errorOccurred.Wait(100), Is.False, "Should have fused");
        }
        finally
        {
            LayerHub.OnLayerEventInfo -= handler;
        }
    }

    [Test]
    public void Reset_detaches_runtime_from_hub_pump_but_keeps_caller_owned_runtime_usable()
    {
        var layer = new TraceLayer("L1", _trace);
        var runtime = LayerHub.CreateLayers().Push(layer).Build();

        layer.PostLocal(new TestEvent { Value = 7 });

        LayerHub.Reset();
        LayerHub.Pump(0.016f);
        Assert.That(_trace, Is.Empty);

        runtime.Pump(0.016f);
        Assert.That(_trace, Is.EqualTo(new[] { "L1_Recv" }));
    }

    private class TraceLayer : Layer
    {
        private readonly bool _handle;
        private readonly string _name;
        private readonly List<string> _trace;

        public TraceLayer(string name, List<string> trace, bool handle = false)
        {
            _name = name;
            _trace = trace;
            _handle = handle;
            Subscribe<TestEvent>(OnRecv);
        }

        private EventHandledState OnRecv(in TestEvent e)
        {
            _trace.Add(_name + "_Recv");
            return _handle ? EventHandledState.Handled : EventHandledState.Continue;
        }
    }

    public struct TestEvent
    {
        public int Value;
    }
}
