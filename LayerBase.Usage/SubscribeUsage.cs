using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;
using LayerBase.DI;

namespace LayerBase.Usage;

public struct AuditLogEvent
{
    public string Message;
}

public partial class AuditService : IService
{
    public void ConfigureServices(IServiceCollection services) => services.AddSingleton(this);

    // 使用 [Subscribe] 標記，源生成器會自動註冊�?Subscribe 鏈條
    [Subscribe]
    public void OnAuditLog(in AuditLogEvent e)
    {
        Console.WriteLine($"[Audit] {e.Message}");
    }
}

public partial class OrderService : IService
{
    public void ConfigureServices(IServiceCollection services) => services.AddSingleton(this);

    [SubscribeFlow]
    public EventHandledState OnOrderPlaced(in AuditLogEvent e)
    {
        Console.WriteLine($"[Business] Processing order for: {e.Message}");
        return EventHandledState.Continue;
    }
}

public class MainLayer : Layer
{
}

public class SubscribeUsage
{
    public static void Run()
    {
        // 1. 初始�?Layer
        var layer = new MainLayer();

        // 2. 註冊服務 (源生成器會自動掃�?[Subscribe] 並註�?
        layer.RegisterService(new AuditService());
        layer.RegisterService(new OrderService());

        // 3. 構建 LayerHub (這會觸發 Build 過程，包�?AutoBind)
        var runtime = LayerHub.CreateLayers().Push(layer).Build();

        Console.WriteLine("Dispatching event...");
        // 4. 發送全局事件
        runtime.Send(new AuditLogEvent { Message = "User123 logged in" });
    }
}
