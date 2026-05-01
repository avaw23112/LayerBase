namespace LayerBase.Core.Event;

public readonly struct PostItem
{
    public readonly int EventTypeId;
    public readonly PayloadHandle PayloadHandle;
    public readonly long SequenceId;
    public readonly BackpressurePolicy Policy;

    public PostItem(
        int eventTypeId,
        PayloadHandle payloadHandle,
        long sequenceId,
        BackpressurePolicy policy)
    {
        EventTypeId = eventTypeId;
        PayloadHandle = payloadHandle;
        SequenceId = sequenceId;
        Policy = policy;
    }
}
