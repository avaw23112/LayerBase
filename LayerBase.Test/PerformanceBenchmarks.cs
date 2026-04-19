using System.Diagnostics;
using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class PerformanceBenchmarks
{
    [SetUp] public void SetUp() => LayerHub.Reset();
    private const int EventCount = 1_000_000;

    [Test]
    public void Benchmark_Density_Comparison()
    {
        // 场景 A: 10层全订阅 (高密度负载)
        var layersFull = CreateBenchChain(10);
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        sw.Stop();
        Console.WriteLine($"\n[高密度-10层全订阅] TPS: {EventCount / sw.Elapsed.TotalSeconds:N0} (由于每层都有订阅，实际执行了 {EventCount * 10:N0} 次 Handler)");

        // 场景 B: 10层单订阅 (模拟真实业务或之前的 25M 场景)
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        var layersSingle = new List<BenchLayer>();
        for (var i = 0; i < 10; i++)
        {
            var l = new BenchLayer();
            if (i == 9) l.Subscribe((in BenchEvent _) => { l.HandledCount++; return EventHandledState.Continue; });
            builder.Push(l);
            layersSingle.Add(l);
        }
        builder.Build();
        
        sw.Restart();
        for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        sw.Stop();
        Console.WriteLine($"[低密度-10层单订阅] TPS: {EventCount / sw.Elapsed.TotalSeconds:N0}");
    }

    private List<BenchLayer> CreateBenchChain(int layerCount)
    {
        var builder = LayerHub.CreateLayers();
        var list = new List<BenchLayer>();
        for (var i = 0; i < layerCount; i++)
        {
            var layer = new BenchLayer();
            layer.Subscribe((in BenchEvent _) => { layer.HandledCount++; return EventHandledState.Continue; });
            builder.Push(layer);
            list.Add(layer);
        }
        builder.Build();
        return list;
    }

    private class BenchLayer : Layer { public int HandledCount; }
    public struct BenchEvent {}
}
