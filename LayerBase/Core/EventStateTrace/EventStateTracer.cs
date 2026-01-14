using System;
using System.Collections.Generic;
using System.Diagnostics;
using LayerBase.Core.Event;

namespace LayerBase.Core.EventStateTrace;

/// <summary>
/// 负责为 EventState 提供可复用的存储槽位与路径池。
/// </summary>
public sealed class EventStateTracer
{
    private readonly List<Slot[]> _slabs = new();
    private readonly int _slabSize;
    private readonly object _lock = new();
    private readonly EventPathPool _pathPool;
    private int _freeHead = -1;

    private struct Slot
    {
        public EventState State;
        public ushort Version;
        public bool InUse;
        public int NextFree;
    }

    private readonly struct SlotPosition
    {
        public SlotPosition(int slab, int index)
        {
            Slab = slab;
            Index = index;
        }

        public int Slab { get; }
        public int Index { get; }
        public int ToGlobalIndex(int slabSize) => Slab * slabSize + Index;
    }

    public EventStateTracer(int slabSize = 512)
    {
        if (slabSize <= 0) throw new ArgumentOutOfRangeException(nameof(slabSize));

        _slabSize = slabSize;
        _pathPool = new EventPathPool();
    }

    /// <summary>
    /// 记录一个事件的状态，返回对应的 token。
    /// </summary>
    public EventStateToken Register<T>(in Event<T> @event, ReadOnlySpan<int> pathSegments) where T : struct
    {
        EventHandledState state = @event.IsVaild() ? EventHandledState.Created : EventHandledState.Handled;
        return Register(EventTypeId<T>.Id, @event.ForwardDir, state, pathSegments);
    }

    /// <summary>
    /// 记录一个事件的状态，返回对应的 token。
    /// </summary>
    public EventStateToken Register(int eventTypeId, ReadOnlySpan<int> pathSegments)
    {
        return Register(eventTypeId, EventForwardDir.BroadCast, EventHandledState.Created, pathSegments);
    }

    public EventStateToken Register(int eventTypeId, EventForwardDir forwardDir, EventHandledState handledState, ReadOnlySpan<int> pathSegments)
    {
        lock (_lock)
        {
            int slotIndex = RentSlot();
            ref var slot = ref GetSlot(slotIndex);
            slot.Version = NextVersion(slot.Version);
            slot.InUse = true;
            slot.NextFree = -1;

            slot.State = new EventState
            {
                EventTypeId = eventTypeId,
                ForwardDir = forwardDir,
                HandledState = handledState,
                CreatedTimestamp = Stopwatch.GetTimestamp(),
                PathHandle = _pathPool.Rent(pathSegments)
            };

            return new EventStateToken(slotIndex, slot.Version);
        }
    }

    /// <summary>
    /// 根据 token 读取当前的 EventState（返回副本）。
    /// </summary>
    public bool TryGet(in EventStateToken token, out EventState state)
    {
        lock (_lock)
        {
            if (!TryValidate(token, out var pos))
            {
                state = default;
                return false;
            }

            state = _slabs[pos.Slab][pos.Index].State;
            return true;
        }
    }

    /// <summary>
    /// 更新事件的处理状态。
    /// </summary>
    public bool TryUpdateHandledState(in EventStateToken token, EventHandledState handledState)
    {
        lock (_lock)
        {
            if (!TryValidate(token, out var pos))
            {
                return false;
            }

            ref var slot = ref _slabs[pos.Slab][pos.Index];
            slot.State.HandledState = handledState;
            return true;
        }
    }

    /// <summary>
    /// 更新事件的传播方向。
    /// </summary>
    public bool TryUpdateForwardDir(in EventStateToken token, EventForwardDir forwardDir)
    {
        lock (_lock)
        {
            if (!TryValidate(token, out var pos))
            {
                return false;
            }

            ref var slot = ref _slabs[pos.Slab][pos.Index];
            slot.State.ForwardDir = forwardDir;
            return true;
        }
    }

    /// <summary>
    /// 释放对应的状态槽位与路径。
    /// </summary>
    public bool TryRelease(in EventStateToken token)
    {
        lock (_lock)
        {
            if (!TryValidate(token, out var pos))
            {
                return false;
            }

            ref var slot = ref _slabs[pos.Slab][pos.Index];
            _pathPool.Return(slot.State.PathHandle);
            slot.State = default;
            slot.InUse = false;
            slot.Version = NextVersion(slot.Version);

            int globalIndex = pos.ToGlobalIndex(_slabSize);
            slot.NextFree = _freeHead;
            _freeHead = globalIndex;
            return true;
        }
    }

    private int RentSlot()
    {
        if (_freeHead == -1)
        {
            AllocateSlab();
        }

        int index = _freeHead;
        ref var slot = ref GetSlot(index);
        _freeHead = slot.NextFree;
        slot.NextFree = -1;
        return index;
    }

    private void AllocateSlab()
    {
        int baseIndex = _slabs.Count * _slabSize;
        var slab = new Slot[_slabSize];
        for (int i = _slabSize - 1; i >= 0; i--)
        {
            slab[i].NextFree = _freeHead;
            _freeHead = baseIndex + i;
        }
        _slabs.Add(slab);
    }

    private ref Slot GetSlot(int globalIndex)
    {
        int slabIndex = globalIndex / _slabSize;
        int slotIndex = globalIndex - slabIndex * _slabSize;
        return ref _slabs[slabIndex][slotIndex];
    }

    private bool TryValidate(in EventStateToken token, out SlotPosition pos)
    {
        if (!token.IsValid)
        {
            pos = default;
            return false;
        }

        int slabIndex = token.Index / _slabSize;
        if (slabIndex < 0 || slabIndex >= _slabs.Count)
        {
            pos = default;
            return false;
        }

        int slotIndex = token.Index - slabIndex * _slabSize;
        ref var slot = ref _slabs[slabIndex][slotIndex];
        if (!slot.InUse || slot.Version != token.Version)
        {
            pos = default;
            return false;
        }

        pos = new SlotPosition(slabIndex, slotIndex);
        return true;
    }

    private static ushort NextVersion(ushort current)
    {
        ushort next = (ushort)(current + 1);
        if (next == 0)
        {
            next = 1;
        }
        return next;
    }
}
