using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;

namespace LayerBase;

public static class LayerContextActorExtensions
{
    /// <summary>
    /// Gets the actor accessor bound to the current context scope.
    /// </summary>
    public static ActorAccessor Actors(this ILayerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return ServiceLayerBinder.RequireBinding(context).Runtime.ScopeHost.MainScope.Actors;
    }

    public static LBTask<TResponse> Ask<TRequest, TResponse>(
        this ILayerContext context,
        ActorId            actorId,
        in TRequest        request,
        CancellationToken  cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return context.AskActor<TRequest, TResponse>(actorId, in request, cancellationToken);
    }

    public static LBTask<TResponse> AskActor<TRequest, TResponse>(
        this ILayerContext context,
        ActorId            actorId,
        in TRequest        request,
        CancellationToken  cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return context.Actors().Ask<TRequest, TResponse>(actorId, in request, cancellationToken);
    }

    public static void PostActor<TMessage>(
        this ILayerContext context,
        ActorId            actorId,
        in TMessage        message)
        where TMessage : struct
    {
        context.Actors().PostTo(actorId, in message);
    }

    public static void DestroyActor(
        this ILayerContext context,
        ActorId            actorId)
    {
        context.Actors().DestroyActor(actorId);
    }

    public static TActor CreateActor<TActor>(this ILayerContext context)
        where TActor : class, IActor, new()
    {
        return context.Actors().CreateActor<TActor>(usePool: false);
    }

    public static TActor CreatePooledActor<TActor>(this ILayerContext context)
        where TActor : class, IActor, new()
    {
        return context.Actors().CreateActor<TActor>(usePool: true);
    }

    public static TActor CreateActor<TActor>(this ILayerContext context, bool usePool)
        where TActor : class, IActor, new()
    {
        return context.Actors().CreateActor<TActor>(usePool);
    }
}
