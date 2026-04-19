using LayerBase.Core.Event;
using LayerBase.Layers;
using LayerBase;
using LayerBase.Core.EventHandler;

namespace EventsTest;

[TestFixture]
public partial class StaticDispatchBenchmark
{
    public struct BenchmarkEvent { public int Value; }

    [Test]
    public unsafe void Compare_Standard_Vs_Optimized()
    {
        const int iterations = 10_000_000;
        
        // --- 1. 标准委托分发测试 ---
        LayerHub.Reset();
        var standardLayer = new FastLayer();
        LayerHub.CreateLayers().Push(standardLayer).Build();
        // 手动使用标准订阅
        standardLayer.Subscribe((in BenchmarkEvent e) => standardLayer.Handle(in e));
        
        // 预热
        for (int i = 0; i < 1000; i++) LayerHub.Send(new BenchmarkEvent());
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) LayerHub.Send(new BenchmarkEvent());
        sw.Stop();
        var standardMs = sw.ElapsedMilliseconds;
        TestContext.WriteLine($"Standard Delegate Dispatch ({iterations:N0}): {standardMs}ms");

        // --- 2. 透明静态桥接分发测试 ---
        LayerHub.Reset();
        var optimizedLayer = new FastLayer();
        LayerHub.CreateLayers().Push(optimizedLayer).Build();
        // 手动注入桥接器 (模拟生成器)
        delegate*<object, in BenchmarkEvent, EventHandledState> bridgePtr = &FastLayer.ManualBridge;
        optimizedLayer.SubscribeOptimized<BenchmarkEvent>((IntPtr)bridgePtr, optimizedLayer, "FastLayer.Handle");
        
        // 预热
        for (int i = 0; i < 1000; i++) LayerHub.Send(new BenchmarkEvent());

        sw.Restart();
        for (int i = 0; i < iterations; i++) LayerHub.Send(new BenchmarkEvent());
        sw.Stop();
        var optimizedMs = sw.ElapsedMilliseconds;
        TestContext.WriteLine($"Transparent Bridge Dispatch  ({iterations:N0}): {optimizedMs}ms");

        // --- 结果对比 ---
        double improvement = (double)(standardMs - optimizedMs) / standardMs * 100;
        TestContext.WriteLine($"Performance Improvement: {improvement:F2}%");
        
        Assert.That(optimizedMs, Is.LessThanOrEqualTo(standardMs), "Optimized path should be faster or equal.");
    }
}

public partial class FastLayer : Layer
{
    public int ReceivedCount;
    
    // 模拟被 [Subscribe] 标记的方法
    internal EventHandledState Handle(in StaticDispatchBenchmark.BenchmarkEvent e)
    {
        ReceivedCount++;
        return EventHandledState.Continue;
    }

    // 🚀 手动桥接器逻辑 (Generator 应该生成的代码)
    internal static EventHandledState ManualBridge(object instance, in StaticDispatchBenchmark.BenchmarkEvent e)
    {
        return ((FastLayer)instance).Handle(in e);
    }
}
