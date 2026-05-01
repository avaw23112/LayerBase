namespace LayerBase.Core.Event;

public enum BufferMode
{
    Latest,
    Queue
}

public enum BufferOverflowPolicy
{
    DropOldest,
    DropNewest,
    ReplaceLatest
}

public readonly struct EventBufferPolicy
{
    public readonly BufferMode Mode;
    public readonly float DefaultTtlSeconds;
    public readonly int Capacity;
    public readonly BufferOverflowPolicy OverflowPolicy;
    public readonly bool UseContractReplace;

    public EventBufferPolicy(
        BufferMode mode,
        float defaultTtlSeconds,
        int capacity,
        BufferOverflowPolicy overflowPolicy,
        bool useContractReplace)
    {
        Mode = mode;
        DefaultTtlSeconds = defaultTtlSeconds;
        Capacity = capacity;
        OverflowPolicy = overflowPolicy;
        UseContractReplace = useContractReplace;
    }
}
