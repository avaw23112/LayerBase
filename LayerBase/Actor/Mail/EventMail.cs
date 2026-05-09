namespace LayerBase.Actor;

internal struct EventMail<TEvent>
    where TEvent : struct
{
    public TEvent SingleValue;
    public int BufferId;
    public int Head;
    public int Count;
    public int Capacity;
}
