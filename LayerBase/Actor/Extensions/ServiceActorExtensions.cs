using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;

namespace LayerBase;

public static class ServiceActorExtensions
{
    /// <summary>
    /// Gets the actor accessor bound to the current service scope.
    /// </summary>
    public static ActorAccessor Actors(this IService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return ServiceLayerBinder.RequireBinding(service).Runtime.ScopeHost.MainScope.Actors;
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
