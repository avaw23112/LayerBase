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
        if (!TryGetPhysicalRow(actorId, state, out EventPostRow<TEvent> row, out int slotIndex))
        {
            return BuildPostFailureCold(actorId);
        }
        switch (state.RouteCode) 
        {
            case ActorPostRouteCode.QueuedGrow:
                return PostQueuedGrowCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool, state.Options);
            case ActorPostRouteCode.QueuedRejectNew:
                return PostQueuedRejectNewCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool, state.Options);
            case ActorPostRouteCode.QueuedDropOldest:
                return PostQueuedDropOldestCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool, state.Options);
            case ActorPostRouteCode.Latest:
                return PostLatestCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool);
            case ActorPostRouteCode.Dirty:
                return PostDirtyCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool);
            default:
                return BuildRouteUnsupportedCold<TEvent>();
        }
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
}
