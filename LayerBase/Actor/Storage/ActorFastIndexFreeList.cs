namespace LayerBase.Actor;

internal struct ActorFastIndexFreeList
{
    private int[] _items;
    private int _count;

    public ActorFastIndexFreeList(int initialCapacity)
    {
        _items = new int[Math.Max(initialCapacity, 4)];
        _count = 0;
    }

    public bool TryPop(out int fastIndex)
    {
        if (_count == 0)
        {
            fastIndex = default;
            return false;
        }

        _count--;
        fastIndex = _items[_count];
        return true;
    }

    public void Push(int fastIndex)
    {
        if (_count == _items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }

        _items[_count] = fastIndex;
        _count++;
    }
}
