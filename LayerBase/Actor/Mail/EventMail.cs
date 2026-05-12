namespace LayerBase.Actor;

internal struct EventMail<TEvent>
    where TEvent : struct
{
    public TEvent SingleValue;
    public int BufferId;
    public TEvent[]? Buffer;
    public int Head;
    public int Tail;
    public int Count;
    public int Capacity;
}