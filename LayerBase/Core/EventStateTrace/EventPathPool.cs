using System;
using System.Buffers;
using System.Threading;

namespace LayerBase.Core.EventStateTrace;

/// <summary>
/// 路径段池化，避免每个事件都分配独立数组。
/// </summary>
internal sealed class EventPathPool
{
    private readonly ArrayPool<int> _pool;
    private readonly bool _clearOnReturn;
    private int _versionSeed;

    public EventPathPool(ArrayPool<int>? pool = null, bool clearOnReturn = false)
    {
        _pool = pool ?? ArrayPool<int>.Shared;
        _clearOnReturn = clearOnReturn;
        _versionSeed = 0;
    }

    public EventPath Rent(ReadOnlySpan<int> segments)
    {
        if (segments.Length == 0)
        {
            return default;
        }

        int[] buffer = _pool.Rent(segments.Length);
        segments.CopyTo(buffer);

        return new EventPath
        {
            Buffer = buffer,
            Offset = 0,
            Length = segments.Length,
            Version = NextVersion()
        };
    }

    public void Return(in EventPath path)
    {
        if (path.Buffer == null)
        {
            return;
        }

        _pool.Return(path.Buffer, _clearOnReturn);
    }

    private ushort NextVersion()
    {
        int next = Interlocked.Increment(ref _versionSeed);
        ushort v = (ushort)next;
        if (v == 0)
        {
            v = (ushort)Interlocked.Increment(ref _versionSeed);
        }
        return v;
    }
}
