using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

internal sealed class EventMailPool<TEvent>
    where TEvent : struct
{
    private readonly RingQueueBuffer<TEvent> _buffer = new();
    private readonly ActorMailOptions _options;

    public EventMailPool()
    {
        _options = default;
    }

    public EventMailPool(ActorMailOptions options)
    {
        _options = options;
    }

    public int Rent(int capacity)
    {
        return _buffer.Rent(capacity);
    }

    public int RentInitial()
    {
        return _buffer.Rent(_options.InitialCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventMailRentResult<TEvent> RentWithBuffer(int capacity)
    {
        return _buffer.RentWithBuffer(capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventMailRentResult<TEvent> RentInitialWithBuffer()
    {
        return _buffer.RentWithBuffer(_options.InitialCapacity);
    }

    public int GetCapacity(int bufferId)
    {
        return _buffer.GetCapacity(bufferId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int bufferId, int index, in TEvent value)
    {
        _buffer.Write(bufferId, index, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TEvent Read(int bufferId, int index)
    {
        return _buffer.Read(bufferId, index);
    }

    public void Resize(int bufferId, int head, int count, int newCapacity)
    {
        _buffer.Resize(bufferId, head, count, newCapacity);
    }

    public bool TryGrow(ref EventMail<TEvent> mail)
    {
        if (mail.Capacity >= _options.MaxCapacity)
        {
            return false;
        }

        int growFactor = Math.Max(_options.GrowFactor, 2);
        int nextCapacity = mail.Capacity * growFactor;
        if (nextCapacity <= mail.Capacity)
        {
            nextCapacity = mail.Capacity + 1;
        }

        nextCapacity = Math.Min(nextCapacity, _options.MaxCapacity);
        if (nextCapacity <= mail.Capacity)
        {
            return false;
        }

        TEvent[] newBuffer = _buffer.ResizeWithBuffer(mail.BufferId, mail.Head, mail.Count, nextCapacity);

        mail.Buffer = newBuffer;
        mail.Head = 0;
        mail.Tail = mail.Count;
        mail.Capacity = newBuffer.Length;
        return true;
    }

    public void Release(int bufferId)
    {
        _buffer.Release(bufferId);
    }
}
