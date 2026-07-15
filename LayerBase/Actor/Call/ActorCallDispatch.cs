using System.Collections.Concurrent;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Scope;

namespace LayerBase.Actor;

public readonly struct ActorCallRequest<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    public ActorCallRequest(int originScopeId, ActorHandle target, in TRequest request)
    {
        OriginScopeId = originScopeId;
        Target = target;
        Request = request;
    }

    public int OriginScopeId { get; }

    public ActorHandle Target { get; }

    public TRequest Request { get; }
}

internal static class ActorCallDispatcherRegistry
{
    private static readonly ConcurrentDictionary<int, IActorScopeCallDispatcher> s_dispatchers = new();

    public static void EnsureRegistered<TRequest, TResponse>()
        where TRequest : struct
        where TResponse : struct
    {
        int routeId = ScopeLocalCallRouteId<ActorCallRequest<TRequest, TResponse>, TResponse>.Id;
        s_dispatchers.GetOrAdd(routeId, static _ => new ActorScopeCallDispatcher<TRequest, TResponse>());
    }

    public static bool TryDispatch(
        int routeId,
        ActorWorld world,
        int runtimeId,
        ScopeCallEnvelope envelope,
        EventPayloadStorage payloadStorage)
    {
        if (!s_dispatchers.TryGetValue(routeId, out IActorScopeCallDispatcher? dispatcher))
            return false;

        dispatcher.Dispatch(world, runtimeId, envelope, payloadStorage);
        return true;
    }
}

internal interface IActorScopeCallDispatcher
{
    void Dispatch(
        ActorWorld world,
        int runtimeId,
        ScopeCallEnvelope envelope,
        EventPayloadStorage payloadStorage);
}

internal sealed class ActorScopeCallDispatcher<TRequest, TResponse> : IActorScopeCallDispatcher
    where TRequest : struct
    where TResponse : struct
{
    public void Dispatch(
        ActorWorld world,
        int runtimeId,
        ScopeCallEnvelope envelope,
        EventPayloadStorage payloadStorage)
    {
        if (!payloadStorage.TryGet<ScopeQueuedCall<ActorCallRequest<TRequest, TResponse>, TResponse>>(
                runtimeId,
                envelope.Payload,
                out var queuedCall))
        {
            envelope.Completion?.TrySetException(
                new InvalidOperationException("Actor call payload is no longer available."));
            return;
        }

        if (queuedCall.CancellationToken.IsCancellationRequested)
        {
            queuedCall.Completion.TrySetCanceled(queuedCall.CancellationToken);
            return;
        }

        try
        {
            TRequest request = queuedCall.Request.Request;
            var mail = new ActorCallMail<TRequest, TResponse>(
                in request,
                queuedCall.CancellationToken,
                queuedCall.Completion.Source);
            PostResult postResult = world.TryPostCall(queuedCall.Request.Target.ActorId, in mail);
            if (!postResult.IsSuccess)
            {
                queuedCall.Completion.TrySetException(new ActorCallException(
                    ActorWorld.ToCallFailureKind(postResult)));
            }
        }
        catch (Exception ex)
        {
            queuedCall.Completion.TrySetException(ex);
        }
    }
}
