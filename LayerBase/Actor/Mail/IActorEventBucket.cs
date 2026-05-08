namespace LayerBase.Actor;

internal interface IActorEventBucket
{
    bool PumpOne(ref RuntimeFrameBudget budget);
}
