using System.Diagnostics;
using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class PerformanceBenchmarks
{
    private const int EventCount = 1_000_000;

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Benchmark_Synchronous_Global_Send()
    {
        var layers = CreateBenchChain(10);
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < EventCount; i++)
        {
            LayerHub.Send(new BenchEvent());
        }

        sw.Stop();
        double tps = EventCount / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"[Sync Global] Total: {EventCount}, Time: {sw.ElapsedMilliseconds}ms, TPS: {tps:N0}");
        
        // 验证：每层都应该收到了百万次调用
        Assert.That(layers[9].HandledCount, Is.EqualTo(EventCount));
    }

    [Test]
    public void Benchmark_Asynchronous_Global_Post_Throughput()
    {
        var layers = CreateBenchChain(10);
        
        // 预热：让懒加载完成，让队列池初始化，消除抖动
        LayerHub.Post(new BenchEvent());
        for(int i=0; i<20; i++) LayerHub.Pump(0.01f); 
        foreach(var l in layers) l.HandledCount = 0;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long memBefore = GC.GetTotalMemory(true);
        
        var sw = Stopwatch.StartNew();

        // 1. 测试极致入队速度 (只管发)
        for (int i = 0; i < EventCount; i++)
        {
            LayerHub.Post(new BenchEvent());
        }
        
        long enqueueTime = sw.ElapsedMilliseconds;
        
        // 2. 测试处理速度 (分帧推进)
        // 在接力模式下，每一轮 Pump 推进一层。
        // 10层结构，最快需要 10 轮 Pump 能处理完第一批，但这里是百万级积压。
        // 我们持续 Pump 直到最后一层处理完所有事件。
        int totalPumps = 0;
        while (layers[9].HandledCount < EventCount)
        {
            LayerHub.Pump(0.01f);
            totalPumps++;
        }

        sw.Stop();
        long memAfter = GC.GetTotalMemory(false);
        
        double totalTps = EventCount / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"[Async Global] Total: {EventCount}, Enqueue: {enqueueTime}ms, TotalTime: {sw.ElapsedMilliseconds}ms, Pumps: {totalPumps}, TPS: {totalTps:N0}");
        Console.WriteLine($"[Memory] Delta: {(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB (主要来自队列内部扩容，稳态后应为0)");
    }

    private List<BenchLayer> CreateBenchChain(int layerCount)
    {
        var builder = LayerHub.CreateLayers();
        var list = new List<BenchLayer>();
        for (int i = 0; i < layerCount; i++)
        {
            var layer = new BenchLayer();
            builder.Push(layer);
            list.Add(layer);
        }
        builder.Build();
        return list;
    }

    private class BenchLayer : Layer
    {
        public int HandledCount = 0;
        public BenchLayer()
        {
            Subscribe<BenchEvent>((in BenchEvent _) =>
            {
                HandledCount++;
                return EventHandledState.Continue;
            });
        }
    }

    public struct BenchEvent { }
}
