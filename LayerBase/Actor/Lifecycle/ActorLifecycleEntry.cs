namespace LayerBase.Actor;

internal readonly struct ActorLifecycleEntry<TLifecycle>
    where TLifecycle : class
{
    public readonly ActorId ActorId;
    public readonly TLifecycle Instance;

    public ActorLifecycleEntry(
        ActorId    actorId,
        TLifecycle instance)
    {
        ActorId = actorId;
        Instance = instance;
    }
}