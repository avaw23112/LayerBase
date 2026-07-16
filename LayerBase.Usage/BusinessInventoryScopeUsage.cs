using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Usage;

[OwnerLayer(typeof(BusinessCommerceLayer))]
[Scope<BusinessInventoryScope>]
public sealed partial class BusinessInventoryLedger : IService
{
    private readonly Dictionary<string, int> _stock = new(StringComparer.Ordinal);

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [Call]
    public async LBTask<BusinessReserveInventoryResponse> ReserveAsync(
        BusinessReserveInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await LBTask.CompletedTask;
        return Reserve(request.OrderId, request.Sku, request.Quantity);
    }

    private BusinessReserveInventoryResponse Reserve(string orderId, string sku, int quantity)
    {
        _stock.TryGetValue(sku, out var current);
        if (current < quantity)
        {
            return new BusinessReserveInventoryResponse(orderId, sku, accepted: false, quantity: 0, remaining: current);
        }

        var remaining = current - quantity;
        _stock[sku] = remaining;
        return new BusinessReserveInventoryResponse(orderId, sku, accepted: true, quantity: quantity, remaining: remaining);
    }

    [Subscribe]
    public void OnInventoryRestocked(in BusinessInventoryRestockedEvent value)
    {
        _stock.TryGetValue(value.Sku, out var current);
        _stock[value.Sku] = current + value.Quantity;
        Console.WriteLine($"[Warehouse] Stock received: {value.Sku} +{value.Quantity}");
    }
}
