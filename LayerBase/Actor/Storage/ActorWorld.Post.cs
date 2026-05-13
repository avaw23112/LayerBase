using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult PostTo<TEvent>(
        ActorId   actorId,
        in TEvent value)
        where TEvent : struct
    {
        EventPostState<TEvent>? state = EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);
        if (state == null || state.RouteCode == ActorPostRouteCode.Disabled)
        {
            return BuildEventNotSupportedCold<TEvent>();
        }
        if (!TryGetPhysicalRowWithGeneration(actorId, state, out EventPostRow<TEvent> row, out int slotIndex))
        {
            return BuildPostFailureCold(actorId);
        }
        return PostRoute(value, state, slotIndex, row);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostRoute<TEvent>(TEvent value, EventPostState<TEvent> state, int slotIndex, EventPostRow<TEvent> row)
        where TEvent : struct
    {
        switch (state.RouteCode)
        {
            case ActorPostRouteCode.QueuedGrow:
                return PostQueuedGrowCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex, state.Pool,
                    state.Options);
            case ActorPostRouteCode.QueuedRejectNew:
                return PostQueuedRejectNewCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex,
                    state.Pool, state.Options);
            case ActorPostRouteCode.QueuedDropOldest:
                return PostQueuedDropOldestCore(slotIndex, in value, row.Mails, row.DirtySlots, row.BucketIndex,
                    state.Pool, state.Options);
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
        in TEvent             value)
        where TEvent : struct
    {
        int length = actorIds.Length;
        int i = 0;
        int unrolledLength = length - (length % 8);
        for (; i < unrolledLength; i += 8)
        {
            _ = PostTo(actorIds[i], in value);
            _ = PostTo(actorIds[i + 1], in value);
            _ = PostTo(actorIds[i + 2], in value);
            _ = PostTo(actorIds[i + 3], in value);
            _ = PostTo(actorIds[i + 4], in value);
            _ = PostTo(actorIds[i + 5], in value);
            _ = PostTo(actorIds[i + 6], in value);
            _ = PostTo(actorIds[i + 7], in value);
        }
        for (; i < length; i++)
        {
            _ = PostTo(actorIds[i], in value);
        }
    }
}