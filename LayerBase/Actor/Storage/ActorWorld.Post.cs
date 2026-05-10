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
        if (state == null)
        {
            return BuildEventNotSupportedCold<TEvent>();
        }

        return PostCompiled(actorId, in value, state);
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
