namespace LayerBase.Core.Event;

public readonly struct TimerHandle
{
    public readonly int Index;
    public readonly int Version;

    public TimerHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }
    
    public bool IsInvalid => Index < 0;
    public static TimerHandle Invalid => new(-1, 0);
}
