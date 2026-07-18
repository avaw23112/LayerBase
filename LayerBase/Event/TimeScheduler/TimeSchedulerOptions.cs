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
    public readonly int MaxCatchUpTicksPerPump;

    public TimeSchedulerOptions(
        float              tickDurationSeconds,
        int                wheelSize,
        int                initialTimerCapacity,
        float              longTimerThresholdSeconds,
        int                maxExpiredPerTick,
        int                maxPromotePerTick,
        TimerRepeatMode    defaultRepeatMode,
        TimerCatchUpPolicy defaultCatchUpPolicy,
        int                maxCatchUpTicksPerPump = 8)
    {
        if (tickDurationSeconds <= 0 || float.IsNaN(tickDurationSeconds) || float.IsInfinity(tickDurationSeconds))
            throw new ArgumentException(nameof(tickDurationSeconds));
        if (initialTimerCapacity <= 0 || initialTimerCapacity > (1 << 30))
            throw new ArgumentException(nameof(initialTimerCapacity));
        if (maxPromotePerTick <= 0)
            throw new ArgumentException(nameof(maxPromotePerTick));
        if (maxExpiredPerTick <= 0)
            throw new ArgumentException(nameof(maxExpiredPerTick));

        TickDurationSeconds = tickDurationSeconds;
        WheelSize = wheelSize;
        InitialTimerCapacity = initialTimerCapacity;
        LongTimerThresholdSeconds = longTimerThresholdSeconds;
        MaxExpiredPerTick = maxExpiredPerTick;
        MaxPromotePerTick = maxPromotePerTick;
        DefaultRepeatMode = defaultRepeatMode;
        DefaultCatchUpPolicy = defaultCatchUpPolicy;
        MaxCatchUpTicksPerPump = maxCatchUpTicksPerPump;
    }

    public static TimeSchedulerOptions Default => new(
        tickDurationSeconds: 1 / 60f,
        wheelSize: 512,
        initialTimerCapacity: 256,
        longTimerThresholdSeconds: 512 * (1 / 60f),
        maxExpiredPerTick: 1024,
        maxPromotePerTick: 64,
        defaultRepeatMode: TimerRepeatMode.Once,
        defaultCatchUpPolicy: TimerCatchUpPolicy.SkipMissed,
        maxCatchUpTicksPerPump: 8);
}