using LayerBase.DI;
using LayerBase.Layers;

namespace LayerBase.Usage;

public sealed partial class SharedInventoryService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<InventoryStorageModule, InventoryStorageModule>();
        services.AddScoped<InventoryQueryModule, InventoryQueryModule>();
    }
}

public sealed partial class SharedStatePublisherService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<InventoryStateModule, InventoryStateModule>();
    }
}

public sealed partial class SharedStateReaderService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<InventoryHudModule, InventoryHudModule>();
    }
}

public sealed partial class InventoryStorageModule : ILayerContext
{
    [Provide(typeof(SharedInventoryService), "items")]
    private readonly List<string> _items = new();

    public void Add(string item)
    {
        _items.Add(item);
    }
}

public sealed partial class InventoryQueryModule : ILayerContext
{
    [From(typeof(SharedInventoryService), "items")]
    private readonly IReadOnlyList<string> _items = default!;

    public int Count()
    {
        return _items.Count;
    }
}

public sealed partial class InventoryStateModule : ILayerContext
{
    [Provide(typeof(SharedFieldLayer), "equip-state")]
    private readonly Dictionary<string, bool> _equipped = new();

    public void SetEquipped(string itemName, bool equipped)
    {
        _equipped[itemName] = equipped;
    }
}

public sealed partial class InventoryHudModule : ILayerContext
{
    [From(typeof(SharedFieldLayer), "equip-state")]
    private readonly IReadOnlyDictionary<string, bool> _equipped = default!;

    public bool IsEquipped(string itemName)
    {
        return _equipped.TryGetValue(itemName, out var equipped) && equipped;
    }
}

public class SharedFieldLayer : Layer
{
}

public static class SharedFieldUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Shared Field Usage ---");
        LayerHub.Reset();

        var layer = new SharedFieldLayer();
        layer.RegisterService(new SharedInventoryService());
        layer.RegisterService(new SharedStatePublisherService());
        layer.RegisterService(new SharedStateReaderService());
        LayerHub.CreateLayers().Push(layer).Build();

        var storage = layer.GetService<InventoryStorageModule>();
        var query = layer.GetService<InventoryQueryModule>();
        storage.Add("Sword");
        storage.Add("Shield");

        var state = layer.GetService<InventoryStateModule>();
        var hud = layer.GetService<InventoryHudModule>();
        state.SetEquipped("Sword", true);

        Console.WriteLine($"[Service Scope] Item count: {query.Count()}");
        Console.WriteLine($"[Layer Scope] Sword equipped: {hud.IsEquipped("Sword")}");
    }
}