using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

public static class ActorExtensions
{
    public static ActorId GetActorId(this IActor actor)
    {
        return ActorGeneratedAccess.RequireGenerated(actor).GetId();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PostResult Post<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        return ActorGeneratedAccess.RequireGenerated(actor).Post(in value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static PostResult TryPost<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        return ActorGeneratedAccess.RequireGenerated(actor).TryPost(in value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static bool GetEnable(this IActor actor)
    {
        return ActorGeneratedAccess.RequireGenerated(actor).GetEnable();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static bool SetEnable(this IActor actor, bool enable)
    {
        return ActorGeneratedAccess.RequireGenerated(actor).SetEnable(enable);
    }
}
