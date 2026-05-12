using System.Diagnostics;
using LayerBase;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class IdleLayerPerformanceBenchmarks
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Benchmark_Pump_Performance_With_Mostly_Idle_Layers()
    {
        const int totalLayers = 64;
        const int iterations = 1000000;

        // Scenario A: 64 Idle Layers
        var builderA = LayerHub.CreateLayers();
        for (var i = 0; i < totalLayers; i++) builderA.Push(new IdleLayer(false));
        builderA.Build();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++) LayerHub.Pump(0.016f);
        sw.Stop();

        Console.WriteLine("\n--- Scene A: All 64 Layers Idle ---");
        Console.WriteLine(
            $"[All Idle] {iterations} iterations, Total: {sw.ElapsedMilliseconds}ms, Avg: {sw.Elapsed.TotalMilliseconds * 1000000 / iterations:F2}ns/pump");

        // Scenario B: Only the last layer is active
        LayerHub.Reset();
        var builderB = LayerHub.CreateLayers();
        for (var i = 0; i < totalLayers - 1; i++) builderB.Push(new IdleLayer(false));
        builderB.Push(new IdleLayer(true));
        builderB.Build();

        sw.Restart();
        for (var i = 0; i < iterations; i++) LayerHub.Pump(0.016f);
        sw.Stop();

        Console.WriteLine("\n--- Scene B: Only Last Layer (Index 63) Active ---");
        Console.WriteLine(
            $"[1 Layer Active (Tail)] {iterations} iterations, Total: {sw.ElapsedMilliseconds}ms, Avg: {sw.Elapsed.TotalMilliseconds * 1000000 / iterations:F2}ns/pump");
    }

    private class IdleLayer : Layer
    {
        private readonly bool _isActive;

        public IdleLayer(bool active)
        {
            _isActive = active;
        }

        public override bool HasActiveLogic => _isActive;

        public override void Pump(float deltaTime)
        {
            // 只有活跃层才执行一点点微小的逻辑
            if (_isActive)
            {
                /* No-op */
            }
        }
    }
}