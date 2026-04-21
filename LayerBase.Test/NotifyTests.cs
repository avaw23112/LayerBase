using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class NotifyTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Notify_Handler_Receives_Event()
    {
        var layer = new TestLayer();
        var manager = new TestNotifyManager();
        layer.RegisterService(manager);
        
        LayerHub.CreateLayers().Push(layer).Build();

        LayerHub.Send(new TestEvent { Value = 42 });

        Assert.That(manager.ReceivedValue, Is.EqualTo(42));
        Assert.That(manager.CallCount, Is.EqualTo(1));
    }

    [Test]
    public void Notify_Small_Fanout_Preserves_Registration_Order()
    {
        var layer = new TestLayer();
        var trace = new List<string>();

        LayerHub.CreateLayers().Push(layer).Build();

        layer.SubscribeNotify<TestEvent>(static (in TestEvent e) =>
        {
            e.Trace!.Add("First");
        });
        layer.SubscribeNotify<TestEvent>(static (in TestEvent e) =>
        {
            e.Trace!.Add("Second");
        });
        layer.SubscribeNotify<TestEvent>(static (in TestEvent e) =>
        {
            e.Trace!.Add("Third");
        });

        LayerHub.Send(new TestEvent { Value = 7, Trace = trace });

        Assert.That(trace, Is.EqualTo(new[] { "First", "Second", "Third" }));
    }

    public class TestLayer : Layer { }

    public struct TestEvent
    {
        public int Value;
        public List<string>? Trace;
    }
}

public partial class TestNotifyManager : IService
{
    public int ReceivedValue;
    public int CallCount;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in NotifyTests.TestEvent e)
    {
        ReceivedValue = e.Value;
        CallCount++;
    }
}
