using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Usage;

// 1. 定义事件（推荐使用 struct 以获得极致性能）
public struct PlayerSpawnEvent
{
    public string Name;
    public int Level;
}

// 2. 定义 Layer 并使用 partial 关键字开启 Source Generator 优化
public partial class GameplayLayer : Layer
{
    // 使用 [Subscribe] 特性自动订阅。
    // 方法必须是 partial 类的一部分，且建议参数带 in 关键字以减少结构体复制。
    [Subscribe]
    private EventHandledState OnPlayerSpawn(in PlayerSpawnEvent e)
    {
        Console.WriteLine($"[Gameplay] Player {e.Name} spawned at level {e.Level}");
        // 返回 Continue 让事件继续流向后续 Layer，返回 Handled 则截断事件流。
        return EventHandledState.Continue;
    }
}

public static class BasicUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Basic Usage ---");
        
        // 重置/初始化环境
        LayerHub.Reset();
        
        // 3. 构建层级拓扑
        var gameplay = new GameplayLayer();
        LayerHub.CreateLayers()
            .Push(gameplay)
            .Build();

        // 4. 发送同步事件
        LayerHub.Send(new PlayerSpawnEvent { Name = "Hero", Level = 1 });
    }
}
