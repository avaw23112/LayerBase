using LayerBase.Async;
using LayerBase.Call;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Tools;

namespace LayerBase.Usage;

[OwnerLayer(typeof(BusinessCommerceLayer))]
public sealed partial class BusinessFulfillmentReporter : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [Call]
    public async LBTask<BusinessQuoteShipmentResponse> QuoteShipmentAsync(
        BusinessQuoteShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await LBTask.CompletedTask;
        var price = request.City.Equals("Shanghai", StringComparison.OrdinalIgnoreCase) ? 8.50m : 12.00m;
        return new BusinessQuoteShipmentResponse(request.OrderId, "LayerExpress", price);
    }
}

[AssemblyModule("business-fulfillment")]
public sealed partial class BusinessFulfillmentModule
{
}

[LayerTool(typeof(BusinessCommerceLayer), typeof(IBusinessShippingLabelTool))]
public sealed class BusinessShippingLabelTool : IBusinessShippingLabelTool
{
    public string CreateLabel(string orderId, string carrier)
    {
        return $"{carrier}-{orderId}";
    }
}
