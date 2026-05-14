using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostTo<TEvent>(in ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        EventPostState<TEvent>? state = EventPostRuntime<TEvent>.GetStateUnchecked(RuntimeIndex);
        if (state == null || state.RouteCode == ActorPostRouteCode.Disabled)
        {
            return ;
        }
        if (!TryGetPhysicalRowWithGeneration(in actorId, state, out EventPostRow<TEvent> row, out int slotIndex))
        {
            return ;
        } 
        PostRoute(in value, state, slotIndex,in row);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PostRoute<TEvent>(in TEvent value, EventPostState<TEvent> state, int slotIndex,in EventPostRow<TEvent> row)
        where TEvent : struct
    {
        switch (state.RouteCode)
        {
            case ActorPostRouteCode.QueuedGrow: 
                PostQueuedGrowCore(slotIndex, in value,in row.Mails, row.DirtySlots, row.BucketIndex, state.Pool,in  state.Options);break;
            case ActorPostRouteCode.QueuedRejectNew:
                PostQueuedRejectNewCore(slotIndex, in value,in  row.Mails, row.DirtySlots, row.BucketIndex, state.Pool, in state.Options);break;
            case ActorPostRouteCode.QueuedDropOldest: 
               PostQueuedDropOldestCore(slotIndex, in value,in  row.Mails, row.DirtySlots, row.BucketIndex, state.Pool,in  state.Options);break;
            case ActorPostRouteCode.Latest: 
                PostLatestCore(slotIndex, in value,in  row.Mails, row.DirtySlots, row.BucketIndex, state.Pool);break;
            case ActorPostRouteCode.Dirty: 
                PostDirtyCore(slotIndex, in value,in  row.Mails, row.DirtySlots, row.BucketIndex, state.Pool);break;
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
            PostTo(actorIds[i], in value);
            PostTo(actorIds[i + 1], in value);
            PostTo(actorIds[i + 2], in value);
            PostTo(actorIds[i + 3], in value);
            PostTo(actorIds[i + 4], in value);
            PostTo(actorIds[i + 5], in value);
            PostTo(actorIds[i + 6], in value);
            PostTo(actorIds[i + 7], in value);
        }
        for (; i < length; i++)
        {
            PostTo(actorIds[i], in value);
        }
    }
}