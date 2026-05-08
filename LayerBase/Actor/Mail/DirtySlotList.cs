namespace LayerBase.Actor;

internal sealed class DirtySlotList
{
    private int[] _items = new int[4];
    private int _head;
    private int _count;
    private readonly HashSet<int> _contains = new();

    public void AddIfNotExists(int slotIndex)
    {
        if (!_contains.Add(slotIndex))
        {
            return;
        }

        EnsureCapacity(_count + 1);
        int tail = (_head + _count) % _items.Length;
        _items[tail] = slotIndex;
        _count++;
    }

    public bool TryPeek(out int slotIndex)
    {
        if (_count == 0)
        {
            slotIndex = default;
            return false;
        }

        slotIndex = _items[_head];
        return true;
    }

    public void Pop()
    {
        if (_count == 0)
        {
            return;
        }

        _contains.Remove(_items[_head]);
        _head = (_head + 1) % _items.Length;
        _count--;

        if (_count == 0)
        {
            _head = 0;
        }
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _items.Length)
        {
            return;
        }

        int[] newItems = new int[_items.Length * 2];
        for (int i = 0; i < _count; i++)
        {
            newItems[i] = _items[(_head + i) % _items.Length];
        }

        _items = newItems;
        _head = 0;
    }
}
