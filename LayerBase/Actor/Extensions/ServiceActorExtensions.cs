using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Scope;

namespace LayerBase;

public static class ServiceActorExtensions
{
    /// <summary>
    /// Gets the <see cref="ActorWorld"/> bound to the current service.
    ///
    /// Advanced API:
    /// Prefer the context-first actor facade APIs in normal business code. Access <see cref="ActorWorld"/>
    /// directly only when doing batch actor operations, framework integration, or low-level tuning.
    /// </summary>
    public static ActorWorld Actors(this IService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (ScopeServiceOwnerRegistry.TryGet(service, out ScopeRuntime ownerScope))
        {
            return ownerScope.Actors;
        }

        return ServiceLayerBinder.RequireBinding(service).Runtime.Actors;
    }

    public static LBTask<TResponse> AskActor<TRequest, TResponse>(
        this IService     service,
        ActorId           actorId,
        in TRequest       request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return service.Actors().Ask<TRequest, TResponse>(actorId, in request, cancellationToken);
    }

    public static TActor CreateActor<TActor>(this IService service, bool usePool = false)
        where TActor : class, IActor, new()
    {
        return service.Actors().CreateActor<TActor>(usePool);
    }
}
