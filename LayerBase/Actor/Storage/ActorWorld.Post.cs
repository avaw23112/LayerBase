using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostTo<TEvent>(in ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        if (!CanUseWorldFast())
        {
            return;
        }

        PostToUnchecked(actorId, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostToMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent             value)
        where TEvent : struct
    {
        if (!CanUseWorldFast())
        {
            return;
        }

        int length = actorIds.Length;
        int i = 0;
        int unrolledLength = length - (length % 8);
        for (; i < unrolledLength; i += 8)
        {
            PostToUnchecked(actorIds[i], in value);
            PostToUnchecked(actorIds[i + 1], in value);
            PostToUnchecked(actorIds[i + 2], in value);
            PostToUnchecked(actorIds[i + 3], in value);
            PostToUnchecked(actorIds[i + 4], in value);
            PostToUnchecked(actorIds[i + 5], in value);
            PostToUnchecked(actorIds[i + 6], in value);
            PostToUnchecked(actorIds[i + 7], in value);
        }
        for (; i < length; i++)
        {
            PostToUnchecked(actorIds[i], in value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PostToUnchecked<TEvent>(in ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        EventStreamCenter<TEvent>? streamCenter =
            EventStreamRuntime<TEvent>.GetCenterUnchecked(RuntimeIndex, actorId.ArchetypeId);

        if (streamCenter != null)
        {
            streamCenter.Post(actorId, in value);
        }
    }
}
