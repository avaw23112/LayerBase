using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Core.Event;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult PostTo<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        return Actors.PostTo(actorId, in value, postPolicy, fullPolicy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult TryPostTo<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        return Actors.TryPostTo(actorId, in value, postPolicy, fullPolicy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PostToMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent value,
        ActorPostPolicy? postPolicy = null,
        ActorMailFullPolicy? fullPolicy = null)
        where TEvent : struct
    {
        Actors.PostToMany(actorIds, in value, postPolicy, fullPolicy);
    }
}
