using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Scope;

namespace LayerBase;

public static class LayerContextActorExtensions
{
    public static ActorClient ActorClient(this ILayerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return ServiceLayerBinder.RequireBinding(context).OwnerScope.ActorClient;
    }

    public static ActorFactory ActorFactory(this ILayerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return ServiceLayerBinder.RequireBinding(context).OwnerScope.ActorFactory;
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
        return context.ActorClient().Call<TRequest, TResponse>(actorId, in request, cancellationToken);
    }

    public static void PostActor<TMessage>(
        this ILayerContext context,
        ActorId            actorId,
        in TMessage        message)
        where TMessage : struct
    {
        ScopePostResult result = context.ActorClient().Post(actorId, in message);
        if (!result.IsAccepted)
            throw new InvalidOperationException($"Actor post failed: {result.Status}.");
    }

    public static void DestroyActor(
        this ILayerContext context,
        ActorId            actorId)
    {
        context.ActorClient().Destroy(actorId);
    }

    public static TActor CreateActor<TActor>(this ILayerContext context)
        where TActor : class, IActor, new()
    {
        return context.ActorFactory().Create<TActor>(usePool: false);
    }

    public static TActor CreatePooledActor<TActor>(this ILayerContext context)
        where TActor : class, IActor, new()
    {
        return context.ActorFactory().Create<TActor>(usePool: true);
    }

    public static TActor CreateActor<TActor>(this ILayerContext context, bool usePool)
        where TActor : class, IActor, new()
    {
        return context.ActorFactory().Create<TActor>(usePool);
    }
}
