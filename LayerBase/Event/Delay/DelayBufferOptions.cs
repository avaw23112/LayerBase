namespace LayerBase.Core.Event;

public readonly struct DelayBufferOptions
{
    public readonly float TickDurationSeconds;
    public readonly int WheelSize;
    public readonly int InitialCapacity;
    public readonly int MaxExpiredPerTick;

    public DelayBufferOptions(
        float tickDurationSeconds,
        int   wheelSize,
        int   initialCapacity,
        int   maxExpiredPerTick)
    {
        TickDurationSeconds = tickDurationSeconds;
        WheelSize = wheelSize;
        InitialCapacity = initialCapacity;
        MaxExpiredPerTick = maxExpiredPerTick;
    }

    public static DelayBufferOptions Default => new(1 / 60f, 64, 256, 1024);
}