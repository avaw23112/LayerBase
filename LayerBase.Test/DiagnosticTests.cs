using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

// --- Test Items (Defined at Top-Level for Stability) ---

public struct SomeEvent
{
}

public struct UnsubscribedEvent
{
}

public partial class TestDiagManager : ILayerContext
{
    [SubscribeFlow]
    public EventHandledState OnEvent(in SomeEvent e)
    {
        return EventHandledState.Continue;
    }
}

public partial class ProducerManager : ILayerContext
{
    [SubscribeFlow]
    public EventHandledState OnStart(in SomeEvent e)
    {
        this.Send(new UnsubscribedEvent());
        return EventHandledState.Continue;
    }
}

public class TestDiagLayer : Layer
{
}

public partial class TestDiagService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<TestDiagManager>(new TestDiagManager());
    }
}

public partial class ProducerService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ProducerManager>(new ProducerManager());
    }
}

[TestFixture]
public class DiagnosticTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        _logs = new List<LayerEventInfo>();
        LayerHub.OnLayerEventInfo += info => _logs.Add(info);
    }

    private List<LayerEventInfo> _logs;

    [Test]
    public void DebugMode_Should_Report_Topology_On_Build()
    {
        var layer = new TestDiagLayer();
        layer.RegisterService(new TestDiagService());

        LayerHub.CreateLayers()
                .Push(layer)
                .SetDebug()
                .Build();

        var topologyLog = _logs.Find(l => l.Source == "System" && l.EventName == "Topology");
        Assert.That(topologyLog.Message, Does.Contain("TestDiagLayer"));
        Assert.That(topologyLog.Message, Does.Contain("-> [M] TestDiagManager"));

        TestContext.Out.WriteLine("Captured Topology Log:\n" + topologyLog.Message);
    }

    [Test]
    public void DeadLetter_Should_Report_Warning_On_Build_In_DebugMode()
    {
        var layer = new TestDiagLayer();
        layer.RegisterService(new ProducerService()); // ProducerManager 发送但没人订阅 UnsubscribedEvent

        LayerHub.CreateLayers()
                .Push(layer)
                .SetDebug()
                .Build(); // 期待在此处触?TopologyAudit

        var warningLog = _logs.Find(l => l.Source == "TopologyAudit" && l.Type == LayerEventInfoType.Warning);

        // 验证警告内容 (LayerEventInfo ?struct，所以查 EventName 是否被填?
        Assert.That(warningLog.EventName, Is.EqualTo("UnsubscribedEvent"));

        TestContext.Out.WriteLine("Captured Build-Time Dead Letter Log: " + warningLog.Message);
    }
}
