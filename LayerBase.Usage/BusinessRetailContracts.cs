using LayerBase.Scope;

namespace LayerBase.Usage;

public sealed class BusinessInventoryScope : IScopeDefinition
{
    public ScopeOptions Options => ScopeOptions.Inline;
}

public sealed class BusinessPaymentScope : IScopeDefinition
{
    public ScopeOptions Options => ScopeOptions.Inline;
}

public readonly struct BusinessOrderAcceptedEvent
{
    public BusinessOrderAcceptedEvent(string orderId, string sku, int quantity, decimal total)
    {
        OrderId = orderId;
        Sku = sku;
        Quantity = quantity;
        Total = total;
    }

    public string OrderId { get; }

    public string Sku { get; }

    public int Quantity { get; }

    public decimal Total { get; }
}

public readonly struct BusinessOrderProjectionEvent
{
    public BusinessOrderProjectionEvent(string orderId, string state)
    {
        OrderId = orderId;
        State = state;
    }

    public string OrderId { get; }

    public string State { get; }
}

public readonly struct BusinessBillingReminderEvent
{
    public BusinessBillingReminderEvent(string orderId)
    {
        OrderId = orderId;
    }

    public string OrderId { get; }
}

public readonly struct BusinessInvoiceAvailableEvent
{
    public BusinessInvoiceAvailableEvent(string orderId)
    {
        OrderId = orderId;
    }

    public string OrderId { get; }
}

public readonly struct BusinessShipmentQueuedEvent
{
    public BusinessShipmentQueuedEvent(string orderId, string label)
    {
        OrderId = orderId;
        Label = label;
    }

    public string OrderId { get; }

    public string Label { get; }
}

public readonly struct BusinessInventoryRestockedEvent
{
    public BusinessInventoryRestockedEvent(string sku, int quantity)
    {
        Sku = sku;
        Quantity = quantity;
    }

    public string Sku { get; }

    public int Quantity { get; }
}

public readonly struct BusinessReserveInventoryRequest
{
    public BusinessReserveInventoryRequest(string orderId, string sku, int quantity)
    {
        OrderId = orderId;
        Sku = sku;
        Quantity = quantity;
    }

    public string OrderId { get; }

    public string Sku { get; }

    public int Quantity { get; }
}

public readonly struct BusinessReserveInventoryResponse
{
    public BusinessReserveInventoryResponse(string orderId, string sku, bool accepted, int quantity, int remaining)
    {
        OrderId = orderId;
        Sku = sku;
        Accepted = accepted;
        Quantity = quantity;
        Remaining = remaining;
    }

    public string OrderId { get; }

    public string Sku { get; }

    public bool Accepted { get; }

    public int Quantity { get; }

    public int Remaining { get; }
}

public readonly struct BusinessQuoteShipmentRequest
{
    public BusinessQuoteShipmentRequest(string orderId, string city)
    {
        OrderId = orderId;
        City = city;
    }

    public string OrderId { get; }

    public string City { get; }
}

public readonly struct BusinessQuoteShipmentResponse
{
    public BusinessQuoteShipmentResponse(string orderId, string carrier, decimal price)
    {
        OrderId = orderId;
        Carrier = carrier;
        Price = price;
    }

    public string OrderId { get; }

    public string Carrier { get; }

    public decimal Price { get; }
}

public readonly struct BusinessFraudScoreInput
{
    public BusinessFraudScoreInput(string orderId, int quantity)
    {
        OrderId = orderId;
        Quantity = quantity;
    }

    public string OrderId { get; }

    public int Quantity { get; }
}

public readonly struct BusinessFraudScoreCalculatedEvent
{
    public BusinessFraudScoreCalculatedEvent(string orderId, int score, int workerIndex)
    {
        OrderId = orderId;
        Score = score;
        WorkerIndex = workerIndex;
    }

    public string OrderId { get; }

    public int Score { get; }

    public int WorkerIndex { get; }
}

public interface IBusinessShippingLabelTool
{
    string CreateLabel(string orderId, string carrier);
}
