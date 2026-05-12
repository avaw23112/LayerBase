namespace LayerBase.Core.Event;

public readonly struct DelayTimerHandle
{
    public readonly int Index;
    public readonly int Version;

    public DelayTimerHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }

    public bool IsValid => Index >= 0;
    public static readonly DelayTimerHandle Invalid = new DelayTimerHandle(-1, 0);
}