using LayerBase.Async;

namespace LayerBase.Actor;

internal interface IActorWorldCompletion
{
    void CompleteOnOwner();
}

internal interface IActorActiveCall
{
    void CompleteCanceled();

    void CompleteDisposed();
}

internal sealed class ActorCallCompletion<TActor, TResponse> :
    IActorWorldCompletion,
    IActorActiveCall
    where TActor : class, IActor, new()
    where TResponse : struct
{
    private readonly TypedActorStorage<TActor> _storage;
    private readonly int _slotIndex;
    private readonly LBTask<TResponse> _task;
    private readonly LBTaskCompletionSource<TResponse> _target;
    private readonly IDisposable? _operationResource;
    private int _completed;

    public ActorCallCompletion(
        TypedActorStorage<TActor> storage,
        int slotIndex,
        LBTask<TResponse> task,
        LBTaskCompletionSource<TResponse> target,
        IDisposable? operationResource = null)
    {
        _storage = storage;
        _slotIndex = slotIndex;
        _task = task;
        _target = target;
        _operationResource = operationResource;
    }

    public void CompleteOnOwner()
    {
        if (!TryCompleteOperation())
            return;

        try
        {
            _target.SetResult(_task.GetAwaiter().GetResult());
        }
        catch (OperationCanceledException exception)
        {
            _target.SetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            _target.SetException(exception);
        }
    }

    public void CompleteCanceled()
    {
        if (!TryCompleteOperation())
            return;

        _target.SetCanceled(new CancellationToken(canceled: true));
    }

    public void CompleteDisposed()
    {
        if (!TryCompleteOperation())
            return;

        _target.SetException(new ObjectDisposedException(nameof(ActorWorld)));
    }

    private bool TryCompleteOperation()
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0)
            return false;

        _storage.UnregisterActiveCall(_slotIndex, this);
        _storage.CompleteOperation(_slotIndex);
        _operationResource?.Dispose();
        return true;
    }
}
