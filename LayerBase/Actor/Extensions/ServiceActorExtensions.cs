using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Scope;

namespace LayerBase;

public static class ServiceActorExtensions
{
    /// <summary>
    /// Gets the <see cref="ScopeActorGateway"/> bound to the current service
    /// for posting messages to actors.
    /// </summary>
    public static ScopeActorGateway Actors(this IService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (ScopeObjectBinder.TryGet(service, out ScopeObjectBinding? scopeBinding))
        {
            scopeBinding.Scope.RequireAccess("IService.Actors");
            return scopeBinding.Scope.Actors;
        }

        ServiceLayerBinding? binding = ServiceLayerBinder.GetBinding(service);
        if (binding?.Scope != null)
        {
            binding.Scope.RequireAccess("IService.Actors");
            return binding.Scope.Actors;
        }

        return new ScopeActorGateway(
            (binding?.Runtime ?? ServiceLayerBinder.RequireBinding(service).Runtime).Actors);
    }

    public static LBTask<TResponse> AskActor<TRequest, TResponse>(
        this IService     service,
        ActorId           actorId,
        in TRequest       request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        ActorWorld world = ResolveActorWorld(service);
        return world.Ask<TRequest, TResponse>(actorId, in request, cancellationToken);
    }

    public static TActor CreateActor<TActor>(this IService service, bool usePool = false)
        where TActor : class, IActor, new()
    {
        ActorWorld world = ResolveActorWorld(service);
        return world.CreateActor<TActor>(usePool);
    }

    private static ActorWorld ResolveActorWorld(IService service)
    {
        if (ScopeObjectBinder.TryGet(service, out ScopeObjectBinding? scopeBinding))
        {
            scopeBinding.Scope.RequireAccess("IService.ActorWorld");
            return scopeBinding.Scope.Actors.InnerWorld;
        }

        ServiceLayerBinding? binding = ServiceLayerBinder.GetBinding(service);
        if (binding?.Scope != null)
        {
            binding.Scope.RequireAccess("IService.ActorWorld");
            return binding.Scope.Actors.InnerWorld;
        }

        return binding?.Runtime.Actors ?? ServiceLayerBinder.RequireBinding(service).Runtime.Actors;
    }
}
