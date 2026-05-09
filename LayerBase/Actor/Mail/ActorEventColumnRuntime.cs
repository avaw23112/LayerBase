namespace LayerBase.Actor;

internal abstract class ActorEventColumnRuntime
{
    public abstract void EnsureSlotCapacity(int slotIndex);

    public abstract void ClearMail(int slotIndex);

    public abstract int GetPendingCount(int slotIndex);

    public abstract int GetTotalPendingCount();
}

internal abstract class ActorCallColumnRuntime
{
    public abstract void EnsureSlotCapacity(int slotIndex);

    public abstract void ClearMail(int slotIndex);

    public abstract int GetPendingCount(int slotIndex);

    public abstract int GetTotalPendingCount();
}

internal interface IActorEventColumn<TEvent>
    where TEvent : struct
{
    ActorColumnPumpResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats);

    bool HasPendingWork();
}

internal interface IActorCallColumn<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    ActorColumnPumpResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats);

    bool HasPendingWork();
}
