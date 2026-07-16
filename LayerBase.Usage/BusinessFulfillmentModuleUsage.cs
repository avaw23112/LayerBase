using LayerBase.Async;
using LayerBase.Call;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace LayerBase.Usage;

[OwnerLayer(typeof(BusinessCommerceLayer))]
public sealed class BusinessQuoteShipmentHandler
    : IScopeLocalCallHandler<BusinessQuoteShipmentRequest, BusinessQuoteShipmentResponse>
{
    public LBTask<BusinessQuoteShipmentResponse> HandleAsync(
        BusinessQuoteShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var price = request.City.Equals("Shanghai", StringComparison.OrdinalIgnoreCase) ? 8.50m : 12.00m;
        return LBTask<BusinessQuoteShipmentResponse>.FromResult(
            new BusinessQuoteShipmentResponse(request.OrderId, "LayerExpress", price));
    }
}

public sealed class BusinessFulfillmentReporter : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }
}

public sealed class BusinessFulfillmentModule : IAssemblyModule
{
    private static readonly AssemblyModuleId s_id = new("business-fulfillment");

    public AssemblyModuleId Id => s_id;

    public AssemblyModuleManifest Manifest { get; } = new(
        s_id,
        new[]
        {
            ServiceContribution.ForTypes(
                typeof(BusinessInventoryLedger),
                typeof(BusinessInventoryLedger),
                typeof(BusinessCommerceLayer),
                typeof(BusinessInventoryScope),
                ServiceLifetime.Singleton),
            ServiceContribution.ForTypes(
                typeof(BusinessFulfillmentReporter),
                typeof(BusinessFulfillmentReporter),
                typeof(BusinessCommerceLayer),
                typeof(MainScope),
                ServiceLifetime.Singleton)
        },
        Array.Empty<ContextContribution>(),
        new[]
        {
            LocalCallContribution.ForTypes(
                typeof(BusinessReserveInventoryRequest),
                typeof(BusinessReserveInventoryResponse),
                typeof(BusinessReserveInventoryHandler),
                typeof(BusinessCommerceLayer),
                typeof(BusinessInventoryScope)),
            LocalCallContribution.ForTypes(
                typeof(BusinessQuoteShipmentRequest),
                typeof(BusinessQuoteShipmentResponse),
                typeof(BusinessQuoteShipmentHandler),
                typeof(BusinessCommerceLayer),
                typeof(MainScope))
        },
        Array.Empty<EventHandlerContribution>(),
        new[]
        {
            LayerToolContribution.ForTypes(
                typeof(IBusinessShippingLabelTool),
                typeof(BusinessShippingLabelTool),
                "default",
                typeof(BusinessCommerceLayer))
        });
}

public sealed class BusinessShippingLabelTool : IBusinessShippingLabelTool
{
    public string CreateLabel(string orderId, string carrier)
    {
        return $"{carrier}-{orderId}";
    }
}
