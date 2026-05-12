namespace LayerBase.Actor;

internal enum PumpOneResult
{
    Processed,
    EmptyBucket,
    NoWork,
    BucketLimited,
    ActorLimited
}

internal enum ActorColumnPumpResult
{
    Processed,
    NoWork,
    ActorLimited
}