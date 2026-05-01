namespace LayerBase.Core.Event;

public readonly struct EventPostPolicy
{
    public readonly PostDeliveryMode Mode;
    public readonly BackpressurePolicy Backpressure;
    public readonly int MaxPending;

    public EventPostPolicy(
        PostDeliveryMode mode,
        BackpressurePolicy backpressure,
        int maxPending)
    {
        Mode = mode;
        Backpressure = backpressure;
        MaxPending = maxPending;
    }
    
    public static EventPostPolicy Default => new(PostDeliveryMode.Normal, BackpressurePolicy.RejectNew, 0);
}
