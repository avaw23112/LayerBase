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

    public PostResult PostInside<TEvent>(in TEvent value)
        where TEvent : struct
    {
        return World.PostTo(ActorId, in value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public PostResult TryPostInside<TEvent>(in TEvent value)
        where TEvent : struct
    {
        return World.PostTo(ActorId, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool PostFastInside<TEvent>(in TEvent value)
        where TEvent : struct
    {
        return World.PostFast(ActorId, in value);
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
