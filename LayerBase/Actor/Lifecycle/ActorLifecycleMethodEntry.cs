namespace LayerBase.Actor;

internal readonly struct ActorLifecycleMethodEntry
{
    public readonly ActorId ActorId;
    public readonly IActor Actor;
    public readonly ActorLifecycleMethodInvoker Invoker;

    public ActorLifecycleMethodEntry(
        ActorId                     actorId,
        IActor                      actor,
        ActorLifecycleMethodInvoker invoker)
    {
        ActorId = actorId;
        Actor = actor;
        Invoker = invoker;
    }
}
