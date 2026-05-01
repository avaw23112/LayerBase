namespace LayerBase.Core.Event;

internal struct TimerEntry<TPayload>
{
    public TPayload Payload;
    public long ExpireTick;
    public long IntervalTicks;
    public int RemainingRepeatCount;
    public TimerFlags Flags;
    public int Version;
    public int Next;
    public int Prev;
    public int SlotIndex; // -1 表示在堆中，>= 0 表示在时间轮槽位中
}
