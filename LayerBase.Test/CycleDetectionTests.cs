using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

// --- Test Events ---
public struct Event_A
{
}

public struct Event_B
{
}

// --- Test Managers ---
public partial class DirectCycleManager : ILayerContext
{
    [SubscribeFlow]
    public EventHandledState OnEvent(in Event_A e)
    {
        this.Send(new Event_A());
        return EventHandledState.Continue;
    }
}

public partial class IndirectManagerA : ILayerContext
{
    [SubscribeFlow]
    public EventHandledState OnA(in Event_A e)
    {
        this.Send(new Event_B());
        return EventHandledState.Continue;
    }
}

public partial class IndirectManagerB : ILayerContext
{
    [SubscribeFlow]
    public EventHandledState OnB(in Event_B e)
    {
        this.Send(new Event_A());
        return EventHandledState.Continue;
    }
}

[TestFixture]
public class CycleDetectionTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Build_Should_Throw_When_Direct_Cycle_Detected()
    {
        TestContext.Progress.WriteLine(">>> [Test] Starting Direct Cycle Test...");

        var layer = new GameLayer();
        layer.RegisterService(new DirectCycleService());

        // 寮哄姏璇婃柇锛氭煡鐪?Generator 鎻愬彇鍑虹殑渚濊禆
        var mgr = new DirectCycleManager();
        var deps = ((IAutoSubscribe)mgr).GetEventDependencies().ToList();
        TestContext.Progress.WriteLine($"[Diagnostic] DirectCycleManager deps count: {deps.Count}");
        foreach (var d in deps) TestContext.Progress.WriteLine($"  - {d.Source.Name} -> {d.Target.Name}");

        var ex = Assert.Throws<EventCycleException>(() => { LayerHub.CreateLayers().Push(layer).Build(); });

        TestContext.Progress.WriteLine("[SUCCESS] Caught Expected Exception:");
        TestContext.Progress.WriteLine(ex.Message);
    }

    [Test]
    public void Build_Should_Throw_When_Indirect_Cycle_Detected()
    {
        TestContext.Progress.WriteLine(">>> [Test] Starting Indirect Cycle Test...");

        var layer = new GameLayer();
        layer.RegisterService(new IndirectCycleService());

        // 寮哄姏璇婃柇锛氭煡鐪嬩袱涓?Manager 鐨勪緷璧?
        var mgrA = new IndirectManagerA();
        var depsA = ((IAutoSubscribe)mgrA).GetEventDependencies().ToList();
        TestContext.Progress.WriteLine($"[Diagnostic] IndirectManagerA deps count: {depsA.Count}");
        foreach (var d in depsA) TestContext.Progress.WriteLine($"  - {d.Source.Name} -> {d.Target.Name}");

        var mgrB = new IndirectManagerB();
        var depsB = ((IAutoSubscribe)mgrB).GetEventDependencies().ToList();
        TestContext.Progress.WriteLine($"[Diagnostic] IndirectManagerB deps count: {depsB.Count}");
        foreach (var d in depsB) TestContext.Progress.WriteLine($"  - {d.Source.Name} -> {d.Target.Name}");

        var ex = Assert.Throws<EventCycleException>(() => { LayerHub.CreateLayers().Push(layer).Build(); });

        TestContext.Progress.WriteLine("[SUCCESS] Caught Expected Exception:");
        TestContext.Progress.WriteLine(ex.Message);

        Assert.That(ex.Message, Does.Contain("Event_A -> Event_B -> Event_A"));
    }

    private class GameLayer : Layer
    {
    }

    private class DirectCycleService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<DirectCycleManager>(new DirectCycleManager());
        }
    }

    private class IndirectCycleService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IndirectManagerA>(new IndirectManagerA());
            services.AddSingleton<IndirectManagerB>(new IndirectManagerB());
        }
    }
}

