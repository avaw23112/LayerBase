using LayerBase.Core.Event;

namespace LayerBase.Actor;

internal abstract class TypedStorageRuntime
{
    public abstract bool IsAlive(int slotIndex, int generation);

    public abstract bool IsEnable(int slotIndex, int generation);

    public abstract bool SetEnable(int slotIndex, int generation, bool enable);

    public abstract bool MarkPendingDestroy(int slotIndex, int generation);

    public abstract void SweepPendingDestroy(ActorWorld world);

    public abstract PostResult Post<TEvent>(
        int slotIndex,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct;

    public abstract void PostToAliveActors<TEvent>(
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct;

    public abstract IEnumerable<IActor> EnumerateActors();
}
