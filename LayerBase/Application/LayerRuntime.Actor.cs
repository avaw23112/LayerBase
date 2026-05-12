using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Async;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TActor CreateActor<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        return Actors.CreateActor<TActor>(usePool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LBTask<TResponse> AskActor<TRequest, TResponse>(
        ActorId           actorId,
        in TRequest       request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        return Actors.Ask<TRequest, TResponse>(actorId, in request, cancellationToken);
    }
}