namespace LayerBase.Lifetime;

internal readonly struct LifetimeOperation
{
    internal LifetimeOperation(int id)
    {
        Id = id;
    }

    public int Id { get; }

    public static LifetimeOperation Invalid => new(-1);
}

internal sealed class LifetimeOperationTracker
{
    private int _nextOperationId;
    private int _activeCount;
    private bool _admissionClosed;

    public int ActiveCount => Volatile.Read(ref _activeCount);

    public bool IsAdmissionClosed => Volatile.Read(ref _admissionClosed);

    public bool IsDrained =>
        Volatile.Read(ref _admissionClosed) &&
        Volatile.Read(ref _activeCount) == 0;

    public bool TryBegin(out LifetimeOperationLease lease)
    {
        if (Volatile.Read(ref _admissionClosed))
        {
            lease = LifetimeOperationLease.Invalid;
            return false;
        }

        int id = Interlocked.Increment(ref _nextOperationId);
        Interlocked.Increment(ref _activeCount);
        lease = LifetimeOperationLease.Create(
            this,
            new LifetimeOperation(id));
        return true;
    }

    public void CloseAdmission()
    {
        Volatile.Write(ref _admissionClosed, true);
    }

    public void CompleteOnOwner(LifetimeOperation operation)
    {
        if (operation.Id < 0)
            return;

        int count = Interlocked.Decrement(ref _activeCount);
        if (count < 0)
        {
            Interlocked.Exchange(ref _activeCount, 0);
            throw new InvalidOperationException(
                $"Operation {operation.Id} completed more than once.");
        }
    }
}
