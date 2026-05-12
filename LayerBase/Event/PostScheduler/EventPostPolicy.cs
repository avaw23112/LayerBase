namespace LayerBase.Core.Event;

public readonly struct EventPostPolicy
{
    public readonly PostDeliveryMode Mode;
    public readonly BackpressurePolicy Backpressure;
    public readonly int MaxPending;
    public readonly MergeFailurePolicy MergeFailure;

    public EventPostPolicy(
        PostDeliveryMode   mode,
        BackpressurePolicy backpressure,
        int                maxPending,
        MergeFailurePolicy mergeFailure = MergeFailurePolicy.Reject)
    {
        Mode = mode;
        Backpressure = backpressure;
        MaxPending = maxPending;
        MergeFailure = mergeFailure;
    }

    public static EventPostPolicy Default => new(PostDeliveryMode.Normal, BackpressurePolicy.RejectNew, 0);
}