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

public partial class TestBindableService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    [Subscribe]
    public void OnOrder(in OrderEvent e)
    {
    }
}

public partial class PlainLayerContextOnly : ILayerContext
{
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

    [Test]
    public void Generated_subscribe_types_should_expose_binding_accessor_slots()
    {
        var layer = new GameLayer();
        var bindableService = new TestBindableService();

        layer.RegisterService(new CapabilityTestService(_trace));
        layer.RegisterService(bindableService);

        LayerHub.CreateLayers().Push(layer).Build();

        var manager = layer.GetService<TestManagerC>();

        Assert.That(manager, Is.InstanceOf<IInternalLayerContext>());
        Assert.That(manager, Is.InstanceOf<ILayerBindingAccessor>());
        Assert.That(((ILayerBindingAccessor)manager).__LayerBaseBinding, Is.Not.Null);

        Assert.That(bindableService, Is.InstanceOf<ILayerBindingAccessor>());
        Assert.That(((ILayerBindingAccessor)bindableService).__LayerBaseBinding, Is.Not.Null);
    }

    [Test]
    public void Plain_layer_context_without_subscribe_should_not_generate_binding_accessor()
    {
        var context = new PlainLayerContextOnly();

        Assert.That(context, Is.Not.InstanceOf<IInternalLayerContext>());
        Assert.That(context, Is.Not.InstanceOf<ILayerBindingAccessor>());
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