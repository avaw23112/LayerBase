using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Usage;

public struct AssetLoadRequest
{
    public string AssetPath;
}

public partial class ResourceLayer : Layer
{
    public bool IsLoadFinished { get; private set; }

    [SubscribeAsync]
    private async LBTask OnLoadAsset(AssetLoadRequest e)
    {
        Console.WriteLine($"[Resource] Starting load: {e.AssetPath}");
        await LBTask.Delay(TimeSpan.FromMilliseconds(500));
        Console.WriteLine($"[Resource] Finished load: {e.AssetPath}");
        IsLoadFinished = true;
    }
}

public static class AsyncUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Async Usage ---");
        LayerHub.Reset();

        var resource = new ResourceLayer();
        LayerHub.CreateLayers().Push(resource).Build();

        // 1. 发送异步事�?
        LayerHub.Send(new AssetLoadRequest { AssetPath = "Textures/Player.png" });

        // 2. 核心：驱动循�?(Main Loop)
        var timeout = 0;
        while (!resource.IsLoadFinished && timeout < 20)
        {
            LayerHub.Pump(0.1f);
            Thread.Sleep(100);
            timeout++;
        }
    }
}

