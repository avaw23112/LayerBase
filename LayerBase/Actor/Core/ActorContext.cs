using LayerBase.Core.Event;

namespace LayerBase.Actor;

public readonly struct ActorContext
{
    public ActorId ActorId { get; }

    internal ActorWorld World { get; }

    internal ActorContext(ActorWorld world, ActorId actorId)
    {
        World = world;
        ActorId = actorId;
    }

    public PostResult Post<TEvent>(in TEvent value)
        where TEvent : struct
    {
        return World.Post(ActorId, in value);
    }

    public PostResult TryPost<TEvent>(in TEvent value)
        where TEvent : struct
    {
        return World.TryPost(ActorId, in value);
    }

    public bool IsEnable()
    {
        return World.IsEnable(ActorId);
    }

    public bool SetEnable(bool enable)
    {
        return World.SetEnable(ActorId, enable);
    }
}
