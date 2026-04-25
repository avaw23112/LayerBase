using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class SharedFieldBindingTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Service_scope_Provide_and_Use_share_the_same_list()
    {
        var layer = new Layer_A();
        layer.RegisterService(new ServiceScopeSharingService());
        LayerHub.CreateLayers().Push(layer).Build();

        var storage = layer.GetService<PlayerStorageModule>();
        var query = layer.GetService<PlayerQueryModule>();

        storage.Add(7);
        storage.Add(9);

        Assert.That(query.Count(), Is.EqualTo(2));
        Assert.That(query.Contains(9), Is.True);
    }

    [Test]
    public void Layer_scope_Provide_and_Use_share_state_across_services()
    {
        var layer = new Layer_A();
        layer.RegisterService(new PlayerStateService());
        layer.RegisterService(new PlayerHudService());
        LayerHub.CreateLayers().Push(layer).Build();

        var state = layer.GetService<PlayerStateModule>();
        var hud = layer.GetService<PlayerHudModule>();

        state.SetOnline(42, true);

        Assert.That(hud.IsOnline(42), Is.True);
    }

    [Test]
    public void Global_scope_Provide_and_Use_share_reference_across_layers()
    {
        var layerA = new Layer_A();
        layerA.RegisterService(new GlobalPublisherService());
        var layerB = new Layer_B();
        layerB.RegisterService(new GlobalConsumerService());

        LayerHub.CreateLayers().Push(layerA).Push(layerB).Build();

        var publisher = layerA.GetService<SharedReferencePublisherModule>();
        var consumer = layerB.GetService<SharedReferenceConsumerModule>();

        publisher.SetValue("ready");

        Assert.That(consumer.ReadValue(), Is.EqualTo("ready"));
    }

    [Test]
    public void Duplicate_publishers_in_the_same_scope_fail_build()
    {
        var layer = new Layer_A();
        layer.RegisterService(new DuplicatePublisherServiceA());
        layer.RegisterService(new DuplicatePublisherServiceB());

        Assert.That(
            () => LayerHub.CreateLayers().Push(layer).Build(),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Shared field provider conflict"));
    }

    [Test]
    public void Missing_publisher_fails_build()
    {
        var layer = new Layer_A();
        layer.RegisterService(new MissingPublisherConsumerService());

        Assert.That(
            () => LayerHub.CreateLayers().Push(layer).Build(),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("could not find a provider"));
    }

    [Test]
    public void Writable_container_consumer_is_rejected()
    {
        var layer = new Layer_A();
        layer.RegisterService(new WritableConsumerService());

        Assert.That(
            () => LayerHub.CreateLayers().Push(layer).Build(),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Only read-only projections are allowed"));
    }
}

public class Layer_A : Layer { }
public class Layer_B : Layer { }

public class SharedFieldLayer : Layer
{
}

public sealed class ServiceScopeSharingService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<PlayerStorageModule, PlayerStorageModule>();
        services.AddScoped<PlayerQueryModule, PlayerQueryModule>();
    }
}

public sealed class PlayerStateService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<PlayerStateModule, PlayerStateModule>();
    }
}

public sealed class PlayerHudService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<PlayerHudModule, PlayerHudModule>();
    }
}

public sealed class GlobalPublisherService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<SharedReferencePublisherModule, SharedReferencePublisherModule>();
    }
}

public sealed class GlobalConsumerService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<SharedReferenceConsumerModule, SharedReferenceConsumerModule>();
    }
}

public sealed class DuplicatePublisherServiceA : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<DuplicateLayerPublisherModuleA, DuplicateLayerPublisherModuleA>();
    }
}

public sealed class DuplicatePublisherServiceB : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<DuplicateLayerPublisherModuleB, DuplicateLayerPublisherModuleB>();
    }
}

public sealed class MissingPublisherConsumerService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<MissingPublisherConsumerModule, MissingPublisherConsumerModule>();
    }
}

public sealed class WritableConsumerService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<PlayerStorageModule, PlayerStorageModule>();
        services.AddScoped<WritableListConsumerModule, WritableListConsumerModule>();
    }
}

public sealed class PlayerStorageModule : ILayerContext
{
    [Provide(typeof(ServiceScopeSharingService), "players")]
    private readonly List<int> _players = new();

    public void Add(int playerId)
    {
        _players.Add(playerId);
    }
}

public sealed class PlayerQueryModule : ILayerContext
{
    [Use(typeof(ServiceScopeSharingService), "players")]
    private readonly IReadOnlyList<int> _players = default!;

    public int Count()
    {
        return _players.Count;
    }

    public bool Contains(int playerId)
    {
        for (var i = 0; i < _players.Count; i++)
            if (_players[i] == playerId)
                return true;

        return false;
    }
}

public sealed class PlayerStateModule : ILayerContext
{
    [Provide(typeof(Layer_A), "player_states")]
    private readonly Dictionary<int, bool> _states = new();

    public void SetOnline(int playerId, bool isOnline)
    {
        _states[playerId] = isOnline;
    }
}

public sealed class PlayerHudModule : ILayerContext
{
    [Use(typeof(Layer_A), "player_states")]
    private readonly IReadOnlyDictionary<int, bool> _states = default!;

    public bool IsOnline(int playerId)
    {
        return _states.TryGetValue(playerId, out var isOnline) && isOnline;
    }
}

public sealed class SharedReferencePublisherModule : ILayerContext
{
    [Provide(typeof(GlobalScope), "shared-ref")]
    private readonly SharedReferenceBox _box = new();

    public void SetValue(string value)
    {
        _box.Value = value;
    }
}

public sealed class SharedReferenceConsumerModule : ILayerContext
{
    [Use(typeof(GlobalScope), "shared-ref")]
    private readonly SharedReferenceBox _box = default!;

    public string ReadValue()
    {
        return _box.Value;
    }
}

public sealed class DuplicateLayerPublisherModuleA : ILayerContext
{
    [Provide(typeof(Layer_A), "duplicate-layer-key")]
    private Dictionary<int, int> _state = new();
}

public sealed class DuplicateLayerPublisherModuleB : ILayerContext
{
    [Provide(typeof(Layer_A), "duplicate-layer-key")]
    private Dictionary<int, int> _state = new();
}

public sealed class MissingPublisherConsumerModule : ILayerContext
{
    [Use(typeof(Layer_A), "missing-layer-key")]
    private IReadOnlyDictionary<int, int> _state = default!;
}

public sealed class WritableListConsumerModule : ILayerContext
{
    [Use(typeof(ServiceScopeSharingService), "players")]
    private List<int> _players = default!;
}

public sealed class SharedReferenceBox
{
    public string Value { get; set; } = string.Empty;
}
