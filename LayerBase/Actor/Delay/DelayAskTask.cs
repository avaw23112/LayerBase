using LayerBase.Async;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

internal sealed class DelayAskTask<TRequest, TResponse> : IActorDelayTask
    where TRequest : struct
    where TResponse : struct
{
    private readonly ActorWorld _world;
    private readonly ActorId _actorId;
    private readonly TRequest _request;
    private readonly CancellationToken _cancellationToken;
    private readonly LBTaskCompletionSource<TResponse> _source;

    public DelayAskTask(
        ActorWorld world,
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken,
        LBTaskCompletionSource<TResponse> source)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _actorId = actorId;
        _request = request;
        _cancellationToken = cancellationToken;
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public void Execute()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            _source.SetCanceled(_cancellationToken);
            return;
        }

        var mail = new ActorCallMail<TRequest, TResponse>(in _request, _cancellationToken, _source);
        PostResult postResult = _world.TryPostCall(_actorId, in mail);
        if (!postResult.IsSuccess)
        {
            _source.SetException(new ActorCallException(
                ActorCallFailureKind.MailboxFull,
                postResult.ErrorMessage));
        }
    }

    public void Cancel()
    {
        _source.SetCanceled(_cancellationToken);
    }
}
