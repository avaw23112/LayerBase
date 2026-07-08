namespace LayerBase.ECS.Runtime;

internal sealed class EcsResultBatch
{
    private IEcsResultItem[] _items;
    private int _count;
    private int _readIndex;

    public EcsResultBatch(int capacity)
    {
        _items = new IEcsResultItem[Math.Max(1, capacity)];
    }

    public int Count => _count;

    public int RemainingCount => _count - _readIndex;

    public void Add(IEcsResultItem item)
    {
        int index = _count;
        if ((uint)index >= (uint)_items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }

        _items[index] = item;
        _count = index + 1;
    }

    public bool TryDequeue(out IEcsResultItem? item)
    {
        int index = _readIndex;
        if (index >= _count)
        {
            item = null;
            return false;
        }

        item = _items[index];
        _items[index] = null!;
        _readIndex = index + 1;
        return true;
    }

    public void Clear(bool disposeItems)
    {
        if (disposeItems)
        {
            for (int i = _readIndex; i < _count; i++)
            {
                if (_items[i] is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        Array.Clear(_items, 0, _count);
        _count = 0;
        _readIndex = 0;
    }
}
