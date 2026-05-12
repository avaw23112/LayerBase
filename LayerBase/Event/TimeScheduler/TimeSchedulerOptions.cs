namespace LayerBase.Core.Event;

public readonly struct TimeSchedulerOptions
{
    public readonly float TickDurationSeconds;
    public readonly int WheelSize;
    public readonly int InitialTimerCapacity;
    public readonly float LongTimerThresholdSeconds;
    public readonly int MaxExpiredPerTick;
    public readonly int MaxPromotePerTick;
    public readonly TimerRepeatMode DefaultRepeatMode;
    public readonly TimerCatchUpPolicy DefaultCatchUpPolicy;

    public TimeSchedulerOptions(
        float              tickDurationSeconds,
        int                wheelSize,
        int                initialTimerCapacity,
        float              longTimerThresholdSeconds,
        int                maxExpiredPerTick,
        int                maxPromotePerTick,
        TimerRepeatMode    defaultRepeatMode,
        TimerCatchUpPolicy defaultCatchUpPolicy)
    {
        TickDurationSeconds = tickDurationSeconds;
        WheelSize = wheelSize;
        InitialTimerCapacity = initialTimerCapacity;
        LongTimerThresholdSeconds = longTimerThresholdSeconds;
        MaxExpiredPerTick = maxExpiredPerTick;
        MaxPromotePerTick = maxPromotePerTick;
        DefaultRepeatMode = defaultRepeatMode;
        DefaultCatchUpPolicy = defaultCatchUpPolicy;
    }

    public static TimeSchedulerOptions Default => new(
        tickDurationSeconds: 1 / 60f,
        wheelSize: 512,
        initialTimerCapacity: 256,
        longTimerThresholdSeconds: 512 * (1 / 60f),
        maxExpiredPerTick: 1024,
        maxPromotePerTick: 64,
        defaultRepeatMode: TimerRepeatMode.Once,
        defaultCatchUpPolicy: TimerCatchUpPolicy.SkipMissed);
}