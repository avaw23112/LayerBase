namespace LayerBase.Actor;

internal struct ActorSlotFreeList
{
    private int[] _items;
    private int _count;

    public ActorSlotFreeList(int initialCapacity)
    {
        _items = new int[Math.Max(initialCapacity, 4)];
        _count = 0;
    }

    public bool TryPop(out int slotIndex)
    {
        if (_count == 0)
        {
            slotIndex = default;
            return false;
        }

        _count--;
        slotIndex = _items[_count];
        return true;
    }

    public void Push(int slotIndex)
    {
        if (_count == _items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }

        _items[_count] = slotIndex;
        _count++;
    }
}
