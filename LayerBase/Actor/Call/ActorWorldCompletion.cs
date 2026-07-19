using LayerBase.Async;

namespace LayerBase.Actor;

internal interface IActorWorldCompletion
{
    void CompleteOnOwner();
}

internal sealed class ActorCallCompletion<TActor, TResponse> : IActorWorldCompletion
    where TActor : class, IActor, new()
    where TResponse : struct
{
    private readonly TypedActorStorage<TActor> _storage;
    private readonly int _slotIndex;
    private readonly LBTask<TResponse> _task;
    private readonly LBTaskCompletionSource<TResponse> _target;
    private readonly IDisposable? _operationResource;

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
        _storage.CompleteOperation(_slotIndex);

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
        finally
        {
            _operationResource?.Dispose();
        }
    }
}
