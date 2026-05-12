namespace LayerBase.Core.Event;

public readonly struct PostSchedulerOptions
{
    public readonly int ReadyCapacity;
    public readonly int NextCapacity;
    public readonly int MaxEventsPerPump;
    public readonly double MaxMillisecondsPerPump;
    public readonly int MaxWavesPerPump;
    public readonly int TimeCheckInterval;
    public readonly int MaxCompletionsPerPump;
    public readonly int MaxIngressPostsPerPump;
    public readonly BackpressurePolicy DefaultBackpressure;

    public PostSchedulerOptions(
        int                readyCapacity,
        int                nextCapacity,
        int                maxEventsPerPump,
        double             maxMillisecondsPerPump,
        int                maxWavesPerPump,
        int                timeCheckInterval,
        BackpressurePolicy defaultBackpressure,
        int                maxCompletionsPerPump  = 0,
        int                maxIngressPostsPerPump = 4096)
    {
        ReadyCapacity = readyCapacity;
        NextCapacity = nextCapacity;
        MaxEventsPerPump = maxEventsPerPump;
        MaxMillisecondsPerPump = maxMillisecondsPerPump;
        MaxWavesPerPump = maxWavesPerPump <= 0 ? 1 : maxWavesPerPump;
        TimeCheckInterval = timeCheckInterval <= 0 ? 64 : timeCheckInterval;
        MaxCompletionsPerPump = maxCompletionsPerPump;
        MaxIngressPostsPerPump = maxIngressPostsPerPump;
        DefaultBackpressure = defaultBackpressure;
    }

    public static PostSchedulerOptions Default => new(
        readyCapacity: 1024,
        nextCapacity: 1024,
        maxEventsPerPump: 0,
        maxMillisecondsPerPump: 0,
        maxWavesPerPump: 1,
        timeCheckInterval: 64,
        maxCompletionsPerPump: 0,
        maxIngressPostsPerPump: 4096,
        defaultBackpressure: BackpressurePolicy.RejectNew);
}