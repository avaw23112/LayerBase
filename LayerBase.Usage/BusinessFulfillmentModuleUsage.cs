using LayerBase.Async;
using LayerBase.Call;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;
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

[LayerTool("business.shipping-label", Contract = typeof(IBusinessShippingLabelTool))]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BusinessShippingLabelToolAttribute : Attribute
{
    public BusinessShippingLabelToolAttribute(Type layer, Type ownerScope)
    {
        Layer = layer;
        OwnerScope = ownerScope;
    }

    public Type Layer { get; }

    public Type OwnerScope { get; }

    public string Key { get; set; } = "default";

    public bool Cache { get; set; } = true;
}

[BusinessShippingLabelTool(typeof(BusinessCommerceLayer), typeof(MainScope))]
public sealed class BusinessShippingLabelTool : IBusinessShippingLabelTool
{
    public string CreateLabel(string orderId, string carrier)
    {
        return $"{carrier}-{orderId}";
    }
}
