using System.Collections.Concurrent;
using LayerBase.Core.Event;
using LayerBase.Scope;

namespace LayerBase.Actor;

public readonly struct ActorCommandBatch<TEvent>
    where TEvent : struct
{
    public ActorCommandBatch(int originScopeId, ActorHandle target, in TEvent value)
    {
        OriginScopeId = originScopeId;
        Target = target;
        Value = value;
        Count = 1;
    }

    public int OriginScopeId { get; }

    public ActorHandle Target { get; }

    public TEvent Value { get; }

    public int Count { get; }
}

public readonly struct ActorDestroyCommand
{
    public ActorDestroyCommand(int originScopeId, ActorHandle target)
    {
        OriginScopeId = originScopeId;
        Target = target;
    }

    public int OriginScopeId { get; }

    public ActorHandle Target { get; }
}

internal static class ActorCommandDispatcherRegistry
{
    private static readonly ConcurrentDictionary<int, IActorCommandDispatcher> s_dispatchers = new();

    public static void EnsurePostRegistered<TEvent>()
        where TEvent : struct
    {
        int routeId = EventTypeId<ActorCommandBatch<TEvent>>.Id;
        s_dispatchers.GetOrAdd(routeId, static _ => new ActorPostCommandDispatcher<TEvent>());
    }

    public static void EnsureDestroyRegistered()
    {
        int routeId = EventTypeId<ActorDestroyCommand>.Id;
        s_dispatchers.GetOrAdd(routeId, static _ => ActorDestroyCommandDispatcher.Instance);
    }

    public static bool TryDispatch(
        int routeId,
        ActorWorld world,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        if (!s_dispatchers.TryGetValue(routeId, out IActorCommandDispatcher? dispatcher))
            return false;

        dispatcher.Dispatch(world, runtimeId, payload, payloadStorage);
        return true;
    }
}

internal interface IActorCommandDispatcher
{
    void Dispatch(
        ActorWorld world,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage);
}

internal sealed class ActorPostCommandDispatcher<TEvent> : IActorCommandDispatcher
    where TEvent : struct
{
    public void Dispatch(
        ActorWorld world,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        if (!payloadStorage.TryGet<ActorCommandBatch<TEvent>>(runtimeId, payload, out var batch))
            return;

        TEvent value = batch.Value;
        world.PostTo(batch.Target.ActorId, in value);
    }
}

internal sealed class ActorDestroyCommandDispatcher : IActorCommandDispatcher
{
    public static readonly ActorDestroyCommandDispatcher Instance = new();

    private ActorDestroyCommandDispatcher()
    {
    }

    public void Dispatch(
        ActorWorld world,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage)
    {
        if (!payloadStorage.TryGet<ActorDestroyCommand>(runtimeId, payload, out var command))
            return;

        world.DestroyActor(command.Target.ActorId);
    }
}
