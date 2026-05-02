using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

// --- Test Events ---
public struct OrderEvent
{
}

public struct CapabilityEvent
{
}

// --- Test Managers (Top Level) ---

public partial class TestManagerA : ILayerContext
{
    private readonly List<string> _trace;

    public TestManagerA(List<string> trace)
    {
        _trace = trace;
    }

    [SubscribeFlow]
    public EventHandledState OnOrder(in OrderEvent e)
    {
        _trace.Add("ManagerA");
        return EventHandledState.Continue;
    }
}

public partial class TestManagerB : ILayerContext
{
    private readonly List<string> _trace;

    public TestManagerB(List<string> trace)
    {
        _trace = trace;
    }

    [SubscribeFlow]
    public EventHandledState OnOrder(in OrderEvent e)
    {
        _trace.Add("ManagerB");
        return EventHandledState.Continue;
    }
}

public partial class TestManagerC : ILayerContext
{
    private readonly List<string> _trace;

    public TestManagerC(List<string> trace)
    {
        _trace = trace;
    }

    [SubscribeFlow]
    public EventHandledState OnCap(in CapabilityEvent e)
    {
        _trace.Add("HandledByManager");
        return EventHandledState.Continue;
    }

    public void DoSend()
    {
        this.Send(new CapabilityEvent());
    }
}

[TestFixture]
public partial class ManagerAutoSubscriptionTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        _trace = new List<string>();
    }

    private List<string> _trace;

    [Test]
    public void Subscriptions_Should_Follow_Registration_Order()
    {
        var layer = new GameLayer();
        var service = new OrderTestService(_trace);
        layer.RegisterService(service);

        LayerHub.CreateLayers().Push(layer).Build();

        LayerHub.Send(new OrderEvent());

        Assert.That(_trace, Is.EqualTo(new[] { "ManagerA", "ManagerB" }),
            "Subscription order should match registration order in ConfigureServices");
    }

    [Test]
    public void Managers_Should_Inherit_Layer_Context_Capability()
    {
        var layer = new GameLayer();
        var service = new CapabilityTestService(_trace);
        layer.RegisterService(service);

        LayerHub.CreateLayers().Push(layer).Build();

        var manager = layer.GetService<TestManagerC>();
        manager.DoSend();

        Assert.That(_trace, Is.EqualTo(new[] { "HandledByManager" }));
    }

    private class GameLayer : Layer
    {
    }

    private partial class OrderTestService : IService
    {
        private readonly List<string> _trace;

        public OrderTestService(List<string> trace)
        {
            _trace = trace;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TestManagerA>(new TestManagerA(_trace));
            services.AddSingleton<TestManagerB>(new TestManagerB(_trace));
        }
    }

    private partial class CapabilityTestService : IService
    {
        private readonly List<string> _trace;

        public CapabilityTestService(List<string> trace)
        {
            _trace = trace;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TestManagerC>(new TestManagerC(_trace));
        }
    }
}
