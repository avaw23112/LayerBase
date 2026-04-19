namespace LayerBase.Core.EventStateTrace;

internal struct Slot<T> where T : struct
{
    public T Value;
    public ushort Version;
    public bool InUse;
    public int NextFree;
    public bool Completed;
}

internal struct SlotRef
{
    public SlotRef(int globalIndex, ushort version)
    {
        GlobalIndex = globalIndex;
        Version = version;
    }

    /// <summary>
    ///     当前节点在整个内存空间的相对位置
    /// </summary>
    public int GlobalIndex { get; }

    /// <summary>
    ///     由于GlobalIndex会被复用,需要额外参数查重.
    /// </summary>
    public ushort Version { get; }
}

/// <summary>
///     freelist
/// </summary>
internal sealed class FreeList<T> where T : struct
{
    /// <summary>
    ///     总内存空间
    /// </summary>
    private readonly List<Slot<T>[]> _slabs = new();

    /// <summary>
    ///     每组Slot[]的固定长度
    /// </summary>
    private readonly int _slabSize;

    private readonly object _syncLock = new();

    /// <summary>
    ///     空闲节点头指针
    /// </summary>
    private int _freeHead = -1;

    public FreeList(int slabSize)
    {
        _slabSize = slabSize;
    }


    /// <summary>
    ///     租用新的slot
    /// </summary>
    /// <returns></returns>
    public SlotRef Rent()
    {
        lock (_syncLock)
        {
            //当目前空间不足时,即_freeHead指向最后一个节点的nextFree时,重新开辟内存
            if (_freeHead == -1) AllocateSlabInternal();

            //取出最新可用空闲节点
            var globalIndex = _freeHead;
            ref var slot = ref GetSlotInternal(globalIndex);
            _freeHead = slot.NextFree;

            //将已经分配的slot移除出空闲链表
            slot.NextFree = -1;
            slot.InUse = true;
            slot.Completed = false;
            slot.Version = NextVersion(slot.Version);

            //返回slot引用
            return new SlotRef(globalIndex, slot.Version);
        }
    }

    /// <summary>
    ///     使用原始方式直接获取Slot
    /// </summary>
    public bool TryBorrow(int GlobalIndex, int Version, out SlotRef slotRef)
    {
        lock (_syncLock)
        {
            if (!TryValidateInternal(GlobalIndex, Version, out var globalIndex))
            {
                slotRef = default;
                return false;
            }

            ref var slot = ref GetSlotInternal(globalIndex);
            slotRef = new SlotRef(globalIndex, slot.Version);
            return true;
        }
    }

    public ref Slot<T> Resolve(SlotRef slotRef)
    {
        lock (_syncLock)
        {
            return ref GetSlotInternal(slotRef.GlobalIndex);
        }
    }

    /// <summary>
    ///     回收Slot
    /// </summary>
    public void Release(in SlotRef slotRef)
    {
        lock (_syncLock)
        {
            ref var slot = ref GetSlotInternal(slotRef.GlobalIndex);

            if (!slot.InUse || slot.Version != slotRef.Version) return;

            //重置当前Slot
            slot.Value = default;
            slot.InUse = false;
            slot.Completed = false;
            slot.Version = NextVersion(slot.Version);

            //延申freeList,使当前已经被释放的slot成为freeList的头节点.
            slot.NextFree = _freeHead;
            _freeHead = slotRef.GlobalIndex;
        }
    }

    private bool TryValidateInternal(int GlobalIndex, int Version, out int globalIndex)
    {
        var slabIndex = GlobalIndex / _slabSize;
        if (slabIndex < 0 || slabIndex >= _slabs.Count)
        {
            globalIndex = default;
            return false;
        }

        var slotIndex = GlobalIndex % _slabSize;
        ref var slot = ref _slabs[slabIndex][slotIndex];
        if (!slot.InUse || slot.Version != Version)
        {
            globalIndex = default;
            return false;
        }

        globalIndex = GlobalIndex;
        return true;
    }

    private ref Slot<T> GetSlotInternal(int globalIndex)
    {
        var slabIndex = globalIndex / _slabSize;
        var slotIndex = globalIndex % _slabSize;
        return ref _slabs[slabIndex][slotIndex];
    }

    private void AllocateSlabInternal()
    {
        var baseIndex = _slabs.Count * _slabSize;
        var slab = new Slot<T>[_slabSize];

        for (var i = _slabSize - 1; i >= 0; i--)
        {
            slab[i].NextFree = _freeHead;
            _freeHead = baseIndex + i;
        }

        _slabs.Add(slab);
    }

    private static ushort NextVersion(ushort current)
    {
        var next = (ushort)(current + 1);
        if (next == 0) next = 1;
        return next;
    }
}