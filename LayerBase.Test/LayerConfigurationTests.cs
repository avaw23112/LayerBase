using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class LayerConfigurationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Layer_can_intercept_events_using_Handled_state()
    {
        var l1 = new InterceptLayer();
        var l2 = new NormalLayer();
        var rt = LayerHub.CreateLayers().Push(l1).Push(l2).Build();

        var eventId = Guid.NewGuid();
        rt.Send(new PlainEvent(eventId));

        Assert.That(l1.ReceivedIds, Contains.Item(eventId));
        Assert.That(l2.ReceivedIds, Is.Empty, "L2 should not receive events handled by L1");
    }

    [Test]
    public void Layer_mapping_is_correctly_initialized()
    {
        var l1 = new NormalLayer();
        var rt = LayerHub.CreateLayers().Push(l1).Build();
        Assert.That(l1.RouteIndex, Is.EqualTo(0));
    }

    private class NormalLayer : Layer
    {
        public List<Guid> ReceivedIds = new();

        public NormalLayer()
        {
            Subscribe<PlainEvent>(Handle);
        }

        private EventHandledState Handle(in PlainEvent e)
        {
            ReceivedIds.Add(e.Id);
            return EventHandledState.Continue;
        }
    }

    private class InterceptLayer : Layer
    {
        public List<Guid> ReceivedIds = new();

        public InterceptLayer()
        {
            Subscribe<PlainEvent>(Handle);
        }

        private EventHandledState Handle(in PlainEvent e)
        {
            ReceivedIds.Add(e.Id);
            return EventHandledState.Handled;
        }
    }

    public struct PlainEvent
    {
        public Guid Id;
        public PlainEvent(Guid id) => Id = id;
    }
}