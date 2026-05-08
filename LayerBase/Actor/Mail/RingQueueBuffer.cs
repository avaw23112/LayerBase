namespace LayerBase.Actor;

internal sealed class RingQueueBuffer<TEvent>
    where TEvent : struct
{
    private TEvent[]?[] _buffers = new TEvent[4][];
    private readonly Stack<int> _freeIds = new();

    public int Rent(int initialCapacity)
    {
        TEvent[] buffer = new TEvent[Math.Max(initialCapacity, 1)];

        if (_freeIds.Count > 0)
        {
            int reusedId = _freeIds.Pop();
            _buffers[reusedId - 1] = buffer;
            return reusedId;
        }

        int id = 1;
        while (id <= _buffers.Length && _buffers[id - 1] != null)
        {
            id++;
        }

        if (id > _buffers.Length)
        {
            Array.Resize(ref _buffers, _buffers.Length * 2);
        }

        _buffers[id - 1] = buffer;
        return id;
    }

    public int GetCapacity(int bufferId)
    {
        return GetBuffer(bufferId).Length;
    }

    public void Write(int bufferId, int index, in TEvent value)
    {
        GetBuffer(bufferId)[index] = value;
    }

    public TEvent Read(int bufferId, int index)
    {
        return GetBuffer(bufferId)[index];
    }

    public void Release(int bufferId)
    {
        if (bufferId <= 0 || bufferId > _buffers.Length || _buffers[bufferId - 1] == null)
        {
            return;
        }

        _buffers[bufferId - 1] = null;
        _freeIds.Push(bufferId);
    }

    public void Resize(int bufferId, int head, int count, int newCapacity)
    {
        if (newCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newCapacity));
        }

        TEvent[] oldBuffer = GetBuffer(bufferId);
        var newBuffer = new TEvent[newCapacity];

        for (int i = 0; i < count; i++)
        {
            newBuffer[i] = oldBuffer[(head + i) % oldBuffer.Length];
        }

        _buffers[bufferId - 1] = newBuffer;
    }

    private TEvent[] GetBuffer(int bufferId)
    {
        if (bufferId <= 0 || bufferId > _buffers.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferId));
        }

        return _buffers[bufferId - 1] ?? throw new InvalidOperationException("Buffer is not allocated.");
    }
}
