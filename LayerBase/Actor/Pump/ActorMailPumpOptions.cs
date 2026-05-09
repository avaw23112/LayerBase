namespace LayerBase.Actor;

public readonly struct ActorMailPumpOptions
{
    public readonly int MaxTotalMailsPerPump;
    public readonly int MaxMailsPerBucketPerPump;
    public readonly int MaxMailsPerActorPerPump;
    public readonly int MaxEmptyBucketChecksPerPump;

    public ActorMailPumpOptions(
        int maxTotalMailsPerPump,
        int maxMailsPerBucketPerPump,
        int maxMailsPerActorPerPump,
        int maxEmptyBucketChecksPerPump)
    {
        MaxTotalMailsPerPump = maxTotalMailsPerPump;
        MaxMailsPerBucketPerPump = maxMailsPerBucketPerPump;
        MaxMailsPerActorPerPump = maxMailsPerActorPerPump;
        MaxEmptyBucketChecksPerPump = maxEmptyBucketChecksPerPump;
    }

    public static ActorMailPumpOptions Default => new(
        maxTotalMailsPerPump: 1024,
        maxMailsPerBucketPerPump: 128,
        maxMailsPerActorPerPump: 8,
        maxEmptyBucketChecksPerPump: 64);
}
