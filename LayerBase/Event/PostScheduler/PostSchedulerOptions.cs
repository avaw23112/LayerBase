namespace LayerBase.Core.Event;

public enum PayloadDiagnosticsMode : byte
{
    Disabled = 0,
    Local = 1,
    Atomic = 2
}

public readonly struct PostSchedulerOptions
{
    public readonly int ReadyCapacity;
    public readonly int NextCapacity;
    public readonly int MaxEventsPerPump;
    public readonly double MaxMillisecondsPerPump;
    public readonly int MaxWavesPerPump;
    public readonly int TimeCheckInterval;
    public readonly int MaxCompletionsPerPump;
    public readonly BackpressurePolicy DefaultBackpressure;
    public readonly PayloadDiagnosticsMode PayloadDiagnostics;
    public readonly int MaxSpecialPending;

    public PostSchedulerOptions(
        int                readyCapacity,
        int                nextCapacity,
        int                maxEventsPerPump,
        double             maxMillisecondsPerPump,
        int                maxWavesPerPump,
        int                timeCheckInterval,
        BackpressurePolicy defaultBackpressure,
        int                maxCompletionsPerPump = 0,
        PayloadDiagnosticsMode payloadDiagnostics = PayloadDiagnosticsMode.Local,
        int                maxSpecialPending = 4096)
    {
        ReadyCapacity = readyCapacity;
        NextCapacity = nextCapacity;
        MaxEventsPerPump = maxEventsPerPump;
        MaxMillisecondsPerPump = maxMillisecondsPerPump;
        MaxWavesPerPump = maxWavesPerPump <= 0 ? 1 : maxWavesPerPump;
        TimeCheckInterval = timeCheckInterval <= 0 ? 64 : timeCheckInterval;
        MaxCompletionsPerPump = maxCompletionsPerPump;
        DefaultBackpressure = defaultBackpressure;
        PayloadDiagnostics = payloadDiagnostics;
        MaxSpecialPending = maxSpecialPending;
    }

    public static PostSchedulerOptions Default => new(
        readyCapacity: 1024,
        nextCapacity: 1024,
        maxEventsPerPump: 0,
        maxMillisecondsPerPump: 0,
        maxWavesPerPump: 1,
        timeCheckInterval: 64,
        maxCompletionsPerPump: 0,
        defaultBackpressure: BackpressurePolicy.RejectNew,
        payloadDiagnostics: PayloadDiagnosticsMode.Local);
}
