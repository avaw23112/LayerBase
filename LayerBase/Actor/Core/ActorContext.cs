using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Async;
using LayerBase.Layers;

namespace LayerBase.Actor;

public readonly struct ActorContext
{
    public ActorId ActorId { get; }

    internal ActorWorld World { get; }
    internal LayerRuntime? Runtime { get; }

    internal ActorContext(ActorWorld world, ActorId actorId)
    {
        World = world;
        Runtime = world.Runtime;
        ActorId = actorId;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public PostResult Post<TEvent>(in TEvent value)
        where TEvent : struct
    {
        return World.Post(ActorId, in value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public PostResult TryPost<TEvent>(in TEvent value)
        where TEvent : struct
    {
        return World.TryPost(ActorId, in value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public bool IsEnable()
    {
        return World.IsEnable(ActorId);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public bool SetEnable(bool enable)
    {
        return World.SetEnable(ActorId, enable);
    }
}
