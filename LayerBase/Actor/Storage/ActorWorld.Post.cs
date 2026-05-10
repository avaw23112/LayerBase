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
        EventPostState<TEvent>? state = EventPostRuntime<TEvent>.GetState(this);
        if (state == null)
        {
            return PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                "Event post state is not built.",
                PostFailureKind.UnsupportedEvent);
        }

        if (state.Route == ActorPostRouteKind.DiagnosticOnly)
        {
            return TryPostToDiagnostic(actorId, in value);
        }

        if (state.Route == ActorPostRouteKind.Disabled)
        {
            return PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                "ActorPost is disabled for this event.",
                PostFailureKind.UnsupportedEvent);
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
        return PostResult.Failure(
            ActorPostStatus.EventNotSupported,
            "DiagnosticOnly route: post is not allowed through default path.",
            PostFailureKind.UnsupportedEvent);
    }
}
