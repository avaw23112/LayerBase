namespace LayerBase.Actor;

internal sealed class EventMailPool<TEvent>
    where TEvent : struct
{
    private readonly RingQueueBuffer<TEvent> _buffer = new();

    public int Rent(int capacity)
    {
        return _buffer.Rent(capacity);
    }

    public int GetCapacity(int bufferId)
    {
        return _buffer.GetCapacity(bufferId);
    }

    public void Write(int bufferId, int index, in TEvent value)
    {
        _buffer.Write(bufferId, index, in value);
    }

    public TEvent Read(int bufferId, int index)
    {
        return _buffer.Read(bufferId, index);
    }

    public void Resize(int bufferId, int head, int count, int newCapacity)
    {
        _buffer.Resize(bufferId, head, count, newCapacity);
    }

    public void Release(int bufferId)
    {
        _buffer.Release(bufferId);
    }
}
