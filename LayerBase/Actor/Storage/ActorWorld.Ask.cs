using LayerBase.Async;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public LBTask<TResponse> Ask<TRequest, TResponse>(
        ActorId           actorId,
        in TRequest       request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return LBTask<TResponse>.FromCanceled(cancellationToken);
        }

        var source = new LBTaskCompletionSource<TResponse>();
        var mail = new ActorCallMail<TRequest, TResponse>(in request, cancellationToken, source);
        PostResult postResult = TryPostCall(actorId, in mail);
        if (!postResult.IsSuccess)
        {
            source.SetException(new ActorCallException(
                ToCallFailureKind(postResult)));
        }

        return source.Task;
    }

    internal PostResult TryPostCall<TRequest, TResponse>(
        ActorId                               actorId,
        in ActorCallMail<TRequest, TResponse> mail)
        where TRequest : struct
        where TResponse : struct
    {
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return PostResult.Failure(
                ActorPostStatus.ActorNotFound,
                PostFailureKind.InvalidActorId);
        }

        return _archetypes[actorId.ArchetypeId].PostCall(actorId, in mail);
    }

    private static ActorCallFailureKind ToCallFailureKind(PostResult postResult)
    {
        return postResult.ActorStatus switch
               {
                   ActorPostStatus.ActorNotFound       => ActorCallFailureKind.InvalidActorId,
                   ActorPostStatus.ActorNotAlive       => ActorCallFailureKind.ActorNotFound,
                   ActorPostStatus.ActorPendingDestroy => ActorCallFailureKind.PendingDestroy,
                   ActorPostStatus.MailFullRejected    => ActorCallFailureKind.MailboxFull,
                   _                                   => ActorCallFailureKind.MailboxFull
               };
    }
}