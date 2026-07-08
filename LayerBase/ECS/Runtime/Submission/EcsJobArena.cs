using System.Runtime.CompilerServices;

namespace LayerBase.ECS.Runtime.Submission;

internal sealed class EcsJobArena
{
    private byte[] _buffer;
    private int _offset;

    public EcsJobArena(int capacity)
    {
        _buffer = new byte[Math.Max(64, capacity)];
    }

    public int Store<TJob>(in TJob job)
        where TJob : struct
    {
        int size = Unsafe.SizeOf<TJob>();
        int offset = Align(_offset, Math.Min(size, 16));
        EnsureCapacity(offset + size);

        Unsafe.WriteUnaligned(ref _buffer[offset], job);
        _offset = offset + size;
        return offset;
    }

    public ref TJob Get<TJob>(int offset)
        where TJob : struct
    {
        return ref Unsafe.As<byte, TJob>(ref _buffer[offset]);
    }

    public void Reset()
    {
        _offset = 0;
    }

    private static int Align(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    public void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length)
        {
            return;
        }

        Array.Resize(ref _buffer, Math.Max(required, _buffer.Length * 2));
    }
}
