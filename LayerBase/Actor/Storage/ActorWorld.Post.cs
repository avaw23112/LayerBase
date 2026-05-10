using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult PostTo<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        EventPostState<TEvent>? state = EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);
        if (state == null || state.RouteCode == ActorPostRouteCode.Disabled)
        {
            return BuildEventNotSupportedCold<TEvent>();
        }
        if (state.RouteCode == ActorPostRouteCode.QueuedGrowPhysicalSafe)
        {
            return PostQueuedGrowPhysicalSafe(actorId, in value, state);
        }
        return PostToNonDefaultCold(actorId, in value, state, state.RouteCode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostToMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent value)
        where TEvent : struct
    {
        foreach (ActorId actorId in actorIds)
        {
            _ = PostTo(actorId, in value);
        }
    }

    private PostResult TryPostToDiagnostic<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        return BuildEventNotSupportedCold<TEvent>();
    }
}
