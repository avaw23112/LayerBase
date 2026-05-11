namespace LayerBase.Actor;

internal struct ActorLifecycleHandles
{
    public ActorLifecycleHandle Update;
    public ActorLifecycleHandle LateUpdate;
    public ActorLifecycleHandle FixedUpdate;

    public static ActorLifecycleHandles Empty => new()
    {
        Update = ActorLifecycleHandle.Invalid,
        LateUpdate = ActorLifecycleHandle.Invalid,
        FixedUpdate = ActorLifecycleHandle.Invalid
    };
}
