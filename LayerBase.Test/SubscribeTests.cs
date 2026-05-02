using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public partial class SubscribeTests
{
    [SetUp]
    public void Setup()
    {
        LayerHub.Reset();
    }

    public struct TestEvent { public List<string>? Order; }

    public class TestLayer : Layer { }

    [Test]
    public void Subscribe_ShouldExecuteInOrder()
    {
        var layer = new TestLayer();
        LayerHub.CreateLayers().Push(layer).Build();
        
        var order = new List<string>();
        
        layer.SubscribeNotify((in TestEvent e) => e.Order!.Add("Notify"));
        layer.Subscribe((in       TestEvent e) => e.Order!.Add("SubscribeSafe"));
        layer.SubscribeFlow((in   TestEvent e) => { e.Order!.Add("SyncFlow"); return EventHandledState.Continue; });

        LayerHub.Send(new TestEvent { Order = order });

        Assert.That(order.Count, Is.EqualTo(3));
        Assert.That(order[0], Is.EqualTo("Notify"));
        Assert.That(order[1], Is.EqualTo("SubscribeSafe"));
        Assert.That(order[2], Is.EqualTo("SyncFlow"));
    }

    [Test]
    public void Subscribe_ExceptionShouldNotInterrupt()
    {
        var layer = new TestLayer();
        LayerHub.CreateLayers().Push(layer).Build();
        
        var executed = false;
        
        // Subscribe (原 Subscribe) 具備異常隔離
        layer.Subscribe((in TestEvent e) => throw new Exception("Safe Crash"));
        layer.SubscribeFlow((in TestEvent e) => { executed = true; return EventHandledState.Continue; });

        Assert.DoesNotThrow(() => LayerHub.Send(new TestEvent()));
        Assert.That(executed, Is.True);
    }

    public struct Event_X { }
    public struct Event_Y { }

    private partial class MockCycleSubscriber : IService, IAutoSubscribe
    {
        public void ConfigureServices(IServiceCollection services) => services.AddSingleton(this);
        public void AutoBind(Layer layer) { }
        public IEnumerable<EventDependency> GetEventDependencies()
        {
            // 模擬 Subscribe 中的 Send 行為：訂閱 X，但在處理 X 時發送 Y (同步)
            yield return new EventDependency(typeof(Event_X), typeof(Event_Y));
            // 訂閱 Y，但在處理 Y 時發送 X (同步) -> 形成環
            yield return new EventDependency(typeof(Event_Y), typeof(Event_X));
        }
        public IEnumerable<Type> GetSubscribedEvents()
        {
            yield return typeof(Event_X);
            yield return typeof(Event_Y);
        }
    }

    [Test]
    public void Subscribe_CycleDetection_ShouldThrow()
    {
        var layer = new TestLayer();
        // 使用標準註冊流程，確保在 FinalizeBuild 時被納入 DiscoveredSubscribers
        layer.RegisterService(new MockCycleSubscriber());
        
        var ex = Assert.Throws<EventCycleException>(() => {
            LayerHub.CreateLayers().Push(layer).Build();
        });
        
        Assert.That(ex.Message, Does.Contain("Synchronous event cycle detected"));
        Assert.That(ex.Message, Does.Contain("Event_X -> Event_Y -> Event_X"));
    }
}
