using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Core.Event;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostTo<TEvent>(
        ActorId   actorId,
        in TEvent value)
        where TEvent : struct
    {
         Actors.PostTo(actorId, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostToMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent             value)
        where TEvent : struct
    {
        Actors.PostToMany(actorIds, in value);
    }
}