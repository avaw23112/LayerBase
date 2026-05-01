namespace LayerBase.Core.Event;

public readonly struct CoalescedSlotKey : IEquatable<CoalescedSlotKey>
{
    public readonly int EventTypeId;
    public readonly int CoalesceKey;

    public CoalescedSlotKey(int eventTypeId, int coalesceKey)
    {
        EventTypeId = eventTypeId;
        CoalesceKey = coalesceKey;
    }

    public bool Equals(CoalescedSlotKey other)
    {
        return EventTypeId == other.EventTypeId && CoalesceKey == other.CoalesceKey;
    }

    public override bool Equals(object? obj) => obj is CoalescedSlotKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(EventTypeId, CoalesceKey);

    public static bool operator ==(CoalescedSlotKey left, CoalescedSlotKey right) => left.Equals(right);
    public static bool operator !=(CoalescedSlotKey left, CoalescedSlotKey right) => !left.Equals(right);
}

public struct CoalescedSlot
{
    public CoalescedSlotKey Key;
    public PayloadHandle PayloadHandle;
    public long FirstSequenceId;
    public long LastSequenceId;
    public int MergeCount;
    public bool Active;
}
