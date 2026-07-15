using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public partial class SharedFieldBindingTests
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
    public void Same_layer_from_can_bind_to_another_service_provider()
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
    public void Same_scope_cross_layer_from_fails()
    {
        var publisherLayer = new Layer_A();
        publisherLayer.RegisterService(new CrossLayerPublisherService());
        var consumerLayer = new Layer_B();
        consumerLayer.RegisterService(new CrossLayerConsumerService());

        Assert.That(
            () => LayerHub.CreateLayers().Push(publisherLayer).Push(consumerLayer).Build(),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Cross-layer From is not allowed"));
    }

    [Test]
    public void Same_service_same_key_duplicate_fails()
    {
        var layer = new Layer_A();
        layer.RegisterService(new DuplicatePublisherService());

        Assert.That(
            () => LayerHub.CreateLayers().Push(layer).Build(),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Shared field provider conflict"));
    }

    [Test]
    public void Different_services_can_use_same_key()
    {
        var layer = new Layer_A();
        layer.RegisterService(new DuplicatePublisherServiceA());
        layer.RegisterService(new DuplicatePublisherServiceB());

        Assert.DoesNotThrow(() => LayerHub.CreateLayers().Push(layer).Build());
    }

    [Test]
    public void From_provider_must_be_service_registration()
    {
        var layer = new Layer_A();
        layer.RegisterService(new InvalidProviderConsumerService());

        Assert.That(
            () => LayerHub.CreateLayers().Push(layer).Build(),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Provider service"));
    }

    [Test]
    public void Missing_publisher_fails_build()
    {
        var layer = new Layer_A();
        layer.RegisterService(new PlayerStateService());
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

public class Layer_A : Layer
{
}

public class Layer_B : Layer
{
}

public class SharedFieldLayer : Layer
{
}

public sealed partial class ServiceScopeSharingService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<PlayerStorageModule, PlayerStorageModule>();
        services.AddScoped<PlayerQueryModule, PlayerQueryModule>();
    }
}

public sealed partial class PlayerStateService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<PlayerStateModule, PlayerStateModule>();
    }
}

public sealed partial class PlayerHudService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<PlayerHudModule, PlayerHudModule>();
    }
}

public sealed partial class CrossLayerPublisherService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<SharedReferencePublisherModule, SharedReferencePublisherModule>();
    }
}

public sealed partial class CrossLayerConsumerService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<SharedReferenceConsumerModule, SharedReferenceConsumerModule>();
    }
}

public sealed partial class DuplicatePublisherServiceA : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<DuplicateLayerPublisherModuleA, DuplicateLayerPublisherModuleA>();
    }
}

public sealed partial class DuplicatePublisherServiceB : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<DuplicateLayerPublisherModuleB, DuplicateLayerPublisherModuleB>();
    }
}

public sealed partial class DuplicatePublisherService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<DuplicateLayerPublisherModuleA, DuplicateLayerPublisherModuleA>();
        services.AddScoped<DuplicateLayerPublisherModuleB, DuplicateLayerPublisherModuleB>();
    }
}

public sealed partial class MissingPublisherConsumerService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<MissingPublisherConsumerModule, MissingPublisherConsumerModule>();
    }
}

public sealed partial class InvalidProviderConsumerService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<InvalidProviderConsumerModule, InvalidProviderConsumerModule>();
    }
}

public sealed partial class WritableConsumerService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<PlayerStorageModule, PlayerStorageModule>();
        services.AddScoped<WritableListConsumerModule, WritableListConsumerModule>();
    }
}

public sealed partial class PlayerStorageModule : ILayerContext
{
    [Provide("players")]
    private readonly List<int> _players = new();

    public void Add(int playerId)
    {
        _players.Add(playerId);
    }
}

public sealed partial class PlayerQueryModule : ILayerContext
{
    [From(typeof(ServiceScopeSharingService), "players")]
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

public sealed partial class PlayerStateModule : ILayerContext
{
    [Provide("player_states")]
    private readonly Dictionary<int, bool> _states = new();

    public void SetOnline(int playerId, bool isOnline)
    {
        _states[playerId] = isOnline;
    }
}

public sealed partial class PlayerHudModule : ILayerContext
{
    [From(typeof(PlayerStateService), "player_states")]
    private readonly IReadOnlyDictionary<int, bool> _states = default!;

    public bool IsOnline(int playerId)
    {
        return _states.TryGetValue(playerId, out var isOnline) && isOnline;
    }
}

public sealed partial class SharedReferencePublisherModule : ILayerContext
{
    [Provide("shared-ref")]
    private readonly SharedReferenceBox _box = new();

    public void SetValue(string value)
    {
        _box.Value = value;
    }
}

public sealed partial class SharedReferenceConsumerModule : ILayerContext
{
    [From(typeof(CrossLayerPublisherService), "shared-ref")]
    private readonly SharedReferenceBox _box = default!;

    public string ReadValue()
    {
        return _box.Value;
    }
}

public sealed partial class DuplicateLayerPublisherModuleA : ILayerContext
{
    [Provide("duplicate-layer-key")]
    private Dictionary<int, int> _state = new();
}

public sealed partial class DuplicateLayerPublisherModuleB : ILayerContext
{
    [Provide("duplicate-layer-key")]
    private Dictionary<int, int> _state = new();
}

public sealed partial class MissingPublisherConsumerModule : ILayerContext
{
    [From(typeof(PlayerStateService), "missing-layer-key")]
    private IReadOnlyDictionary<int, int> _state = default!;
}

public sealed partial class InvalidProviderConsumerModule : ILayerContext
{
    [From(typeof(SharedReferenceBox), "missing-layer-key")]
    private IReadOnlyDictionary<int, int> _state = default!;
}

public sealed partial class WritableListConsumerModule : ILayerContext
{
    [From(typeof(WritableConsumerService), "players")]
    private List<int> _players = default!;
}

public sealed class SharedReferenceBox
{
    public string Value { get; set; } = string.Empty;
}
