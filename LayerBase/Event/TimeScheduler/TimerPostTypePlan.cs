namespace LayerBase.Core.Event;

internal readonly struct TimerPostTypePlan
{
    public TimerPostTypePlan(
        int eventTypeId,
        bool hasExpiredOverride,
        PostTypePlan expiredPlan,
        TimerRepeatMode? repeatMode,
        TimerCatchUpPolicy? catchUpPolicy)
    {
        EventTypeId = eventTypeId;
        HasExpiredOverride = hasExpiredOverride;
        ExpiredPlan = expiredPlan;
        RepeatMode = repeatMode;
        CatchUpPolicy = catchUpPolicy;
    }

    public int EventTypeId { get; }
    public bool HasExpiredOverride { get; }
    public PostTypePlan ExpiredPlan { get; }
    public TimerRepeatMode? RepeatMode { get; }
    public TimerCatchUpPolicy? CatchUpPolicy { get; }
}
