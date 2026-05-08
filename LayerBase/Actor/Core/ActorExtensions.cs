using LayerBase.Core.Event;

namespace LayerBase.Actor;

public static class ActorExtensions
{
    public static ActorId GetActorId(this IActor actor)
    {
        return ActorGeneratedAccess.RequireGenerated(actor).GetId();
    }

    public static PostResult Post<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        return ActorGeneratedAccess.RequireGenerated(actor).Post(in value);
    }

    public static PostResult TryPost<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        return ActorGeneratedAccess.RequireGenerated(actor).TryPost(in value);
    }
}
