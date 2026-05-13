namespace LayerBase.Actor;

public readonly struct ActorMailPumpOptions
{
    public readonly int MaxTotalMailsPerPump;
    public readonly int MaxMailsPerBucketPerPump;
    public readonly int MaxMailsPerActorPerPump;
    public readonly int MaxEmptyBucketChecksPerPump;
    public readonly int TimeCheckInterval;
    public readonly int MaxEventCountPerPump;

    public ActorMailPumpOptions(
        int maxTotalMailsPerPump,
        int maxMailsPerBucketPerPump,
        int maxMailsPerActorPerPump,
        int maxEmptyBucketChecksPerPump,
        int timeCheckInterval,
        int maxEventCountPerPump)
    {
        MaxTotalMailsPerPump = maxTotalMailsPerPump;
        MaxMailsPerBucketPerPump = maxMailsPerBucketPerPump;
        MaxMailsPerActorPerPump = maxMailsPerActorPerPump;
        MaxEmptyBucketChecksPerPump = maxEmptyBucketChecksPerPump;
        TimeCheckInterval = Math.Max(timeCheckInterval, 1);
        MaxEventCountPerPump = maxEventCountPerPump;
    }

    public static ActorMailPumpOptions Default => new(
        maxTotalMailsPerPump: 1024,
        maxMailsPerBucketPerPump: 0,
        maxMailsPerActorPerPump: 0,
        maxEmptyBucketChecksPerPump: 64,
        timeCheckInterval: 64,
        maxEventCountPerPump : 64);

    public static ActorMailPumpOptions Fair => new(
        maxTotalMailsPerPump: 1024,
        maxMailsPerBucketPerPump: 128,
        maxMailsPerActorPerPump: 8,
        maxEmptyBucketChecksPerPump: 64,
        timeCheckInterval: 16,
        maxEventCountPerPump : 1);
}
