using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class NotifySafeTests
{
    [SetUp]
    public void Setup()
    {
        LayerHub.Reset();
    }

    public struct TestEvent { public List<string>? Order; }

    public class TestLayer : Layer { }

    [Test]
    public void NotifySafe_ShouldExecuteInOrder()
    {
        var layer = new TestLayer();
        LayerHub.CreateLayers().Push(layer).Build();
        
        var order = new List<string>();
        
        layer.SubscribeNotify((in TestEvent e) => e.Order!.Add("Notify"));
        layer.SubscribeNotifySafe((in TestEvent e) => e.Order!.Add("NotifySafe"));
        layer.Subscribe((in TestEvent e) => { e.Order!.Add("Sync"); return EventHandledState.Continue; });

        LayerHub.Send(new TestEvent { Order = order });

        Assert.That(order.Count, Is.EqualTo(3));
        Assert.That(order[0], Is.EqualTo("Notify"));
        Assert.That(order[1], Is.EqualTo("NotifySafe"));
        Assert.That(order[2], Is.EqualTo("Sync"));
    }

    [Test]
    public void NotifySafe_ExceptionShouldNotInterrupt()
    {
        var layer = new TestLayer();
        LayerHub.CreateLayers().Push(layer).Build();
        
        var executed = false;
        
        layer.SubscribeNotifySafe((in TestEvent e) => throw new Exception("Safe Crash"));
        layer.Subscribe((in TestEvent e) => { executed = true; return EventHandledState.Continue; });

        Assert.DoesNotThrow(() => LayerHub.Send(new TestEvent()));
        Assert.That(executed, Is.True);
    }
}