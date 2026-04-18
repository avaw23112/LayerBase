using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class HierarchicalOrderInvestigationTests
{
    private List<int> _executionOrder;

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        _executionOrder = new List<int>();
    }

    [Test]
    public void Investigate_Bubble_Queued_Order()
    {
        var layer0 = new OrderTrackingLayer(0, _executionOrder);
        var layer1 = new OrderTrackingLayer(1, _executionOrder);
        var layer2 = new OrderTrackingLayer(2, _executionOrder);

        // Chain: 0 (Outer) -> 1 -> 2 (Inner)
        LayerHub.CreateLayers().Push(layer0).Push(layer1).Push(layer2).Build();

        // Layer 2 posts a Bubble event. Expected hierarchical order: 2 -> 1 -> 0.
        layer2.PostBubble(new OrderEvent());

        // Pump to process
        LayerHub.Pump(0.02f); // Cycle 1: Layer 2 processes its queue, posts to 1 and 0
        LayerHub.Pump(0.02f); // Cycle 2: Layers 0 and 1 process their queues

        Console.WriteLine("Bubble Execution Order: " + string.Join(" -> ", _executionOrder));
        
        // Expected: 2, 1, 0. Actual: 2, 0, 1
        Assert.That(_executionOrder, Is.EqualTo(new[] { 2, 1, 0 }), "Bubble order should be Inner -> Outer");
    }

    [Test]
    public void Investigate_Drop_Queued_Order()
    {
        var layer0 = new OrderTrackingLayer(0, _executionOrder);
        var layer1 = new OrderTrackingLayer(1, _executionOrder);
        var layer2 = new OrderTrackingLayer(2, _executionOrder);

        // Chain: 0 (Outer) -> 1 -> 2 (Inner)
        LayerHub.CreateLayers().Push(layer0).Push(layer1).Push(layer2).Build();

        // Layer 0 posts a Drop event. Expected hierarchical order: 0 -> 1 -> 2.
        layer0.PostDrop(new OrderEvent());

        // Pump to process
        LayerHub.Pump(0.02f); 

        Console.WriteLine("Drop Execution Order: " + string.Join(" -> ", _executionOrder));
        
        // Expected: 0, 1, 2.
        Assert.That(_executionOrder, Is.EqualTo(new[] { 0, 1, 2 }), "Drop order should be Outer -> Inner");
    }

    private class OrderTrackingLayer : Layer
    {
        private readonly int _id;
        private readonly List<int> _order;

        public OrderTrackingLayer(int id, List<int> order)
        {
            _id = id;
            _order = order;
            Subscribe<OrderEvent>(Handle);
        }

        private EventHandledState Handle(in OrderEvent evt)
        {
            _order.Add(_id);
            return EventHandledState.Continue;
        }
    }

    public struct OrderEvent { }
}
