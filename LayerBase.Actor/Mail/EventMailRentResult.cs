namespace LayerBase.Actor;

internal readonly struct EventMailRentResult<TEvent>
    where TEvent : struct
{
    public readonly int BufferId;
    public readonly TEvent[] Buffer;

    public EventMailRentResult(int bufferId, TEvent[] buffer)
    {
        BufferId = bufferId;
        Buffer = buffer;
    }
}
