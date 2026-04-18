using System;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.Event.Delay;
using LayerBase.LayerHub;
using LayerBase.Layers;

namespace LayerBase.Template;

// ============================================================================
// 1. 定义事件 (Events)
// ============================================================================
public struct DamageEvent { public int Value; }
public struct MsgEvent { public string Text; }

// ============================================================================
// 2. 声明式 Manager
// ============================================================================

public partial class DamageManager : ILayerContext
{
    [Subscribe]
    private EventHandledState OnDamage(in DamageEvent evt)
    {
        Console.WriteLine($"[DamageManager] Handling {evt.Value} damage.");
        // 方案1提供的能力：直接 SendLocal
        this.SendLocal(new MsgEvent { Text = $"Log from DamageManager: {evt.Value}" });
        return EventHandledState.Continue;
    }
}

public partial class MessageManager : ILayerContext
{
    [Subscribe]
    private EventHandledState OnMsg(in MsgEvent evt)
    {
        Console.WriteLine($"[MessageManager] MSG RECEIVED: {evt.Text}");
        return EventHandledState.Continue;
    }

    [SubscribeParallel]
    private EventHandledState OnParallelMsg(in MsgEvent evt)
    {
        Console.WriteLine($"[MessageManager - Parallel] Background: {evt.Text}");
        return EventHandledState.Continue;
    }
    
    
}

// ============================================================================
// 3. 业务服务 (Services)
// ============================================================================

public class GameService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 严格保证顺序：MessageManager 先，DamageManager 后
        services.AddSingleton<MessageManager, MessageManager>();
        services.AddSingleton<DamageManager, DamageManager>();
    }

    public void Run()
    {
        Console.WriteLine("\n--- Dispatching Sync DamageEvent ---");
        // Service 自动获得发送能力
        this.SendGlobal(new DamageEvent { Value = 100 });
    }
}

// ============================================================================
// 4. 自定义层级
// ============================================================================

public class GameLayer : Layer { }

// ============================================================================
// 5. 运行入口 (Program)
// ============================================================================

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== LayerBase Full Capability Demo ===\n");
        LayerHub.LayerHub.InitializeJobScheduler(workerCount: 4);

        var layer = new GameLayer();
        var service = new GameService();
        layer.RegisterService(service);

        // 构建层级
        LayerHub.LayerHub.CreateLayers().Push(layer).Build();

        // 诊断输出
        var msgMgr = layer.GetService<MessageManager>();
        Console.WriteLine($"[Diagnostic] MessageManager implements IAutoSubscribe: {msgMgr is IAutoSubscribe}");
        Console.WriteLine($"[Diagnostic] MessageManager implements ILayerContext: {msgMgr is ILayerContext}");

        // 运行同步流程
        service.Run();

        Console.WriteLine("\n--- Dispatching Async MsgEvent ---");
        layer.PostGlobal(new MsgEvent { Text = "Hello Async World" });

        // 模拟运行循环
        for (int i = 0; i < 6; i++)
        {
            Console.WriteLine($"\n[Step {i}] Pumping...");
            LayerHub.LayerHub.Pump(0.1f);
            System.Threading.Thread.Sleep(50);
        }

        Console.WriteLine("\nDemo Finished.");
    }
}
