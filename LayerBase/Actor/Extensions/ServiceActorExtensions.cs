using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;

namespace LayerBase;

public static class ServiceActorExtensions
{
    public static ActorClient ActorClient(this IService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return ServiceLayerBinder.RequireBinding(service).OwnerScope.ActorClient;
    }

    public static ActorFactory ActorFactory(this IService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return ServiceLayerBinder.RequireBinding(service).OwnerScope.ActorFactory;
    }

    public static LBTask<TResponse> AskActor<TRequest, TResponse>(
        this IService     service,
        ActorId           actorId,
        in TRequest       request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return service.ActorClient().Call<TRequest, TResponse>(actorId, in request, cancellationToken);
    }

    public static TActor CreateActor<TActor>(this IService service, bool usePool = false)
        where TActor : class, IActor, new()
    {
        return service.ActorFactory().Create<TActor>(usePool);
    }
}
