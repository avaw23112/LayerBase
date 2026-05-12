using LayerBase.Async;

namespace LayerBase.Actor;

internal static class ActorCallFailure
{
    public static LBTask<TResponse> InvalidActor<TResponse>(
        ActorCallFailureKind kind)
        where TResponse : struct
    {
        return LBTask<TResponse>.FromException(new ActorCallException(kind));
    }

    public static LBTask<TResponse> InvalidActor<TResponse>(
        ActorId              actorId,
        ActorCallFailureKind kind)
        where TResponse : struct
    {
        return LBTask<TResponse>.FromException(new ActorCallException(kind, actorId));
    }

    public static LBTask<TResponse> Unsupported<TResponse, TRequest, TExpectedResponse>()
        where TResponse : struct
        where TRequest : struct
        where TExpectedResponse : struct
    {
        return LBTask<TResponse>.FromException(
            new ActorCallException(
                ActorCallFailureKind.UnsupportedRequest,
                typeof(TRequest),
                typeof(TExpectedResponse)));
    }
}