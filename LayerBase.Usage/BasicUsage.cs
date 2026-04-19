using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Usage;

public struct PlayerSpawnEvent
{
    public string Name;
    public int Level;
}

public partial class GameplayLayer : Layer
{
    [Subscribe]
    private EventHandledState OnPlayerSpawn(in PlayerSpawnEvent e)
    {
        Console.WriteLine($"[Gameplay] Player {e.Name} spawned at level {e.Level}");
        return EventHandledState.Continue;
    }
}

public static class BasicUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Basic Usage ---");
        LayerHub.Reset();
        
        var gameplay = new GameplayLayer();
        LayerHub.CreateLayers()
            .Push(gameplay)
            .Build();

        LayerHub.Send(new PlayerSpawnEvent { Name = "Hero", Level = 1 });
    }
}
