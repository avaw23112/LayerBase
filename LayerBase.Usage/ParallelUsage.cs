using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;

namespace LayerBase.Usage;

public partial struct HeavyComputeEvent
{
    public int Data;
}

public class HeavyComputeEventMetaData : EventMetaData<HeavyComputeEvent>
{
}

public partial class ComputeLayer : Layer
{
    // [SubscribeParallel] 会在独立的线程池中执行�?
    // 这提供了极致的性能，同时也实现了故障隔离�?
    [SubscribeParallel]
    private void DoWork(in HeavyComputeEvent e)
    {
        if (e.Data < 0) throw new Exception("Compute Error!"); // 故意制造异�?

        Console.WriteLine($"[Parallel] Processing data: {e.Data} on thread {Thread.CurrentThread.ManagedThreadId}");
    }
}

public static class ParallelUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Parallel Usage ---");

        // 1. 初始化并行调度器（指定工作线程数�?
        LayerHub.InitializeJobScheduler(4);

        var compute = new ComputeLayer();
        LayerHub.CreateLayers().Push(compute).Build();

        // 2. 监听全局错误信息（故障隔离演示）
        LayerHub.OnLayerEventInfo += info =>
        {
            if (info.Type == LayerEventInfoType.Error)
                Console.WriteLine($"[ALERT] Fault Detected in {info.Source}: {info.Message}");
        };

        // 3. 正常分发
        LayerHub.Send(new HeavyComputeEvent { Data = 100 });

        // 4. 触发异常分发：该 Handler 会被自动“熔断”并上报，不影响后续分发
        LayerHub.Send(new HeavyComputeEvent { Data = -1 });

        Thread.Sleep(200);
    }
}