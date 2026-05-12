namespace LayerBase.Actor;

internal interface IActorEventBucket
{
    PumpOneResult PumpOne(
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats,
        int                       bucketIndex);

    bool HasPendingWork();
}