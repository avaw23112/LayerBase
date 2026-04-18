using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.LayerHub;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class ManagerAutoSubscriptionTests
{
    private List<string> _trace;

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        _trace = new List<string>();
    }

    [Test]
    public void Subscriptions_Should_Follow_Registration_Order()
    {
        var layer = new TestLayer();
        var service = new OrderTestService(_trace);
        layer.RegisterService(service);
        
        // Build the layer. 
        LayerHub.CreateLayers().Push(layer).Build();

        // Send a global event
        LayerHub.Send(new OrderEvent());

        // Check sequence
        Assert.That(_trace, Is.EqualTo(new[] { "ManagerA", "ManagerB" }), 
            "Subscription order should match registration order in ConfigureServices");
    }

    [Test]
    public void Managers_Should_Inherit_Layer_Context_Capability()
    {
        var layer = new TestLayer();
        var service = new CapabilityTestService(_trace);
        layer.RegisterService(service);

        LayerHub.CreateLayers().Push(layer).Build();

        // Get the manager and trigger a local send
        var manager = layer.GetService<TestManager>();
        manager.DoLocalSend();

        Assert.That(_trace, Is.EqualTo(new[] { "HandledByManager" }));
    }

    private class TestLayer : Layer { }

    private class OrderTestService : IService
    {
        private readonly List<string> _trace;
        public OrderTestService(List<string> trace) => _trace = trace;

        public void ConfigureServices(IServiceCollection services)
        {
            // Register in specific order
            services.AddSingleton(new TestManagerA(_trace));
            services.AddSingleton(new TestManagerB(_trace));
        }
    }

    private class CapabilityTestService : IService
    {
        private readonly List<string> _trace;
        public CapabilityTestService(List<string> trace) => _trace = trace;
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new TestManager(_trace));
        }
    }

    // --- Mocking Generator Output ---

    private class TestManagerA : ILayerContext, IAutoSubscribe
    {
        private readonly List<string> _trace;
        public TestManagerA(List<string> trace) => _trace = trace;
        void IAutoSubscribe.AutoBind(Layer layer)
        {
            layer.Subscribe<OrderEvent>((in OrderEvent _) => {
                _trace.Add("ManagerA");
                return EventHandledState.Continue;
            });
        }
    }

    private class TestManagerB : ILayerContext, IAutoSubscribe
    {
        private readonly List<string> _trace;
        public TestManagerB(List<string> trace) => _trace = trace;
        void IAutoSubscribe.AutoBind(Layer layer)
        {
            layer.Subscribe<OrderEvent>((in OrderEvent _) => {
                _trace.Add("ManagerB");
                return EventHandledState.Continue;
            });
        }
    }

    private class TestManager : ILayerContext, IAutoSubscribe
    {
        private readonly List<string> _trace;
        public TestManager(List<string> trace) => _trace = trace;

        void IAutoSubscribe.AutoBind(Layer layer)
        {
            layer.Subscribe<CapabilityEvent>((in CapabilityEvent _) => {
                _trace.Add("HandledByManager");
                return EventHandledState.Continue;
            });
        }

        public void DoLocalSend()
        {
            // Verifying the extension method is accessible and works
            this.SendLocal(new CapabilityEvent());
        }
    }

    public struct OrderEvent { }
    public struct CapabilityEvent { }
}
