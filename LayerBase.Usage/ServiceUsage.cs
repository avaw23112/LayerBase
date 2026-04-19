using LayerBase;
using LayerBase.Layers;
using LayerBase.DI;
namespace Usage;
public class ServiceGameLayer : Layer { }
public class ServiceUsage {
    public struct DataRequestEvent { public string Query; }
    public static void Run()
 {
        var rt = LayerHub.CreateLayers().Push(new ServiceGameLayer()).Build();
        rt.Send(new DataRequestEvent { Query = "Get My Data" });
    }
}