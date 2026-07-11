namespace LayerBase.ECS.Runtime.Submission;

internal sealed class EcsSubmissionBatch
{
    private IEcsWorkItem?[] _items;
    private int _count;

    public EcsSubmissionBatch(int capacity)
    {
        _items = new IEcsWorkItem?[Math.Max(1, capacity)];
    }

    public int Count => _count;

    public long Sequence { get; set; }

    public void Add(IEcsWorkItem item)
    {
        int index = _count;
        if ((uint)index >= (uint)_items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }

        _items[index] = item;
        _count = index + 1;
    }

    public void EnsureCapacity(int capacity)
    {
        if (capacity > _items.Length)
        {
            Array.Resize(ref _items, capacity);
        }
    }

    public ReadOnlySpan<IEcsWorkItem?> AsSpan()
    {
        return _items.AsSpan(0, _count);
    }

    public void Clear()
    {
        Array.Clear(_items, 0, _count);
        _count = 0;
        Sequence = 0;
    }
}
