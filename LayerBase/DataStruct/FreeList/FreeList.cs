using System.Runtime.CompilerServices;

namespace LayerBase.Core.EventStateTrace;

/// <summary>
/// 空闲列表中的槽位。存储值、版本号、使用状态和链表指针。
/// </summary>
internal struct Slot<T> where T : struct
{
    public T Value;
    public ushort Version;
    public bool InUse;
    public int NextFree;
    public bool Completed;
}

/// <summary>
/// 空闲列表中槽位的引用令牌，包含全局索引和版本号。
/// </summary>
internal struct SlotRef
{
    public SlotRef(int globalIndex, ushort version)
    {
        GlobalIndex = globalIndex;
        Version = version;
    }


    public int GlobalIndex { get; }


    public ushort Version { get; }
}

/// <summary>
/// 基于分块（slab）的空闲列表。支持 Rent（租用）、TryBorrow（借用）和 Release（释放）操作。
/// 每个槽位使用版本号防止悬挂引用（ABA 问题）。
/// 用于 TimerScheduler 等需要对象池化和安全并发访问的场景。
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
            return ref GetSlotInternal(slotRef.GlobalIndex);
        }
    }


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