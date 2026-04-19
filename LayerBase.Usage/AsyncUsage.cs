using LayerBase;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;

namespace Usage;

public class AsyncGameLayer : Layer { }

public struct AssetLoadRequest { public string AssetPath; }

public partial class LoaderManager : ILayerContext
{
    [SubscribeAsync]
    public async LBTask OnLoad(AssetLoadRequest e)
    {
        await LBTask.Delay(TimeSpan.FromMilliseconds(1000));
    }
}

public static class AsyncUsage
{
    public static void Run()
    {
        var rt = LayerHub.CreateLayers().Push(new AsyncGameLayer()).Build();
        rt.Send(new AssetLoadRequest { AssetPath = "Textures/Player.png" });
    }
}