using LayerBase.Async;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public LBTask<TResponse> ImmediatelyAsk<TRequest, TResponse>(
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

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                actorId,
                ActorCallFailureKind.InvalidActorId);
        }

        return _archetypes[actorId.ArchetypeId].ImmediatelyAsk<TRequest, TResponse>(
            actorId,
            in request,
            cancellationToken);
    }

    public LBTask<TResponse> Call<TActor, TRequest, TResponse>(
        ActorId           actorId,
        in TRequest       request,
        CancellationToken cancellationToken = default)
        where TActor : class, IActor
        where TRequest : struct
        where TResponse : struct
    {
        return Ask<TRequest, TResponse>(actorId, in request, cancellationToken);
    }
}