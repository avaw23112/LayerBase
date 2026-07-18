using LayerBase;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Layers;

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
        var runtime = LayerHub.CreateLayers().Push(l1).Push(l2).Build();

        runtime.Send(new TestEvent { Value = 1 });
        Assert.That(_trace, Is.EqualTo(new[] { "L1_Recv", "L2_Recv" }));
    }

    [Test]
    public void Ordered_handlers_keep_registration_order_when_sync_and_async_are_mixed()
    {
        var layer = new TraceLayer("L1", _trace);

        // Use Test method context to create fresh layer
        var runtime = LayerHub.CreateLayers().Push(layer).Build();

        layer.SubscribeFlow((in TestEvent e) =>
        {
            _trace.Add("First");
            return EventHandledState.Continue;
        });
        layer.SubscribeAsync<TestEvent>(async e =>
        {
            _trace.Add("Second");
            await LBTask.Yield();
        });
        layer.SubscribeFlow((in TestEvent e) =>
        {
            _trace.Add("Third");
            return EventHandledState.Continue;
        });

        runtime.Send(new TestEvent());

        // Wait for trace to fill
        for (var i = 0; i < 50 && _trace.Count < 4; i++) Thread.Sleep(5);
        // New Timing: Sync handlers are batched first, then Async handlers are started.
        // Third is sync, Second is async. So Third executes before Second starts.
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
            layer.SubscribeFlow((in TestEvent e) => throw new Exception("Boom"));
            layer.SubscribeFlow((in TestEvent e) =>
            {
                _trace.Add("Safe");
                return EventHandledState.Continue;
            });
            var runtime = LayerHub.CreateLayers().Push(layer).Build();

            runtime.Send(new TestEvent());
            Assert.That(errorCount, Is.EqualTo(1));
            Assert.That(_trace, Is.EquivalentTo(new[] { "L1_Recv", "Safe" }));

            _trace.Clear();
            runtime.Send(new TestEvent());
            Assert.That(errorCount, Is.EqualTo(1), "Should fuse and not report again");
            Assert.That(_trace, Is.EquivalentTo(new[] { "L1_Recv", "Safe" }));
        }
        finally
        {
            LayerHub.OnLayerEventInfo -= handler;
        }
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
            SubscribeFlow<TestEvent>(OnRecv);
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
