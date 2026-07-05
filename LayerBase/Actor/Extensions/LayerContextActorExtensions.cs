using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;

namespace LayerBase;

public static class LayerContextActorExtensions
{
    /// <summary>
    /// Gets the <see cref="ActorWorld"/> bound to the current context.
    ///
    /// Advanced API:
    /// Prefer <c>CreateActor</c>, <c>CreatePooledActor</c>, <c>PostActor</c>, <c>Ask</c>, and <c>DestroyActor</c>
    /// in normal business code. Access <see cref="ActorWorld"/> directly only when doing batch actor operations,
    /// framework integration, or low-level tuning.
    /// </summary>
    public static ActorWorld Actors(this ILayerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return ServiceLayerBinder.RequireBinding(context).Runtime.Actors;
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
        ServiceLayerBinder.RequireBinding(context).Runtime.PostTo(actorId, in message);
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
