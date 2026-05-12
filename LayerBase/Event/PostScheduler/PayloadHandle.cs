namespace LayerBase.Core.Event;

public readonly struct PayloadHandle
{
    public readonly int EventTypeId;
    public readonly int Index;
    public readonly int Version;

    public PayloadHandle(int eventTypeId, int index, int version)
    {
        EventTypeId = eventTypeId;
        Index = index;
        Version = version;
    }

    public bool IsInvalid => Index < 0;
    public static PayloadHandle Invalid => new(-1, -1, 0);
}