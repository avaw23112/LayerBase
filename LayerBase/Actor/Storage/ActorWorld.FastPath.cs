using System.Runtime.CompilerServices;
using System.Diagnostics;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static PostResult BuildEventNotSupportedCold<TEvent>()
        where TEvent : struct
    {
        return PostResult.Failure(
            ActorPostStatus.EventNotSupported,
            PostFailureKind.UnsupportedEvent);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PostResult BuildRouteUnsupportedCold<TEvent>()
        where TEvent : struct
    {
        return PostResult.Failure(
            ActorPostStatus.EventNotSupported,
            PostFailureKind.UnsupportedEvent);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PostResult BuildPostFailureCold(ActorId actorId)
    {
        return PostResult.Failure(
            ActorPostStatus.PhysicalTargetInvalid,
            PostFailureKind.PhysicalTargetInvalid);
    }
}
