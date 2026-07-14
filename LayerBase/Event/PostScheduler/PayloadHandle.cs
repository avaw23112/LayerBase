namespace LayerBase.Core.Event;

public readonly struct PayloadHandle
{
    public readonly int EventTypeId;
    public readonly int Index;
    public readonly int Version;
    public readonly int StoreId;

    public PayloadHandle(int eventTypeId, int index, int version)
        : this(eventTypeId, index, version, 0)
    {
    }

    internal PayloadHandle(int eventTypeId, int index, int version, int storeId)
    {
        EventTypeId = eventTypeId;
        Index = index;
        Version = version;
        StoreId = storeId;
    }

    public bool IsInvalid => Index < 0;
    public static PayloadHandle Invalid => new(-1, -1, 0);
}
