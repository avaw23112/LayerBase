using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeAttributeApiMigrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Subscribe_attribute_on_scoped_service_receives_owner_scope_post()
    {
        var layer = new AttributeApiLayer();
        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .AddAssemblyModule(new AttributeApiModule())
                                    .Build();

        runtime.GetScope<AttributeApiScope>().Post(new AttributeApiStockArrived("sku-1", 5));
        runtime.Pump(0.016f);

        var service = layer.GetService<AttributeApiInventoryService>();
        Assert.That(service, Is.InstanceOf<IAutoSubscribe>());
        Assert.That(ServiceLayerBinder.GetBinding(service)?.OwnerScope.ScopeId, Is.EqualTo(AttributeApiScope.ScopeId));
        Assert.That(service.Available, Is.EqualTo(5));
        Assert.That(service.LastSku, Is.EqualTo("sku-1"));
    }

    [Test]
    public async Task Call_attribute_on_scoped_service_registers_owner_scope_route()
    {
        var layer = new AttributeApiLayer();
        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .AddAssemblyModule(new AttributeApiModule())
                                    .Build();

        Assert.That(layer.GetService<AttributeApiInventoryService>(), Is.InstanceOf<IAutoCallBinder>());
        Assert.That(layer.LocalCallRouteEntries.Any(static entry =>
            entry.OwnerScopeId == AttributeApiScope.ScopeId &&
            entry.RequestType == typeof(AttributeApiReserveStock) &&
            entry.ResponseType == typeof(AttributeApiReserveStockResult)), Is.True,
            string.Join(", ", layer.LocalCallRouteEntries.Select(static entry =>
                $"{entry.OwnerScopeId}:{entry.RequestType.Name}->{entry.ResponseType.Name}:{entry.HandlerType.Name}")));

        var task = runtime.GetScope<AttributeApiScope>()
                          .Call<AttributeApiReserveStock, AttributeApiReserveStockResult>(
                              new AttributeApiReserveStock("sku-2", 3));
        runtime.Pump(0.016f);
        var response = await task;

        Assert.That(response.Accepted, Is.True);
        Assert.That(response.ReservedSku, Is.EqualTo("sku-2"));
        Assert.That(layer.GetService<AttributeApiInventoryService>().Reserved, Is.EqualTo(3));
    }
}

public readonly struct AttributeApiScope : IScopeDefinition
{
    public const int ScopeId = 61;
}

public sealed partial class AttributeApiLayer : Layer
{
}

public sealed class AttributeApiModule : IAssemblyModule
{
    private static readonly AssemblyModuleId s_id = new("attribute-api-scope-migration");

    public AttributeApiModule()
    {
        Manifest = new AssemblyModuleManifest(
            s_id,
            new[]
                {
                    ServiceContribution.ForTypes(
                        typeof(AttributeApiScopeActivator),
                        typeof(AttributeApiScopeActivator),
                        typeof(AttributeApiLayer),
                        typeof(AttributeApiScope),
                        ServiceLifetime.Singleton)
            },
            Array.Empty<ContextContribution>(),
            Array.Empty<LocalCallContribution>(),
            Array.Empty<EventHandlerContribution>(),
            Array.Empty<LayerToolContribution>());
    }

    public AssemblyModuleId Id => s_id;

    public AssemblyModuleManifest Manifest { get; }
}

public sealed class AttributeApiScopeActivator : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}

public readonly struct AttributeApiStockArrived
{
    public AttributeApiStockArrived(string sku, int quantity)
    {
        Sku = sku;
        Quantity = quantity;
    }

    public string Sku { get; }

    public int Quantity { get; }
}

public readonly struct AttributeApiReserveStock
{
    public AttributeApiReserveStock(string sku, int quantity)
    {
        Sku = sku;
        Quantity = quantity;
    }

    public string Sku { get; }

    public int Quantity { get; }
}

public readonly struct AttributeApiReserveStockResult
{
    public AttributeApiReserveStockResult(bool accepted, string reservedSku)
    {
        Accepted = accepted;
        ReservedSku = reservedSku;
    }

    public bool Accepted { get; }

    public string ReservedSku { get; }
}

[Scope<AttributeApiScope>]
[OwnerLayer(typeof(AttributeApiLayer))]
public sealed partial class AttributeApiInventoryService : IService
{
    public int Available { get; private set; }

    public int Reserved { get; private set; }

    public string LastSku { get; private set; } = string.Empty;

    public void ConfigureServices(IServiceCollection services)
    {
    }

    [Subscribe]
    public void OnStockArrived(in AttributeApiStockArrived value)
    {
        LastSku = value.Sku;
        Available += value.Quantity;
    }

    [Call]
    public LBTask<AttributeApiReserveStockResult> ReserveAsync(
        AttributeApiReserveStock request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Reserved += request.Quantity;
        return LBTask<AttributeApiReserveStockResult>.FromResult(
            new AttributeApiReserveStockResult(true, request.Sku));
    }
}
