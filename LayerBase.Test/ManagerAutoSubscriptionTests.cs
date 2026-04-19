using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class ManagerAutoSubscriptionTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        _trace = new List<string>();
    }

    private List<string> _trace;

    [Test]
    public void Manager_subscriptions_are_automatically_registered()
    {
        var layer = new TestManagerLayer(_trace);
        var rt = LayerHub.CreateLayers().Push(layer).Build();

        rt.Send(new OrderEvent());

        Assert.That(_trace, Is.EqualTo(new[] { "ManagerA", "ManagerB" }),
            "Subscription order should match registration order in ConfigureServices");
    }
}

internal class TestManagerLayer : Layer
{
    public TestManagerLayer(List<string> trace)
    {
        RegisterService(new OrderTestService(trace));
    }
}

internal class OrderTestService : IService
{
    private readonly List<string> _trace;
    public OrderTestService(List<string> trace) => _trace = trace;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<TestManagerA>(sp => new TestManagerA(_trace));
        services.AddScoped<TestManagerB>(sp => new TestManagerB(_trace));
    }
}

public partial class TestManagerA : ILayerContext
{
    private readonly List<string> _trace;
    public TestManagerA(List<string> trace) => _trace = trace;

    [Subscribe]
    public EventHandledState OnEvent(in OrderEvent e)
    {
        _trace.Add("ManagerA");
        return EventHandledState.Continue;
    }
}

public partial class TestManagerB : ILayerContext
{
    private readonly List<string> _trace;
    public TestManagerB(List<string> trace) => _trace = trace;

    [Subscribe]
    public EventHandledState OnEvent(in OrderEvent e)
    {
        _trace.Add("ManagerB");
        return EventHandledState.Continue;
    }
}

public struct OrderEvent { }
