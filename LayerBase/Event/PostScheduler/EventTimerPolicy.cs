namespace LayerBase.Core.Event;

public readonly struct EventTimerPolicy
{
    public readonly TimerRepeatMode RepeatMode;
    public readonly TimerCatchUpPolicy CatchUpPolicy;
    public readonly int MaxCatchUpPerTick;
    public readonly bool PreferLongTimerHeap;
    public readonly EventPostPolicy? ExpiredPostPolicy;

    public EventTimerPolicy(
        TimerRepeatMode    repeatMode,
        TimerCatchUpPolicy catchUpPolicy,
        int                maxCatchUpPerTick,
        bool               preferLongTimerHeap,
        EventPostPolicy?   expiredPostPolicy)
    {
        RepeatMode = repeatMode;
        CatchUpPolicy = catchUpPolicy;
        MaxCatchUpPerTick = maxCatchUpPerTick;
        PreferLongTimerHeap = preferLongTimerHeap;
        ExpiredPostPolicy = expiredPostPolicy;
    }
}