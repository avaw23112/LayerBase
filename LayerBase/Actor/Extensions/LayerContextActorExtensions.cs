using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Scope;

namespace LayerBase;

public static class LayerContextActorExtensions
{
    public static ScopeActorGateway Actors(this ILayerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (ScopeObjectBinder.TryGet(context, out ScopeObjectBinding? scopeBinding))
        {
            scopeBinding.Scope.RequireAccess("ILayerContext.Actors");
            return scopeBinding.Scope.Actors;
        }

        ServiceLayerBinding binding = ServiceLayerBinder.RequireBinding(context);
        if (binding.Scope != null)
        {
            binding.Scope.RequireAccess("ILayerContext.Actors");
            return binding.Scope.Actors;
        }

        return new ScopeActorGateway(binding.Runtime.Actors);
    }

    private static ActorWorld ResolveActorWorld(ILayerContext context)
    {
        if (ScopeObjectBinder.TryGet(context, out ScopeObjectBinding? scopeBinding))
        {
            scopeBinding.Scope.RequireAccess("ILayerContext.ActorWorld");
            return scopeBinding.Scope.Actors.InnerWorld;
        }

        ServiceLayerBinding binding = ServiceLayerBinder.RequireBinding(context);
        if (binding.Scope != null)
        {
            binding.Scope.RequireAccess("ILayerContext.ActorWorld");
            return binding.Scope.Actors.InnerWorld;
        }

        return binding.Runtime.Actors;
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
        ActorWorld world = ResolveActorWorld(context);
        return world.Ask<TRequest, TResponse>(actorId, in request, cancellationToken);
    }

    public static void PostActor<TMessage>(
        this ILayerContext context,
        ActorId            actorId,
        in TMessage        message)
        where TMessage : struct
    {
        if (ScopeObjectBinder.TryGet(context, out ScopeObjectBinding? scopeBinding))
        {
            scopeBinding.Scope.RequireAccess("ILayerContext.PostActor");
            scopeBinding.Scope.Actors.PostTo(actorId, in message);
            return;
        }

        ServiceLayerBinder.RequireBinding(context).Runtime.PostTo(actorId, in message);
    }

    public static void DestroyActor(
        this ILayerContext context,
        ActorId            actorId)
    {
        ActorWorld world = ResolveActorWorld(context);
        world.DestroyActor(actorId);
    }

    public static TActor CreateActor<TActor>(this ILayerContext context)
        where TActor : class, IActor, new()
    {
        ActorWorld world = ResolveActorWorld(context);
        return world.CreateActor<TActor>(usePool: false);
    }

    public static TActor CreatePooledActor<TActor>(this ILayerContext context)
        where TActor : class, IActor, new()
    {
        ActorWorld world = ResolveActorWorld(context);
        return world.CreateActor<TActor>(usePool: true);
    }

    public static TActor CreateActor<TActor>(this ILayerContext context, bool usePool)
        where TActor : class, IActor, new()
    {
        ActorWorld world = ResolveActorWorld(context);
        return world.CreateActor<TActor>(usePool);
    }
}
