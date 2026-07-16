using LayerBase.Async;
using LayerBase.Call;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace LayerBase.Usage;

[OwnerLayer(typeof(BusinessCommerceLayer))]
public sealed partial class BusinessFulfillmentReporter : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [Call]
    public LBTask<BusinessQuoteShipmentResponse> QuoteShipmentAsync(
        BusinessQuoteShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var price = request.City.Equals("Shanghai", StringComparison.OrdinalIgnoreCase) ? 8.50m : 12.00m;
        return LBTask<BusinessQuoteShipmentResponse>.FromResult(
            new BusinessQuoteShipmentResponse(request.OrderId, "LayerExpress", price));
    }
}

public sealed class BusinessInventoryScopeActivator : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
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
                typeof(BusinessInventoryScopeActivator),
                typeof(BusinessInventoryScopeActivator),
                typeof(BusinessCommerceLayer),
                typeof(BusinessInventoryScope),
                ServiceLifetime.Singleton)
        },
        Array.Empty<ContextContribution>(),
        Array.Empty<LocalCallContribution>(),
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
