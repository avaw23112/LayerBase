namespace LayerBase.Actor;

internal sealed class DirtyBucketList
{
    private int[] _items;
    private bool[] _contains;
    private int _head;
    private int _count;

    public int Count => _count;

    public DirtyBucketList(int initialCapacity = 4)
    {
        int capacity = Math.Max(initialCapacity, 4);
        _items = new int[capacity];
        _contains = new bool[capacity];
    }

    public void AddIfNotExists(int bucketIndex)
    {
        EnsureContainsCapacity(bucketIndex + 1);
        if (_contains[bucketIndex])
        {
            return;
        }

        _contains[bucketIndex] = true;
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
        if ((uint)bucketIndex < (uint)_contains.Length)
        {
            _contains[bucketIndex] = false;
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

    private void EnsureContainsCapacity(int required)
    {
        if (required <= _contains.Length)
        {
            return;
        }

        int newCapacity = _contains.Length;
        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        Array.Resize(ref _contains, newCapacity);
    }
}
