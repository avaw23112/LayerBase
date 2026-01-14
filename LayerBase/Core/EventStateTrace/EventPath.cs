using System;

namespace LayerBase.Core.EventStateTrace;

/// <summary>
/// 存储在池中的路径切片。
/// </summary>
internal struct EventPath
{
    public int[]? Buffer;
    public int Offset;
    public int Length;
    public ushort Version;

    public bool HasValue => Buffer != null && Length > 0;

    public ReadOnlySpan<int> AsSpan()
    {
        return Buffer == null
            ? ReadOnlySpan<int>.Empty
            : new ReadOnlySpan<int>(Buffer, Offset, Length);
    }
}
