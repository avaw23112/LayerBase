using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Layers;

namespace LayerBase;

public static class LayerActorExtensions
{
    public static ActorWorld Actors(this Layer layer)
    {
        if (layer == null)
        {
            throw new ArgumentNullException(nameof(layer));
        }

        return layer.OwnerContext?.Actors
               ?? throw new InvalidOperationException("Layer not attached to LayerRuntime.");
    }

    public static LBTask<TResponse> AskActor<TRequest, TResponse>(
        this Layer        layer,
        ActorId           actorId,
        in TRequest       request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return layer.Actors().Ask<TRequest, TResponse>(actorId, in request, cancellationToken);
    }

    public static TActor CreateActor<TActor>(this Layer layer, bool usePool = false)
        where TActor : class, IActor, new()
    {
        return layer.Actors().CreateActor<TActor>(usePool);
    }
}