using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Usage;

// 1. 定义服务接口
public interface IDataService
{
    string GetData();
}

// 2. 实现服务
public class DataService : IDataService
{
    public string GetData()
    {
        return "Extreme Performance Data";
    }
}

public struct DataRequestEvent
{
    public string Query;
}

// 3. �?Layer 中使用服�?
public partial class ServiceLayer : Layer
{
    [Subscribe]
    private EventHandledState OnRequest(in DataRequestEvent req)
    {
        // 4. 获取服务实例
        var service = GetService<IDataService>();
        Console.WriteLine($"[ServiceLayer] Handled: {req.Query}, Service says: {service.GetData()}");
        return EventHandledState.Continue;
    }
}

public static class ServiceUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Service DI Usage ---");
        LayerHub.Reset();

        var layer = new ServiceLayer();
        LayerHub.CreateLayers().Push(layer).Build();

        LayerHub.Send(new DataRequestEvent { Query = "Get My Data" });
    }
}

