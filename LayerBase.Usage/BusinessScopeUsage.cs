using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;
using LayerBase.Tools;

namespace LayerBase.Usage;

public static class BusinessScopeUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Scope Business Scenarios ---");
        LayerHub.Reset();

        var layer = new BusinessScopeDemoLayer();
        var payment = new BusinessPaymentService();
        layer.RegisterService(payment);

        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .SetDebug()
                                    .Build();

        Console.WriteLine($"[Scopes] Runtime built, {runtime.CaptureDiagnostics().Scopes.Length} scopes");

        // 1. Get ScopeRef for each scope type
        var inventoryScope = runtime.GetScope<BusinessInventoryScope>();
        var paymentScope = runtime.GetScope<BusinessPaymentScope>();
        Console.WriteLine($"[Scopes] Inventory(id={inventoryScope.Address.ScopeId}), Payment(id={paymentScope.Address.ScopeId})");

        // 2. Post to payment scope (Inline) — processed on next Pump
        paymentScope.Post(new BusinessPaymentEvent("ORDER-001", 150.00m));
        runtime.Pump(0.016f);
        Console.WriteLine($"[Payment] Processed: {layer.PaymentCount} payment(s)");

        // 3. Worker scope receives Inventory events via post
        inventoryScope.Post(new BusinessInventoryRestockedEvent("SKU-BOX", 50));
        runtime.Pump(0.016f);
        Console.WriteLine($"[Inventory] Restock posted to scope {inventoryScope.Address.ScopeId}");

        // 4. Diagnostics per scope
        var diagnostics = runtime.CaptureDiagnostics();
        foreach (var s in diagnostics.Scopes)
            Console.WriteLine($"  #{s.ScopeId} \"{s.ScopeName}\": thread={s.OwnerThreadId}, inbox={s.EventInboxCount}");
    }
}

[Scope<BusinessInventoryScope>]
[OwnerLayer(typeof(BusinessScopeDemoLayer))]
public sealed partial class BusinessInventoryService : IService
{
    public void ConfigureServices(IServiceCollection services) { }
}

[Scope<BusinessPaymentScope>]
[OwnerLayer(typeof(BusinessScopeDemoLayer))]
public sealed partial class BusinessPaymentService : IService
{
    public void ConfigureServices(IServiceCollection services) => services.AddSingleton(this);
}

public sealed partial class BusinessScopeDemoLayer : Layer
{
    public int PaymentCount { get; private set; }

    [Subscribe]
    public void OnPayment(in BusinessPaymentEvent value)
    {
        PaymentCount++;
        Console.WriteLine($"[Payment] Received: order={value.OrderId}, amount={value.Amount:C}");
    }
}

public readonly struct BusinessPaymentEvent
{
    public BusinessPaymentEvent(string orderId, decimal amount)
    {
        OrderId = orderId;
        Amount = amount;
    }
    public string OrderId { get; }
    public decimal Amount { get; }
}
