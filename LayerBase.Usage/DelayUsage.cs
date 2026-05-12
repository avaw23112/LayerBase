using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Event.Delay;
using LayerBase.Layers;

namespace LayerBase.Usage;

public struct NotificationEvent
{
    public string Msg;
}

// 1. 定义 Service 负责业务逻辑
public partial class NotifyManager : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    public void RequestNotification(string message, float delay)
    {
        // 🚀 �?Service 中调用扩展方法，安全且符合架�?
        this.Delay(new NotificationEvent { Msg = message }, delay);
    }
}

public partial class NotifyLayer : Layer
{
    // [SubscribeDelay] 允许层级持有延迟发布的引�?
    [SubscribeDelay] public IDelayPublisher<NotificationEvent> DelayNotify { get; set; }

    public bool HasReceived { get; private set; }

    [SubscribeFlow]
    private EventHandledState OnNotify(in NotificationEvent e)
    {
        Console.WriteLine($"[Notify] Received: {e.Msg} at {DateTime.Now:HH:mm:ss.fff}");
        HasReceived = true;
        return EventHandledState.Continue;
    }
}

public static class DelayUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Delay Usage ---");
        LayerHub.Reset();

        var layer = new NotifyLayer();
        var manager = new NotifyManager();

        // 2. 注册 Service
        layer.RegisterService(manager);

        LayerHub.CreateLayers().Push(layer).Build();

        Console.WriteLine($"Setting delay for 0.5s at {DateTime.Now:HH:mm:ss.fff}");

        // 3. 通过获取到的 Service 实例发起请求
        layer.GetService<NotifyManager>().RequestNotification("Delayed Message", 0.5f);

        // 4. 驱动主循�?
        var timeout = 0;
        while (!layer.HasReceived && timeout < 20)
        {
            LayerHub.Pump(0.1f);
            Thread.Sleep(100);
            timeout++;
        }
    }
}