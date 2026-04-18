using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class LocalEventTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        _trace = new List<string>();
    }

    private List<string> _trace;

    [Test]
    public void SendLocal_only_hits_current_layer()
    {
        var layer0 = new LocalRecordingLayer("L0", _trace);
        var layer1 = new LocalRecordingLayer("L1", _trace);

        LayerHub.CreateLayers().Push(layer0).Push(layer1).Build();

        // Send local from L1
        layer1.SendLocal(new LocalEvent());

        Assert.That(_trace, Is.EqualTo(new[] { "L1" }));
    }

    [Test]
    public void PostLocal_only_hits_current_layer_asynchronously()
    {
        var layer0 = new LocalRecordingLayer("L0", _trace);
        var layer1 = new LocalRecordingLayer("L1", _trace);

        LayerHub.CreateLayers().Push(layer0).Push(layer1).Build();

        // Post local from L0
        layer0.PostLocal(new LocalEvent());

        LayerHub.Pump(0.02f); // L0 Pump
        LayerHub.Pump(0.02f); // L1 Pump

        Assert.That(_trace, Is.EqualTo(new[] { "L0" }));
    }

    private class LocalRecordingLayer : Layer
    {
        private readonly string _name;
        private readonly List<string> _trace;

        public LocalRecordingLayer(string name, List<string> trace)
        {
            _name = name;
            _trace = trace;
            Subscribe((in LocalEvent evt) =>
            {
                _trace.Add(_name);
                return EventHandledState.Continue;
            });
        }
    }

    public struct LocalEvent
    {
    }
}