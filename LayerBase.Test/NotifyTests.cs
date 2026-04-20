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

    public class TestLayer : Layer { }

    public struct TestEvent
    {
        public int Value;
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
