namespace LayerBase.Actor;

internal sealed class RingQueueBuffer<TEvent>
    where TEvent : struct
{
    private TEvent[]?[] _buffers = new TEvent[4][];
    private bool[] _inUse = new bool[4];
    private readonly Stack<int> _freeIds = new();

    public int Rent(int initialCapacity)
    {
        int capacity = ActorMailCapacity.NormalizePowerOfTwo(Math.Max(initialCapacity, 1));

        if (_freeIds.Count > 0)
        {
            int reusedId = _freeIds.Pop();
            int index = reusedId - 1;
            TEvent[]? buffer = _buffers[index];
            if (buffer == null || buffer.Length < capacity)
            {
                buffer = new TEvent[capacity];
                _buffers[index] = buffer;
            }

            _inUse[index] = true;
            return reusedId;
        }

        int id = 1;
        while (id <= _buffers.Length && _buffers[id - 1] != null)
        {
            id++;
        }

        if (id > _buffers.Length)
        {
            int newLength = _buffers.Length * 2;
            Array.Resize(ref _buffers, newLength);
            Array.Resize(ref _inUse, newLength);
        }

        _buffers[id - 1] = new TEvent[capacity];
        _inUse[id - 1] = true;
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
        if (bufferId <= 0 || bufferId > _buffers.Length)
        {
            return;
        }

        int index = bufferId - 1;
        if (!_inUse[index])
        {
            return;
        }

        _inUse[index] = false;
        _freeIds.Push(bufferId);
    }

    public void Resize(int bufferId, int head, int count, int newCapacity)
    {
        int capacity = ActorMailCapacity.NormalizePowerOfTwo(newCapacity);
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newCapacity));
        }

        TEvent[] oldBuffer = GetBuffer(bufferId);
        var newBuffer = new TEvent[capacity];
        int mask = oldBuffer.Length - 1;

        for (int i = 0; i < count; i++)
        {
            newBuffer[i] = oldBuffer[(head + i) & mask];
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
