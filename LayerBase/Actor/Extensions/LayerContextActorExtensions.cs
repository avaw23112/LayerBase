using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;

namespace LayerBase;

public static class LayerContextActorExtensions
{
    public static ActorWorld Actors(this ILayerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return ServiceLayerBinder.RequireBinding(context).Runtime.Actors;
    }

    public static LBTask<TResponse> AskActor<TRequest, TResponse>(
        this ILayerContext context,
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return context.Actors().Ask<TRequest, TResponse>(actorId, in request, cancellationToken);
    }
    
    public static TActor CreateActor<TActor>( this ILayerContext context,bool usePool = false)
        where TActor: class,IActor,new()
    {
        return context.Actors().CreateActor<TActor>(usePool);
    }
}
