namespace LayerBase.Actor;

internal static class ActorPostRouteCode
{
    public const byte WriteModeMask = 0b0000_0111;
    public const byte ValidationMask = 0b0011_0000;

    public const byte WriteQueuedGrow = 0b0000_0000;
    public const byte WriteQueuedRejectNew = 0b0000_0001;
    public const byte WriteQueuedDropOldest = 0b0000_0010;
    public const byte WriteLatest = 0b0000_0011;
    public const byte WriteDirty = 0b0000_0100;
    public const byte WriteDisabled = 0b0000_0101;

    public const byte ValidationPhysicalSafe = 0b0000_0000;
    public const byte ValidationPostableStamp = 0b0001_0000;
    public const byte ValidationUnchecked = 0b0010_0000;

    public const byte QueuedGrowPhysicalSafe = WriteQueuedGrow | ValidationPhysicalSafe;
    public const byte QueuedGrowPostableStamp = WriteQueuedGrow | ValidationPostableStamp;
    public const byte QueuedGrowUnchecked = WriteQueuedGrow | ValidationUnchecked;

    public const byte QueuedRejectNewPhysicalSafe = WriteQueuedRejectNew | ValidationPhysicalSafe;
    public const byte QueuedRejectNewPostableStamp = WriteQueuedRejectNew | ValidationPostableStamp;
    public const byte QueuedRejectNewUnchecked = WriteQueuedRejectNew | ValidationUnchecked;

    public const byte QueuedDropOldestPhysicalSafe = WriteQueuedDropOldest | ValidationPhysicalSafe;
    public const byte QueuedDropOldestPostableStamp = WriteQueuedDropOldest | ValidationPostableStamp;
    public const byte QueuedDropOldestUnchecked = WriteQueuedDropOldest | ValidationUnchecked;

    public const byte LatestPhysicalSafe = WriteLatest | ValidationPhysicalSafe;
    public const byte LatestPostableStamp = WriteLatest | ValidationPostableStamp;
    public const byte LatestUnchecked = WriteLatest | ValidationUnchecked;

    public const byte DirtyPhysicalSafe = WriteDirty | ValidationPhysicalSafe;
    public const byte DirtyPostableStamp = WriteDirty | ValidationPostableStamp;
    public const byte DirtyUnchecked = WriteDirty | ValidationUnchecked;

    public const byte Disabled = WriteDisabled | ValidationPhysicalSafe;
}
