using LayerBase.Async;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public DelayPostHandle DelayPost<TEvent>(
        ActorId   actorId,
        in TEvent value,
        float     delaySeconds)
        where TEvent : struct
    {
        EnsureDelayAvailable();
        return DelayScheduler.Schedule(
            new DelayPostTask<TEvent>(this, actorId, in value),
            delaySeconds);
    }

    public LBTask<TResponse> DelayAsk<TRequest, TResponse>(
        ActorId           actorId,
        in TRequest       request,
        float             delaySeconds,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return LBTask<TResponse>.FromCanceled(cancellationToken);
        }

        EnsureDelayAvailable();
        var source = new LBTaskCompletionSource<TResponse>();
        DelayScheduler.Schedule(
            new DelayAskTask<TRequest, TResponse>(this, actorId, in request, cancellationToken, source),
            delaySeconds);
        return source.Task;
    }

    private void EnsureDelayAvailable()
    {
        if (_state == ActorWorldState.Disposed)
        {
            throw new ObjectDisposedException(nameof(ActorWorld));
        }
    }
}