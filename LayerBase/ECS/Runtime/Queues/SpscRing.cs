using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LayerBase.ECS.Runtime.Queues;

[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct PaddedInt
{
    [FieldOffset(0)]
    public int Value;
}

internal sealed class SpscRing<T>
    where T : class
{
    private readonly T?[] _buffer;
    private readonly int _mask;

    private PaddedInt _head;
    private PaddedInt _tail;

    public SpscRing(int capacityPowerOfTwo)
    {
        if (capacityPowerOfTwo <= 0 ||
            (capacityPowerOfTwo & (capacityPowerOfTwo - 1)) != 0)
        {
            throw new ArgumentException("Capacity must be a positive power of two.", nameof(capacityPowerOfTwo));
        }

        _buffer = new T?[capacityPowerOfTwo];
        _mask = capacityPowerOfTwo - 1;
    }

    public int Count => Volatile.Read(ref _tail.Value) - Volatile.Read(ref _head.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnqueue(T item)
    {
        int tail = _tail.Value;
        int next = tail + 1;
        int head = Volatile.Read(ref _head.Value);

        if (next - head > _buffer.Length)
        {
            return false;
        }

        _buffer[tail & _mask] = item;
        Volatile.Write(ref _tail.Value, next);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue(out T? item)
    {
        int head = _head.Value;
        int tail = Volatile.Read(ref _tail.Value);

        if (head == tail)
        {
            item = null;
            return false;
        }

        int index = head & _mask;
        item = _buffer[index];
        _buffer[index] = null;
        Volatile.Write(ref _head.Value, head + 1);
        return item != null;
    }
}
