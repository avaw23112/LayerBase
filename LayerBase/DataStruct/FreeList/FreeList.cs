using System.Runtime.CompilerServices;

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
///     高性能 Slot 分配器 (FreeList)
/// </summary>
internal sealed class FreeList<T> where T : struct
{
    private readonly List<Slot<T>[]> _slabs = new();
    private readonly int _slabSize;
    private readonly object _syncLock = new();
    private int _freeHead = -1;

    public FreeList(int slabSize)
    {
        if (slabSize <= 0) throw new ArgumentOutOfRangeException(nameof(slabSize));
        _slabSize = slabSize;
    }

    /// <summary>
    ///     预热内存空间，提前分配指定容量的 Slab。
    /// </summary>
    public void Prewarm(int capacity)
    {
        if (capacity <= 0) return;
        lock (_syncLock)
        {
            var neededSlabs = (capacity + _slabSize - 1) / _slabSize;
            var currentSlabs = _slabs.Count;
            for (var i = currentSlabs; i < neededSlabs; i++) AllocateSlabInternal();
        }
    }

    /// <summary>
    ///     租用新的 slot
    /// </summary>
    public SlotRef Rent()
    {
        lock (_syncLock)
        {
            if (_freeHead == -1) AllocateSlabInternal();

            var globalIndex = _freeHead;
            ref var slot = ref GetSlotInternal(globalIndex);
            _freeHead = slot.NextFree;

            slot.NextFree = -1;
            slot.InUse = true;
            slot.Completed = false;
            slot.Version = NextVersion(slot.Version);

            return new SlotRef(globalIndex, slot.Version);
        }
    }

    /// <summary>
    ///     尝试借用（验证有效性）
    /// </summary>
    public bool TryBorrow(int globalIndex, int version, out SlotRef slotRef)
    {
        lock (_syncLock)
        {
            if (!ValidateInternal(globalIndex, version))
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
            // 在高性能路径上，信任调用者已经通过 TryBorrow 或刚 Rent 到引用
            return ref GetSlotInternal(slotRef.GlobalIndex);
        }
    }

    /// <summary>
    ///     回收 Slot
    /// </summary>
    public void Release(in SlotRef slotRef)
    {
        lock (_syncLock)
        {
            if (!ValidateInternal(slotRef.GlobalIndex, slotRef.Version)) return;

            ref var slot = ref GetSlotInternal(slotRef.GlobalIndex);

            slot.Value = default;
            slot.InUse = false;
            slot.Completed = false;
            slot.Version = NextVersion(slot.Version);

            slot.NextFree = _freeHead;
            _freeHead = slotRef.GlobalIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ValidateInternal(int globalIndex, int version)
    {
        var slabIndex = globalIndex / _slabSize;
        if (slabIndex < 0 || slabIndex >= _slabs.Count) return false;

        var slotIndex = globalIndex % _slabSize;
        ref var slot = ref _slabs[slabIndex][slotIndex];
        return slot.InUse && slot.Version == version;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        // 倒序构建链表，使得 Rent 时能从低索引开始使用（对缓存更友好）
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