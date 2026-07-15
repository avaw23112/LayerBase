using LayerBase.Core.Event;

namespace LayerBase.Worker;

public readonly struct WorkerEventJobOptions
{
    private readonly EventPostPolicy? _resultPostPolicy;

    public WorkerEventJobOptions(EventPostPolicy resultPostPolicy)
    {
        _resultPostPolicy = resultPostPolicy;
    }

    internal EventPostPolicy? ResultPostPolicy => _resultPostPolicy;

    public static WorkerEventJobOptions Default => default;

    public static WorkerEventJobOptions All => new(
        new EventPostPolicy(
            PostDeliveryMode.Normal,
            BackpressurePolicy.RejectNew,
            maxPending: 0));

    public static WorkerEventJobOptions Latest => new(
        new EventPostPolicy(
            PostDeliveryMode.Latest,
            BackpressurePolicy.RejectNew,
            maxPending: 1));

    public static WorkerEventJobOptions Coalesced => new(
        new EventPostPolicy(
            PostDeliveryMode.Coalesced,
            BackpressurePolicy.RejectNew,
            maxPending: 1));
}
