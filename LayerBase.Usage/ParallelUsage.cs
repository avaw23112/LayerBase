using LayerBase;
using LayerBase.Layers;
using LayerBase.DI;
using LayerBase.Core.Event;

namespace Usage;

public class ParallelGameLayer : Layer { }

public struct HeavyComputeEvent { public int Data; }

public partial class ComputeManager : ILayerContext
{
    [SubscribeParallel]
    public EventHandledState OnCompute(in HeavyComputeEvent e) => EventHandledState.Continue;
}

public static class ParallelUsage
{
    public static void Run()
    {
        LayerHub.InitializeJobScheduler(4);
        var rt = LayerHub.CreateLayers().Push(new ParallelGameLayer()).Build();
        rt.Send(new HeavyComputeEvent { Data = 100 });
    }
}