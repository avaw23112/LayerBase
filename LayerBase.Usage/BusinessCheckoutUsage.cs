using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Event.Delay;
using LayerBase.Layers;
using LayerBase.Scope;
using LayerBase.Tools;
using LayerBase.Worker;

namespace LayerBase.Usage;

public static class BusinessScenarioUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Retail Order Checkout Scenario ---");
        LayerHub.Reset();

        var commerceLayer = new BusinessCommerceLayer();
        var checkout = new BusinessCheckoutService();
        commerceLayer.RegisterService(checkout);

        using var runtime = LayerHub.CreateLayers()
                                    .Push(commerceLayer)
                                    .AddAssemblyModule(BusinessFulfillmentModule.Instance)
                                    .SetDebug()
                                    .Build();

        var inventoryScope = runtime.GetScope<BusinessInventoryScope>();
        var orderId = "ORDER-1001";
        var sku = "SKU-COFFEE";

        var receivingNotice = inventoryScope.Post(new BusinessInventoryRestockedEvent(sku, quantity: 12));
        PumpFor(runtime, commerceLayer, maxFrames: 3);
        Console.WriteLine($"[Warehouse] Receiving notice accepted: {receivingNotice.IsAccepted}");

        var reservation = PumpUntilCompleted(
            runtime,
            inventoryScope.Call<BusinessReserveInventoryRequest, BusinessReserveInventoryResponse>(
                new BusinessReserveInventoryRequest(orderId, sku, quantity: 3)));
        Console.WriteLine($"[Warehouse] Reserved {reservation.Quantity} units, remaining {reservation.Remaining}");

        var shippingQuote = runtime.CallAsync<BusinessQuoteShipmentRequest, BusinessQuoteShipmentResponse>(
                                     new BusinessQuoteShipmentRequest(orderId, "Shanghai"))
                                 .GetAwaiter()
                                 .GetResult();
        Console.WriteLine($"[Fulfillment] Shipping quote: {shippingQuote.Carrier} {shippingQuote.Price:C}");

        var labelTool = runtime.Tools.GetOrCreate<IBusinessShippingLabelTool>();
        Console.WriteLine($"[Fulfillment] Shipping label: {labelTool.CreateLabel(orderId, shippingQuote.Carrier)}");

        checkout.PlaceOrder(orderId, sku, reservation.Quantity, shippingQuote.Price);
        PumpFor(runtime, commerceLayer, maxFrames: 20);

        var delayedInvoice = commerceLayer.DelayedInvoices.TryGet(out var invoice);
        Console.WriteLine($"[Billing] Invoice ready in delay buffer: {delayedInvoice}, order: {invoice.OrderId}");

        runtime.Pump(0.10f);
        Console.WriteLine($"[Billing] Payment reminder sent: {commerceLayer.ReminderCount > 0}");

        var snapJson = runtime.FullSnap.SerializeJson();
        var diagnostics = runtime.CaptureDiagnostics();
        Console.WriteLine(
            $"[Operations] Snapshot bytes={snapJson.Length}, scopes={diagnostics.Scopes.Length}, tools={diagnostics.Scopes[0].Tools.RegisteredCount}");

        Console.WriteLine(
            $"[Checkout] Orders={commerceLayer.AcceptedOrders}, riskReports={commerceLayer.FraudScores}, shipments={commerceLayer.Shipments}");
    }

    private static T PumpUntilCompleted<T>(LayerRuntime runtime, LBTask<T> task)
    {
        var awaiter = task.GetAwaiter();
        for (var i = 0; i < 120 && !awaiter.IsCompleted; i++)
        {
            runtime.Pump(0.016f);
            Thread.Sleep(1);
        }

        return awaiter.GetResult();
    }

    private static void PumpFor(LayerRuntime runtime, BusinessCommerceLayer layer, int maxFrames)
    {
        for (var i = 0; i < maxFrames; i++)
        {
            runtime.Pump(0.016f);
            if (layer.FraudScores > 0 && layer.Shipments > 0)
                break;

            Thread.Sleep(5);
        }
    }
}

public sealed partial class BusinessCommerceLayer : Layer
{
    [SubscribeDelay]
    public IDelayPublisher<BusinessInvoiceAvailableEvent> DelayedInvoices { get; set; } = default!;

    public int AcceptedOrders { get; private set; }

    public int FraudScores { get; private set; }

    public int Shipments { get; private set; }

    public int ReminderCount { get; private set; }

    [Subscribe]
    public void OnOrderAccepted(in BusinessOrderAcceptedEvent value)
    {
        AcceptedOrders++;
        Console.WriteLine($"[Checkout] Order accepted: {value.OrderId}, total: {value.Total:C}");
    }

    [Subscribe]
    public void OnOrderProjection(in BusinessOrderProjectionEvent value)
    {
        Console.WriteLine($"[Checkout] Order projection: {value.OrderId} -> {value.State}");
    }

    [Subscribe]
    public void OnFraudScoreCalculated(in BusinessFraudScoreCalculatedEvent value)
    {
        FraudScores++;
        Console.WriteLine($"[Risk] Fraud score: {value.OrderId} -> {value.Score}");
    }

    [Subscribe]
    public void OnShipmentQueued(in BusinessShipmentQueuedEvent value)
    {
        Shipments++;
        Console.WriteLine($"[Fulfillment] Shipment queued: {value.OrderId}, label: {value.Label}");
    }

    [Subscribe]
    public void OnBillingReminder(in BusinessBillingReminderEvent value)
    {
        ReminderCount++;
        Console.WriteLine($"[Billing] Payment reminder scheduled for {value.OrderId}");
    }

    [Subscribe]
    public void OnWorkerJobFailed(in WorkerJobFailedEvent value)
    {
        Console.WriteLine($"[Risk] Worker job failed: {value.Kind} {value.Error.Message}");
    }
}

public sealed partial class BusinessCheckoutService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
        services.AddScoped<BusinessCartContext, BusinessCartContext>();
        services.AddScoped<BusinessOrderProjectionContext, BusinessOrderProjectionContext>();
    }

    public WorkerHandle PlaceOrder(string orderId, string sku, int quantity, decimal shippingPrice)
    {
        var cart = this.GetService<BusinessCartContext>();
        cart.Remember(orderId, sku, quantity);
        var projection = this.GetService<BusinessOrderProjectionContext>();

        this.Post(new BusinessOrderAcceptedEvent(orderId, sku, quantity, total: quantity * 18m + shippingPrice));
        this.Post(new BusinessOrderProjectionEvent(orderId, projection.Describe(orderId)));
        this.SchedulePost(new BusinessBillingReminderEvent(orderId), delaySeconds: 0.05f);
        this.Delay(new BusinessInvoiceAvailableEvent(orderId), ttl: 1.0f);

        var label = this.Tools()
                        .GetOrCreate<IBusinessShippingLabelTool>()
                        .CreateLabel(orderId, carrier: "LayerExpress");
        this.Post(new BusinessShipmentQueuedEvent(orderId, label));

        return this.WorkerJobs().Run<BusinessFraudScoreJob, BusinessFraudScoreInput, BusinessFraudScoreCalculatedEvent>(
            new BusinessFraudScoreJob(),
            new BusinessFraudScoreInput(orderId, quantity));
    }
}

public sealed partial class BusinessCartContext : ILayerContext
{
    [Provide("cart-lines")]
    private readonly Dictionary<string, string> _lines = new();

    public void Remember(string orderId, string sku, int quantity)
    {
        _lines[orderId] = $"{sku} x{quantity}";
    }
}

public sealed partial class BusinessOrderProjectionContext : ILayerContext
{
    [From(typeof(BusinessCheckoutService), "cart-lines")]
    private readonly IReadOnlyDictionary<string, string> _lines = default!;

    public string Describe(string orderId)
    {
        return _lines.TryGetValue(orderId, out var line)
            ? $"accepted with cart line {line}"
            : "accepted without cart line";
    }
}

public readonly struct BusinessFraudScoreJob
    : IWorkerEventJob<BusinessFraudScoreInput, BusinessFraudScoreCalculatedEvent>
{
    public BusinessFraudScoreCalculatedEvent Execute(
        in BusinessFraudScoreInput input,
        in WorkerJobContext context)
    {
        var score = Math.Min(99, 20 + input.Quantity * 7);
        return new BusinessFraudScoreCalculatedEvent(input.OrderId, score, context.ExecutionLaneId);
    }
}
