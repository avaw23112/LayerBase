using System.Buffers;
using Arch.Core;

namespace LayerBase.ECS.Projection;

/// <summary>
/// DirtyProjectionSet 用于保存需要执行 Post 投影的 Entity。
///
/// 该结构不负责 Touch 保活。
/// 该结构不负责去重。
/// 去重由 DirtyTag / DirtyVersion 或调用方保证。
/// </summary>
internal sealed class DirtyProjectionSet : IDisposable
{
    private Entity[] _items;
    private int _count;

    public int Count => _count;

    public DirtyProjectionSet(int initialCapacity = 64)
    {
        _items = ArrayPool<Entity>.Shared.Rent(initialCapacity);
        _count = 0;
    }

    public void Add(Entity entity)
    {
        if ((uint)_count >= (uint)_items.Length)
        {
            Grow();
        }

        _items[_count++] = entity;
    }

    public ReadOnlySpan<Entity> AsSpan()
    {
        return _items.AsSpan(0, _count);
    }

    public void Clear()
    {
        _count = 0;
    }

    private void Grow()
    {
        int newLength = _items.Length << 1;
        Entity[] newItems = ArrayPool<Entity>.Shared.Rent(newLength);
        Array.Copy(_items, newItems, _count);
        ArrayPool<Entity>.Shared.Return(_items, clearArray: false);
        _items = newItems;
    }

    public void Dispose()
    {
        ArrayPool<Entity>.Shared.Return(_items, clearArray: false);
        _items = Array.Empty<Entity>();
        _count = 0;
    }
}
