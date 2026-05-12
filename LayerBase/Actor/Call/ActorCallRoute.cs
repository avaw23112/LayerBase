using LayerBase.Async;

namespace LayerBase.Actor;

public delegate LBTask<TResponse> ActorCallInvoker<TActor, TRequest, TResponse>(
    TActor            actor,
    in TRequest       request,
    CancellationToken cancellationToken)
    where TActor : class, IActor
    where TRequest : struct
    where TResponse : struct;

internal static class ActorCallRouteRegistry
{
    private static int s_nextId;

    public static int GetNextId()
    {
        return Interlocked.Increment(ref s_nextId) - 1;
    }
}

internal static class ActorCallRouteId<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    public static readonly int Id = ActorCallRouteRegistry.GetNextId();
}