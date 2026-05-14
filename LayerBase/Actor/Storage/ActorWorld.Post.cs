using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostTo<TEvent>(in ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        EventStreamCenter<TEvent>? streamCenter =
            EventStreamRuntime<TEvent>.GetCenterUnchecked(RuntimeIndex, actorId.ArchetypeId);

        if (streamCenter != null)
        {
            streamCenter.Post(actorId, in value);
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