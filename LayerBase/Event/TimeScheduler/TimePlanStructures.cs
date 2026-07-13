using System.Runtime.CompilerServices;

namespace LayerBase.Core.Event;

public readonly struct TimerWheelPlan
{
    public readonly int WheelSize;
    public readonly int WheelMask;
    public readonly float TickDurationSeconds;
    public readonly float TickDurationReciprocal;

    public TimerWheelPlan(int wheelSize, float tickDurationSeconds)
    {
        if (wheelSize <= 0) throw new ArgumentOutOfRangeException(nameof(wheelSize));
        if ((wheelSize & (wheelSize - 1)) != 0)
        {
            int p = 1;
            while (p < wheelSize) p <<= 1;
            wheelSize = p;
        }

        WheelSize = wheelSize;
        WheelMask = wheelSize - 1;
        TickDurationSeconds = tickDurationSeconds;
        TickDurationReciprocal = 1f / tickDurationSeconds;
    }
}

[Flags]
public enum TimerFlags : byte
{
    None = 0,
    Active = 1 << 0,
    Repeat = 1 << 1,
    FixedRate = 1 << 2,
    FixedDelay = 1 << 3,
    CatchUp = 1 << 4
}

internal sealed class LongTimerHeap
{
    private int[] _indices;
    private long[] _expireTicks;
    private int[] _versions;
    private int[] _positions;
    private int _count;

    public int Count => _count;

    public LongTimerHeap(int capacity)
    {
        _indices = new int[capacity];
        _expireTicks = new long[capacity];
        _versions = new int[capacity];
        _positions = new int[capacity];
        Array.Fill(_positions, -1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(int timerIndex, long expireTick, int version)
    {
        if (_count == _indices.Length) GrowSlow();
        EnsurePositionCapacity(timerIndex);

        var i = _count++;
        _indices[i] = timerIndex;
        _expireTicks[i] = expireTick;
        _versions[i] = version;
        _positions[timerIndex] = i;
        HeapifyUp(i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(out int timerIndex, out long expireTick, out int version)
    {
        if (_count == 0)
        {
            timerIndex = -1;
            expireTick = 0;
            version = 0;
            return false;
        }

        timerIndex = _indices[0];
        expireTick = _expireTicks[0];
        version = _versions[0];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Dequeue()
    {
        var result = _indices[0];
        _positions[result] = -1;
        _count--;
        if (_count > 0)
        {
            _indices[0] = _indices[_count];
            _expireTicks[0] = _expireTicks[_count];
            _versions[0] = _versions[_count];
            _positions[_indices[0]] = 0;
            HeapifyDown(0);
        }

        return result;
    }

    public bool Remove(int timerIndex, int version)
    {
        if ((uint)timerIndex >= (uint)_positions.Length)
        {
            return false;
        }

        int position = _positions[timerIndex];
        if ((uint)position >= (uint)_count ||
            _indices[position] != timerIndex ||
            _versions[position] != version)
        {
            return false;
        }

        _positions[timerIndex] = -1;
        _count--;
        if (position == _count)
        {
            return true;
        }

        _indices[position] = _indices[_count];
        _expireTicks[position] = _expireTicks[_count];
        _versions[position] = _versions[_count];
        _positions[_indices[position]] = position;

        int afterDown = HeapifyDown(position);
        HeapifyUp(afterDown);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowSlow()
    {
        Array.Resize(ref _indices, _indices.Length * 2);
        Array.Resize(ref _expireTicks, _expireTicks.Length * 2);
        Array.Resize(ref _versions, _versions.Length * 2);
    }

    private void EnsurePositionCapacity(int timerIndex)
    {
        if (timerIndex < _positions.Length)
        {
            return;
        }

        int oldLength = _positions.Length;
        int newLength = oldLength;
        while (newLength <= timerIndex)
        {
            newLength *= 2;
        }

        Array.Resize(ref _positions, newLength);
        Array.Fill(_positions, -1, oldLength, newLength - oldLength);
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            var parent = (index - 1) >> 1;
            if (_expireTicks[index] >= _expireTicks[parent]) break;
            Swap(index, parent);
            index = parent;
        }
    }

    private int HeapifyDown(int index)
    {
        while (true)
        {
            var left = (index << 1) + 1;
            if (left >= _count) break;

            var right = left + 1;
            var smallest = left;

            if (right < _count && _expireTicks[right] < _expireTicks[left])
                smallest = right;

            if (_expireTicks[index] <= _expireTicks[smallest]) break;

            Swap(index, smallest);
            index = smallest;
        }

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Swap(int a, int b)
    {
        (_indices[a], _indices[b]) = (_indices[b], _indices[a]);
        (_expireTicks[a], _expireTicks[b]) = (_expireTicks[b], _expireTicks[a]);
        (_versions[a], _versions[b]) = (_versions[b], _versions[a]);
        _positions[_indices[a]] = a;
        _positions[_indices[b]] = b;
    }

    public void Clear()
    {
        for (int i = 0; i < _count; i++)
        {
            _positions[_indices[i]] = -1;
        }

        _count = 0;
    }
}

internal sealed class IntStack
{
    private int[] _items;
    private int _count;

    public int Count => _count;

    public IntStack(int capacity)
    {
        _items = new int[capacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(int value)
    {
        if (_count == _items.Length) GrowSlow();
        _items[_count++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Pop() => _items[--_count];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowSlow() => Array.Resize(ref _items, _items.Length * 2);

    public void Clear() => _count = 0;
}
