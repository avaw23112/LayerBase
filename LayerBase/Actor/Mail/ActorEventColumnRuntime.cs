namespace LayerBase.Actor;

internal abstract class ActorEventColumnRuntime
{
    public abstract void EnsureSlotCapacity(int slotIndex);
}

internal interface IActorEventColumn<TEvent>
    where TEvent : struct
{
    bool PumpOne(ref RuntimeFrameBudget budget);
}
