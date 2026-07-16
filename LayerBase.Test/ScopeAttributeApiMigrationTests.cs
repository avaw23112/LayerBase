using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.DI.Options;
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
    public void SubscribeScopeEvent_attribute_on_scoped_service_receives_owner_scope_post()
    {
        var layer = new AttributeApiLayer();
        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .AddAssemblyModule(new AttributeApiModule())
                                    .Build();

        runtime.GetScope<AttributeApiScope>().Post(new AttributeApiStockArrived("sku-1", 5));
        runtime.Pump(0.016f);

        var service = layer.GetService<AttributeApiInventoryService>();
        Assert.That(service, Is.InstanceOf<IAutoScopeEndpointBinder>());
        Assert.That(ServiceLayerBinder.GetBinding(service)?.OwnerScope.ScopeId, Is.EqualTo(AttributeApiScope.ScopeId));
        Assert.That(service.Available, Is.EqualTo(5));
        Assert.That(service.LastSku, Is.EqualTo("sku-1"));
    }

    [Test]
    public async Task SubscribeScopeCall_attribute_on_scoped_service_registers_owner_scope_route()
    {
        var layer = new AttributeApiLayer();
        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .AddAssemblyModule(new AttributeApiModule())
                                    .Build();

        Assert.That(layer.GetService<AttributeApiInventoryService>(), Is.InstanceOf<IAutoScopeEndpointBinder>());
        Assert.That(layer.ScopeCallRouteEntries.Any(static entry =>
            entry.OwnerScopeId == AttributeApiScope.ScopeId &&
            entry.RequestType == typeof(AttributeApiReserveStock) &&
            entry.ResponseType == typeof(AttributeApiReserveStockResult)), Is.True,
            string.Join(", ", layer.ScopeCallRouteEntries.Select(static entry =>
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

    [Test]
    public void ScopeRef_call_does_not_route_to_local_Call_attribute()
    {
        var layer = new AttributeApiLayer();
        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .AddAssemblyModule(new AttributeApiModule())
                                    .Build();

        Assert.That(layer.LocalCallRouteEntries.Any(static entry =>
            entry.OwnerScopeId == AttributeApiScope.ScopeId &&
            entry.RequestType == typeof(AttributeApiLocalPriceQuery) &&
            entry.ResponseType == typeof(AttributeApiLocalPriceQuote)), Is.True);

        var task = runtime.GetScope<AttributeApiScope>()
                          .Call<AttributeApiLocalPriceQuery, AttributeApiLocalPriceQuote>(
                              new AttributeApiLocalPriceQuery("sku-3"));
        runtime.Pump(0.016f);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
        Assert.That(exception!.Message, Does.Contain("ScopeCall handler"));
    }

    [Test]
    public async Task Scope_endpoint_attributes_on_layer_context_are_auto_bound()
    {
        var layer = new AttributeApiContextLayer();
        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .AddAssemblyModule(new AttributeApiContextModule())
                                    .Build();

        runtime.GetScope<AttributeApiContextScope>().Post(new AttributeApiContextStockArrived("sku-context", 8));
        runtime.Pump(0.016f);

        var context = layer.GetService<AttributeApiContextService>().Context;
        Assert.That(context, Is.InstanceOf<IAutoScopeEndpointBinder>());
        Assert.That(context.LastSku, Is.EqualTo("sku-context"));
        Assert.That(context.Available, Is.EqualTo(8));

        var reservedTask = runtime.GetScope<AttributeApiContextScope>()
                                  .Call<AttributeApiContextReserveStock, AttributeApiContextReserveStockResult>(
                                      new AttributeApiContextReserveStock("sku-context", 2));
        runtime.Pump(0.016f);

        var reserved = await reservedTask;
        Assert.That(reserved.Accepted, Is.True);
        Assert.That(context.Reserved, Is.EqualTo(2));

        var quote = await context.Call<AttributeApiContextLocalPriceQuery, AttributeApiContextLocalPriceQuote>(
            new AttributeApiContextLocalPriceQuery("sku-context"));
        Assert.That(quote.Price, Is.EqualTo("sku-context".Length));
    }
}

public readonly struct AttributeApiScope : IScopeDefinition
{
    public const int ScopeId = 61;
}

public sealed partial class AttributeApiLayer : Layer
{
}

public readonly struct AttributeApiContextScope : IScopeDefinition
{
    public const int ScopeId = 62;
}

public sealed partial class AttributeApiContextLayer : Layer
{
}

public sealed class AttributeApiContextModule : IAssemblyModule
{
    private static readonly AssemblyModuleId s_id = new("attribute-api-context-endpoints");

    public AttributeApiContextModule()
    {
        Manifest = new AssemblyModuleManifest(
            s_id,
            new[]
            {
                ServiceContribution.ForTypes(
                    typeof(AttributeApiContextService),
                    typeof(AttributeApiContextService),
                    typeof(AttributeApiContextLayer),
                    typeof(AttributeApiContextScope),
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

[OwnerLayer(typeof(AttributeApiContextLayer))]
[Scope<AttributeApiContextScope>]
public sealed partial class AttributeApiContextService : IService
{
    [Mount] private AttributeApiInventoryContext _context = null!;

    public AttributeApiInventoryContext Context => _context;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }
}

public readonly struct AttributeApiContextStockArrived
{
    public AttributeApiContextStockArrived(string sku, int quantity)
    {
        Sku = sku;
        Quantity = quantity;
    }

    public string Sku { get; }

    public int Quantity { get; }
}

public readonly struct AttributeApiContextReserveStock
{
    public AttributeApiContextReserveStock(string sku, int quantity)
    {
        Sku = sku;
        Quantity = quantity;
    }

    public string Sku { get; }

    public int Quantity { get; }
}

public readonly struct AttributeApiContextReserveStockResult
{
    public AttributeApiContextReserveStockResult(bool accepted, string reservedSku)
    {
        Accepted = accepted;
        ReservedSku = reservedSku;
    }

    public bool Accepted { get; }

    public string ReservedSku { get; }
}

public readonly struct AttributeApiContextLocalPriceQuery
{
    public AttributeApiContextLocalPriceQuery(string sku)
    {
        Sku = sku;
    }

    public string Sku { get; }
}

public readonly struct AttributeApiContextLocalPriceQuote
{
    public AttributeApiContextLocalPriceQuote(decimal price)
    {
        Price = price;
    }

    public decimal Price { get; }
}

[OwnerService(typeof(AttributeApiContextService))]
public sealed partial class AttributeApiInventoryContext : ILayerContext
{
    public int Available { get; private set; }

    public int Reserved { get; private set; }

    public string LastSku { get; private set; } = string.Empty;

    [SubscribeScopeEvent]
    public void OnStockArrived(in AttributeApiContextStockArrived value)
    {
        LastSku = value.Sku;
        Available += value.Quantity;
    }

    [SubscribeScopeCall]
    public async LBTask<AttributeApiContextReserveStockResult> ReserveAsync(
        AttributeApiContextReserveStock request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await LBTask.CompletedTask;
        Reserved += request.Quantity;
        return new AttributeApiContextReserveStockResult(true, request.Sku);
    }

    [Call]
    public async LBTask<AttributeApiContextLocalPriceQuote> QuoteLocalPriceAsync(
        AttributeApiContextLocalPriceQuery request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await LBTask.CompletedTask;
        return new AttributeApiContextLocalPriceQuote(request.Sku.Length);
    }
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

public readonly struct AttributeApiLocalPriceQuery
{
    public AttributeApiLocalPriceQuery(string sku)
    {
        Sku = sku;
    }

    public string Sku { get; }
}

public readonly struct AttributeApiLocalPriceQuote
{
    public AttributeApiLocalPriceQuote(decimal price)
    {
        Price = price;
    }

    public decimal Price { get; }
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

    [SubscribeScopeEvent]
    public void OnStockArrived(in AttributeApiStockArrived value)
    {
        LastSku = value.Sku;
        Available += value.Quantity;
    }

    [SubscribeScopeCall]
    public async LBTask<AttributeApiReserveStockResult> ReserveAsync(
        AttributeApiReserveStock request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await LBTask.CompletedTask;
        Reserved += request.Quantity;
        return new AttributeApiReserveStockResult(true, request.Sku);
    }

    [Call]
    public async LBTask<AttributeApiLocalPriceQuote> QuoteLocalPriceAsync(
        AttributeApiLocalPriceQuery request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await LBTask.CompletedTask;
        return new AttributeApiLocalPriceQuote(request.Sku.Length);
    }
}
