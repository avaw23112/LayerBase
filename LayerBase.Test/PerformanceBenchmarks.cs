using System.Diagnostics;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class PerformanceBenchmarks
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    private const int EventCount = 1_000_000;

    [Test]
    public void Benchmark_Memory_Locality_Impact()
    {
        const int totalHandlers = 10;

        // 场景 A: 集中式 (1层 x 10个Handler) - 内存极致连续
        LayerHub.Reset();
        var lA = new BenchLayer();
        for (var i = 0; i < totalHandlers; i++)
            lA.Subscribe((in BenchEvent _) =>
            {
                lA.HandledCount++;
                return EventHandledState.Continue;
            });
        LayerHub.CreateLayers().Push(lA).Build();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        sw.Stop();
        Console.WriteLine($"\n[内存密集型-集中分发] TPS: {EventCount / sw.Elapsed.TotalSeconds:N0}");

        // 场景 B: 碎片式 (10层 x 1个Handler) - 跨对象跳转
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        for (var i = 0; i < totalHandlers; i++)
        {
            var l = new BenchLayer();
            l.Subscribe((in BenchEvent _) =>
            {
                l.HandledCount++;
                return EventHandledState.Continue;
            });
            builder.Push(l);
        }

        builder.Build();

        sw.Restart();
        for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        sw.Stop();
        Console.WriteLine($"[内存碎片型-跨桶分发] TPS: {EventCount / sw.Elapsed.TotalSeconds:N0}");
    }

    private class BenchLayer : Layer
    {
        public int HandledCount;
    }

    public struct BenchEvent
    {
    }
}