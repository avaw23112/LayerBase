namespace LayerBase.Core.Event;

internal struct DelayExpireEntry
{
    public int PublisherId;
    public int ValueVersion;
    public long ExpireTick;
    public int Next;
    public int Prev;
    public int SlotIndex;
    public int EntryVersion;
    public bool Active;
}
