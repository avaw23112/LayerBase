namespace LayerBase.Lifetime;

internal sealed class LifetimeOperationLease
{
    private readonly LifetimeOperationTracker? _tracker;
    private int _completed;

    private LifetimeOperationLease(
        LifetimeOperationTracker? tracker,
        LifetimeOperation operation)
    {
        _tracker = tracker;
        Operation = operation;
    }

    public LifetimeOperation Operation { get; }

    public static LifetimeOperationLease Invalid { get; } =
        new(null, LifetimeOperation.Invalid);

    internal static LifetimeOperationLease Create(
        LifetimeOperationTracker tracker,
        LifetimeOperation operation)
    {
        return new LifetimeOperationLease(tracker, operation);
    }

    public bool TryComplete()
    {
        if (_tracker == null ||
            Operation.Id < 0 ||
            Interlocked.Exchange(ref _completed, 1) != 0)
        {
            return false;
        }

        _tracker.CompleteOnOwner(Operation);
        return true;
    }
}
