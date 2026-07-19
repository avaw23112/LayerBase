using System.Runtime.CompilerServices;

namespace LayerBase.Lifetime;

internal readonly struct LifetimeOperation
{
    internal LifetimeOperation(int id)
    {
        Id = id;
    }

    public int Id { get; }

    public static LifetimeOperation Invalid => default;
}

internal sealed class LifetimeOperationTracker
{
    private int _nextOperationId;
    private int _activeCount;
    private bool _admissionClosed;

    public int ActiveCount => _activeCount;

    public bool IsAdmissionClosed => _admissionClosed;

    public bool IsDrained => _admissionClosed && _activeCount == 0;

    public bool TryBegin(out LifetimeOperation operation)
    {
        if (_admissionClosed)
        {
            operation = LifetimeOperation.Invalid;
            return false;
        }

        int id = _nextOperationId++;
        Interlocked.Increment(ref _activeCount);
        operation = new LifetimeOperation(id);
        return true;
    }

    public void CloseAdmission()
    {
        _admissionClosed = true;
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
