using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LayerBase.Core.Event;

namespace LayerBase.Core.EventStateTrace;

/// <summary>
/// 负责为 EventState 提供可复用的存储槽位于路径池。
/// </summary>
internal sealed class EventStateTracer
{
    private readonly List<Slot[]> _slabs = new();
    private readonly int _slabSize;
    private readonly object _lock = new();
    private readonly EventPathPool _pathPool;
    private readonly EventTraceLogQueue _logQueue;
    private int _freeHead = -1;
    private readonly List<int> _completed = new();
    private volatile bool _enabled = true;

    private struct Slot
    {
        public EventState State;
        public ushort Version;
        public bool InUse;
        public int NextFree;
        public bool Completed;
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

    public EventStateTracer(int slabSize = 512, int logCapacity = 256)
    {
        if (slabSize <= 0) throw new ArgumentOutOfRangeException(nameof(slabSize));
        if (logCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(logCapacity));

        _slabSize = slabSize;
        _pathPool = new EventPathPool();
        _logQueue = new EventTraceLogQueue(logCapacity);
    }

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public EventTraceLogQueue Logs => _logQueue;

    /// <summary>
    /// 记录一个事件的状态，返回对应的 token。
    /// </summary>
    public EventStateToken Register<T>(in Event<T> @event) where T : struct
    {
        EventHandledState state = @event.IsVaild() ? EventHandledState.Created : EventHandledState.Handled;
        return Register(EventTypeId<T>.Id, @event.ForwardDir, state);
    }

    /// <summary>
    /// 记录一个事件的状态，返回对应的 token。
    /// </summary>
    public EventStateToken Register(int eventTypeId)
    {
        return Register(eventTypeId, EventForwardDir.BroadCast, EventHandledState.Created);
    }

    public EventStateToken Register(int eventTypeId, EventForwardDir forwardDir, EventHandledState handledState)
    {
        if (!_enabled)
        {
            return EventStateToken.None;
        }

        lock (_lock)
        {
            int slotIndex = RentSlot();
            ref var slot = ref GetSlot(slotIndex);
            slot.Version = NextVersion(slot.Version);
            slot.InUse = true;
            slot.NextFree = -1;
            slot.Completed = false;

            slot.State = new EventState
            {
                EventTypeId = eventTypeId,
                ForwardDir = forwardDir,
                HandledState = handledState,
                StartTimestamp = Stopwatch.GetTimestamp(),
                CreatedTimestamp = Stopwatch.GetTimestamp(),
                PendingCount = 1,
                PathHandle = _pathPool.Rent()
            };

            return new EventStateToken(slotIndex, slot.Version);
        }
    }

    /// <summary>
    /// 在路径中新增一层。
    /// </summary>
    public bool TryBeginLayer(in EventStateToken token, string layerName, long timestamp = 0)
    {
        if (!_enabled) return false;

        if (string.IsNullOrEmpty(layerName))
        {
            return false;
        }

        lock (_lock)
        {
            if (!TryValidate(token, out var pos))
            {
                return false;
            }

            ref var slot = ref _slabs[pos.Slab][pos.Index];
            var path = slot.State.PathHandle;
            if (!path.HasValue)
            {
                return false;
            }

            EnsureFrameCapacity(ref path, path.FrameCount + 1);

            long ts = timestamp != 0 ? timestamp : Stopwatch.GetTimestamp();
            int frameIndex = path.FrameCount;
            path.Frames![frameIndex] = new PathFrame
            {
                Timestamp = ts,
                LayerName = layerName,
                HandlerStart = path.HandlerCount,
                HandlerCount = 0
            };
            path.FrameCount = frameIndex + 1;

            slot.State.PathHandle = path;
            return true;
        }
    }

    /// <summary>
    /// 在当前层记录一个处理器的处理结果。
    /// </summary>
    public bool TryRecordHandler(in EventStateToken token, string handlerName, EventHandledState handledState)
    {
        if (!_enabled) return false;

        if (string.IsNullOrEmpty(handlerName))
        {
            return false;
        }

        lock (_lock)
        {
            if (!TryValidate(token, out var pos))
            {
                return false;
            }

            ref var slot = ref _slabs[pos.Slab][pos.Index];
            var path = slot.State.PathHandle;
            if (!path.HasValue || path.FrameCount == 0)
            {
                return false;
            }

            EnsureHandlerCapacity(ref path, path.HandlerCount + 1);

            int handlerIndex = path.HandlerCount;
            path.Handlers![handlerIndex] = new HandlerVisit
            {
                HandlerName = handlerName,
                State = handledState
            };
            path.HandlerCount = handlerIndex + 1;

            int frameIndex = path.FrameCount - 1;
            ref var frame = ref path.Frames![frameIndex];
            frame.HandlerCount += 1;

            slot.State.PathHandle = path;
            return true;
        }
    }

    /// <summary>
    /// 导出路径字符串：[Time][Layer]{handler1 : State => handler2 : State...}
    /// </summary>
    public bool TryExport(in EventStateToken token, out string result)
    {
        lock (_lock)
        {
            if (!TryValidate(token, out var pos))
            {
                result = string.Empty;
                return false;
            }

            ref var slot = ref _slabs[pos.Slab][pos.Index];
            return TryExportUnlocked(ref slot, out result);
        }
    }

    /// <summary>
    /// 增加待处理分支计数。
    /// </summary>
    public bool TryIncrementPending(in EventStateToken token, int count = 1)
    {
        if (!_enabled || count <= 0) return false;

        lock (_lock)
        {
            if (!TryValidate(token, out var pos))
            {
                return false;
            }

            ref var slot = ref _slabs[pos.Slab][pos.Index];
            slot.State.PendingCount += count;
            return true;
        }
    }

    /// <summary>
    /// 标记当前分支处理完成，计数归零后待导出。
    /// </summary>
    public bool TryComplete(in EventStateToken token)
    {
        if (!_enabled) return false;

        lock (_lock)
        {
            if (!TryValidate(token, out var pos))
            {
                return false;
            }

            ref var slot = ref _slabs[pos.Slab][pos.Index];
            if (slot.Completed)
            {
                return false;
            }

            slot.State.PendingCount -= 1;
            if (slot.State.PendingCount <= 0)
            {
                slot.Completed = true;
                _completed.Add(pos.ToGlobalIndex(_slabSize));
            }

            return true;
        }
    }

    /// <summary>
    /// 将已完成事件导出到日志队列，并释放槽位。
    /// </summary>
    public void Pump()
    {
        if (!_enabled) return;

        List<int> completedCopy;
        lock (_lock)
        {
            if (_completed.Count == 0) return;
            completedCopy = new List<int>(_completed);
            _completed.Clear();
        }

        for (int i = 0; i < completedCopy.Count; i++)
        {
            int idx = completedCopy[i];
            string line = string.Empty;
            lock (_lock)
            {
                if (idx < 0 || idx / _slabSize >= _slabs.Count)
                {
                    continue;
                }

                ref var slot = ref GetSlot(idx);
                if (!slot.InUse)
                {
                    continue;
                }

                if (TryExportUnlocked(ref slot, out var exported))
                {
                    line = exported;
                }

                ReleaseSlot(idx, ref slot);
            }

            if (!string.IsNullOrEmpty(line))
            {
                _logQueue.EnqueueOverwrite(line);
            }
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

    private void EnsureFrameCapacity(ref EventPath path, int required)
    {
        if (path.Frames!.Length < required)
        {
            path.Frames = _pathPool.GrowFrames(path.Frames, required);
        }
    }

    private void EnsureHandlerCapacity(ref EventPath path, int required)
    {
        if (path.Handlers!.Length < required)
        {
            path.Handlers = _pathPool.GrowHandlers(path.Handlers, required);
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

    private bool TryExportUnlocked(ref Slot slot, out string result)
    {
        var path = slot.State.PathHandle;
        if (!path.HasValue || path.FrameCount == 0)
        {
            result = string.Empty;
            return false;
        }

        var sb = new StringBuilder();
        double elapsedSeconds = (Stopwatch.GetTimestamp() - slot.State.StartTimestamp) / (double)Stopwatch.Frequency;
        if (elapsedSeconds < 0)
        {
            elapsedSeconds = 0;
        }
        var startTime = DateTime.Now - TimeSpan.FromSeconds(elapsedSeconds);
        sb.Append("[Start=").Append(startTime.ToString("HH:mm")).Append(']');
        string nl = Environment.NewLine;
        for (int i = 0; i < path.FrameCount; i++)
        {
            sb.Append(nl);

            ref var frame = ref path.Frames![i];
            sb.Append('[').Append(frame.LayerName).Append(']');
            sb.Append('{');

            int start = frame.HandlerStart;
            int end = start + frame.HandlerCount;
            for (int h = start; h < end; h++)
            {
                if (h > start)
                {
                    sb.Append(" => ");
                }

                var visit = path.Handlers![h];
                sb.Append(visit.HandlerName);
                sb.Append(" : ");
                sb.Append(visit.State);
            }

            sb.Append('}');
        }

        result = sb.ToString();
        return true;
    }

    private void ReleaseSlot(int globalIndex, ref Slot slot)
    {
        _pathPool.Return(slot.State.PathHandle);
        slot.State = default;
        slot.InUse = false;
        slot.Completed = false;
        slot.Version = NextVersion(slot.Version);

        slot.NextFree = _freeHead;
        _freeHead = globalIndex;
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
