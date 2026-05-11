namespace LayerBase.Actor;

internal sealed class DirtyBucketList
{
    private int[] _items;
    private int[] _marks;
    private int _head;
    private int _count;
    private int _stamp = 1;

    public int Count => _count;

    public DirtyBucketList(int initialCapacity = 4)
    {
        int capacity = Math.Max(initialCapacity, 4);
        _items = new int[capacity];
        _marks = new int[capacity];
    }

    public void Mark(int bucketIndex)
    {
        EnsureMarkCapacity(bucketIndex + 1);
        if (_marks[bucketIndex] == _stamp)
        {
            return;
        }

        _marks[bucketIndex] = _stamp;
        EnsureItemCapacity(_count + 1);

        int tail = (_head + _count) % _items.Length;
        _items[tail] = bucketIndex;
        _count++;
    }

    public bool TryPeek(out int bucketIndex)
    {
        if (_count == 0)
        {
            bucketIndex = default;
            return false;
        }

        bucketIndex = _items[_head];
        return true;
    }

    public void Pop()
    {
        if (_count == 0)
        {
            return;
        }

        int bucketIndex = _items[_head];
        if ((uint)bucketIndex < (uint)_marks.Length)
        {
            _marks[bucketIndex] = 0;
        }

        _head = (_head + 1) % _items.Length;
        _count--;

        if (_count == 0)
        {
            _head = 0;
        }
    }

    public void MoveHeadToTail()
    {
        if (_count <= 1)
        {
            return;
        }

        int value = _items[_head];
        _head = (_head + 1) % _items.Length;

        int tail = (_head + _count - 1) % _items.Length;
        _items[tail] = value;
    }

    public void Clear()
    {
        _head = 0;
        _count = 0;
        _stamp++;
        if (_stamp == int.MaxValue)
        {
            Array.Clear(_marks, 0, _marks.Length);
            _stamp = 1;
        }
    }

    private void EnsureItemCapacity(int required)
    {
        if (required <= _items.Length)
        {
            return;
        }

        int newCapacity = _items.Length;
        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        int[] newItems = new int[newCapacity];
        for (int i = 0; i < _count; i++)
        {
            newItems[i] = _items[(_head + i) % _items.Length];
        }

        _items = newItems;
        _head = 0;
    }

    private void EnsureMarkCapacity(int required)
    {
        if (required <= _marks.Length)
        {
            return;
        }

        int newCapacity = _marks.Length;
        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        Array.Resize(ref _marks, newCapacity);
    }
}
