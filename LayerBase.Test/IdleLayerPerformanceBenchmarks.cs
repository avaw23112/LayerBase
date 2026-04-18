using System.Diagnostics;
using LayerBase.LayerHub;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class IdleLayerPerformanceBenchmarks
{
    private const int PumpIterations = 1_000_000;
    private const int MaxLayers = 64;

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Benchmark_Pump_Performance_With_Mostly_Idle_Layers()
    {
        // 创建 64 个层级，这是我们的上限
        var builder = LayerHub.CreateLayers();
        var layers = new List<Layer>();
        for (int i = 0; i < MaxLayers; i++)
        {
            var l = new IdleLayer();
            builder.Push(l);
            layers.Add(l);
        }
        builder.Build();

        // 场景 A: 彻底全空闲
        Console.WriteLine("\n--- Scene A: All 64 Layers Idle ---");
        RunPumpBenchmark("All Idle", PumpIterations);

        // 场景 B: 仅最后一层活跃 (触发最长跳转)
        Console.WriteLine("\n--- Scene B: Only Last Layer (Index 63) Active ---");
        ((IdleLayer)layers[MaxLayers - 1]).MarkActive();
        // 重新 Build 以更新 LayerChain 内部的逻辑掩码
        // 在实际项目中，Build 只调一次，但这里模拟状态变更后的效率
        LayerHub.Reset(); // 彻底重置
        builder = LayerHub.CreateLayers();
        layers.Clear();
        for (int i = 0; i < MaxLayers; i++)
        {
            var l = new IdleLayer();
            if (i == MaxLayers - 1) l.MarkActive();
            builder.Push(l);
            layers.Add(l);
        }
        builder.Build();
        
        RunPumpBenchmark("1 Layer Active (Tail)", PumpIterations);
    }

    private void RunPumpBenchmark(string label, int iterations)
    {
        // 预热
        for (int i = 0; i < 1000; i++) LayerHub.Pump(0.01f);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            LayerHub.Pump(0.01f);
        }
        sw.Stop();

        double averageNs = (sw.Elapsed.TotalMilliseconds * 1_000_000.0) / iterations;
        Console.WriteLine($"[{label}] {iterations} iterations, Total: {sw.ElapsedMilliseconds}ms, Avg: {averageNs:F2}ns/pump");
    }

    private class IdleLayer : Layer
    {
        private bool _isActive = false;
        public void MarkActive() => _isActive = true;

        // 重写这个属性来模拟拥有 Service Update 的活跃层
        public new bool HasActiveLogic => _isActive;

        public override void Update()
        {
            // 只有活跃层才执行一点点微小的逻辑
            if (_isActive) { /* No-op */ }
        }
    }
}
