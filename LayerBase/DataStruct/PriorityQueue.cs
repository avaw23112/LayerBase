namespace LayerBase.Core.DataStruct;

public class PriorityQueue<TElement, TPriority>
{
    private (TElement Element, TPriority Priority)[] _nodes;
    private int _size;
    private readonly IComparer<TPriority> _comparer;

    public PriorityQueue(int initialCapacity = 16, IComparer<TPriority>? comparer = null)
    {
        _nodes = new (TElement, TPriority)[initialCapacity];
        _comparer = comparer ?? Comparer<TPriority>.Default;
    }

    public int Count => _size;

    public void Enqueue(TElement element, TPriority priority)
    {
        if (_size == _nodes.Length) Array.Resize(ref _nodes, _size == 0 ? 4 : _size * 2);
        _nodes[_size] = (element, priority);
        HeapifyUp(_size++);
    }

    public TElement Dequeue()
    {
        if (_size == 0) throw new InvalidOperationException("Queue is empty");
        var element = _nodes[0].Element;
        _nodes[0] = _nodes[--_size];
        _nodes[_size] = default;
        HeapifyDown(0);
        return element;
    }

    public bool TryPeek(out TElement element, out TPriority priority)
    {
        if (_size == 0)
        {
            element = default!;
            priority = default!;
            return false;
        }
        element = _nodes[0].Element;
        priority = _nodes[0].Priority;
        return true;
    }

    public void Clear()
    {
        Array.Clear(_nodes, 0, _size);
        _size = 0;
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_comparer.Compare(_nodes[index].Priority, _nodes[parent].Priority) >= 0) break;
            Swap(index, parent);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int smallest = index;
            int left = 2 * index + 1;
            int right = 2 * index + 2;

            if (left < _size && _comparer.Compare(_nodes[left].Priority, _nodes[smallest].Priority) < 0) smallest = left;
            if (right < _size && _comparer.Compare(_nodes[right].Priority, _nodes[smallest].Priority) < 0) smallest = right;

            if (smallest == index) break;
            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int i, int j)
    {
        var temp = _nodes[i];
        _nodes[i] = _nodes[j];
        _nodes[j] = temp;
    }
}
