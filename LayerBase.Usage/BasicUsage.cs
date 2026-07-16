using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Usage;

// 1. 定义事件（推荐使�?struct 以获得极致性能�?
public struct PlayerSpawnEvent
{
    public string Name;
    public int Level;
}

// 2. 定义 Layer 并使�?partial 关键字开�?Source Generator 优化
public partial class GameplayLayer : Layer
{
    // 使用 [SubscribeFlow] 特性自动订阅�?
    // 方法必须�?partial 类的一部分，且建议参数�?in 关键字以减少结构体复制�?
    [SubscribeFlow]
    private EventHandledState OnPlayerSpawn(in PlayerSpawnEvent e)
    {
        Console.WriteLine($"[Gameplay] Player {e.Name} spawned at level {e.Level}");
        // 返回 Continue 让事件继续流向后�?Layer，返�?Handled 则截断事件流�?
        return EventHandledState.Continue;
    }
}

public static class BasicUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Basic Usage ---");

        // 重置/初始化环�?
        LayerHub.Reset();

        // 3. 构建层级拓扑
        var gameplay = new GameplayLayer();
        var runtime = LayerHub.CreateLayers()
                              .Push(gameplay)
                              .Build();

        // 4. 发送同步事�?
        runtime.Send(new PlayerSpawnEvent { Name = "Hero", Level = 1 });
    }
}
