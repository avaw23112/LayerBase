using LayerBase;
using LayerBase.Layers;
using LayerBase.DI;
using LayerBase.Core.Event;

namespace Usage;

public class BasicGameLayer : Layer { }

public struct PlayerSpawnEvent
{
    public string Name;
    public int Level;
}

public partial class PlayerManager : ILayerContext
{
    [Subscribe]
    public EventHandledState OnPlayerSpawn(in PlayerSpawnEvent e)
    {
        Console.WriteLine($"Player Spawned: {e.Name}, Level: {e.Level}");
        return EventHandledState.Continue;
    }
}

public class PlayerModule : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<PlayerManager, PlayerManager>();
    }
}

public static class BasicUsage
{
    public static void Run()
    {
        var layer = new BasicGameLayer();
        layer.RegisterService(new PlayerModule());

        var rt = LayerHub.CreateLayers()
                         .Push(layer)
                         .Build();

        rt.Send(new PlayerSpawnEvent { Name = "Hero", Level = 1 });
        rt.Pump(0.1f);
    }
}