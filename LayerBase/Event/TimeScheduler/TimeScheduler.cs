using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LayerBase.Core.DataStruct;

namespace LayerBase.Core.Event;

internal interface ITimerPayloadReleaser<TPayload>
{
    void Release(in TPayload payload);
}

public sealed class TimeScheduler<TPayload> : IDisposable
{
    private readonly TimeSchedulerOptions _options;
    private TimerEntry<TPayload>[] _pool;
    private readonly int[] _wheelHeads;
    private readonly int[] _wheelTails;
    private readonly LongTimerHeap _longHeap;
    private readonly IntStack _freeList;
    private readonly ITimerPayloadReleaser<TPayload>? _payloadReleaser;

    private int _poolSize;
    private long _currentTick;
    private double _accumulator;
    private bool _disposed;

    private int _overdueHead = -1;
    private int _overdueTail = -1;
    private const int OverdueSlotIndex = -2;

    private readonly int _wheelSize;
    private readonly int _wheelMask;
    private readonly float _tickDuration;
    private readonly float _tickDurationReciprocal;
    private readonly int _maxPromotePerTick;
    private readonly int _maxExpiredPerTick;
    private readonly long _longTimerThresholdTicks;
    private readonly TimerFlags _defaultRepeatFlags;

    internal int PendingCount => _poolSize - _freeList.Count;

    public TimeScheduler(TimeSchedulerOptions options)
        : this(options, payloadReleaser: null)
    {
    }

    internal TimeScheduler(
        TimeSchedulerOptions options,
        ITimerPayloadReleaser<TPayload>? payloadReleaser)
    {
        _options = options;
        _payloadReleaser = payloadReleaser;

        var plan = new TimerWheelPlan(options.WheelSize, options.TickDurationSeconds);
        _wheelSize = plan.WheelSize;
        _wheelMask = plan.WheelMask;
        _tickDuration = plan.TickDurationSeconds;
        _tickDurationReciprocal = plan.TickDurationReciprocal;

        _maxPromotePerTick = options.MaxPromotePerTick;
        _maxExpiredPerTick = options.MaxExpiredPerTick;

        _pool = new TimerEntry<TPayload>[options.InitialTimerCapacity];
        _wheelHeads = new int[_wheelSize];
        _wheelTails = new int[_wheelSize];
        Array.Fill(_wheelHeads, -1);
        Array.Fill(_wheelTails, -1);

        _freeList = new IntStack(options.InitialTimerCapacity);
        for (int i = options.InitialTimerCapacity - 1; i >= 0; i--)
        {
            _pool[i].Version = 1;
            _freeList.Push(i);
        }

        _poolSize = options.InitialTimerCapacity;
        _longHeap = new LongTimerHeap(
            Math.Max(16, options.InitialTimerCapacity));

        long configuredThresholdTicks = options.LongTimerThresholdSeconds > 0
            ? Math.Max(1, (long)MathF.Ceiling(options.LongTimerThresholdSeconds * _tickDurationReciprocal))
            : 0;
        _longTimerThresholdTicks = configuredThresholdTicks > 0
            ? Math.Min(configuredThresholdTicks, _wheelSize)
            : _wheelSize;

        _defaultRepeatFlags = TimerFlags.Repeat;
        if (options.DefaultRepeatMode == TimerRepeatMode.FixedRate)
            _defaultRepeatFlags |= TimerFlags.FixedRate;
        else
            _defaultRepeatFlags |= TimerFlags.FixedDelay;

        if (options.DefaultCatchUpPolicy == TimerCatchUpPolicy.FireAllCapped)
            _defaultRepeatFlags |= TimerFlags.CatchUp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerHandle Schedule(in TPayload payload, float delaySeconds, int repeatCount = 0, float intervalSeconds = 0,
                                TimerRepeatMode? repeatMode = null, TimerCatchUpPolicy? catchUpPolicy = null)
    {
        if (_disposed) return TimerHandle.Invalid;

        if (_freeList.Count == 0) GrowPool();
        int index = _freeList.Pop();

        ref var entry = ref FastArray.At(_pool, index);
        entry.Payload = payload;

        var flags = TimerFlags.Active;
        if (repeatCount != 0)
        {
            flags |= TimerFlags.Repeat;
            if (repeatMode.HasValue)
            {
                if (repeatMode.Value == TimerRepeatMode.FixedRate) flags |= TimerFlags.FixedRate;
                else flags |= TimerFlags.FixedDelay;
            }
            else
            {
                flags |= (_defaultRepeatFlags & (TimerFlags.FixedRate | TimerFlags.FixedDelay));
            }

            if (catchUpPolicy.HasValue)
            {
                if (catchUpPolicy.Value == TimerCatchUpPolicy.FireAllCapped) flags |= TimerFlags.CatchUp;
            }
            else
            {
                flags |= (_defaultRepeatFlags & TimerFlags.CatchUp);
            }
        }

        entry.Flags = flags;

        entry.RemainingRepeatCount = repeatCount;
        entry.ExpireTick = _currentTick + NormalizeDelayTicks(delaySeconds);
        entry.IntervalTicks = repeatCount == 0 ? 0 : NormalizeDelayTicks(intervalSeconds);

        PlaceEntry(index);

        return new TimerHandle(index, entry.Version);
    }

    public bool Cancel(TimerHandle handle)
    {
        if (handle.IsInvalid || handle.Index >= _poolSize) return false;

        ref var entry = ref _pool[handle.Index];
        if ((entry.Flags & TimerFlags.Active) == 0 || entry.Version != handle.Version) return false;

        RemoveFromStructure(handle.Index);
        ReleaseTimer(handle.Index, ref entry);

        return true;
    }

    public void Tick(float deltaTime, IExpiredTimerSink<TPayload> sink)
    {
        if (_disposed) return;

        if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0)
            throw new ArgumentException(nameof(deltaTime));

        _accumulator += deltaTime;
        if (_accumulator < _tickDuration) return;

        _accumulator -= _tickDuration;
        TickOnce(sink);

        if (_accumulator >= _tickDuration)
            TickCatchUpSlow(sink);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TickOnce(IExpiredTimerSink<TPayload> sink)
    {
        _currentTick++;

        if (_longHeap.Count > 0)
            PromoteLongTimers();

        ProcessCurrentSlot(sink);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void TickCatchUpSlow(IExpiredTimerSink<TPayload> sink)
    {
        int catchUpCount = 0;
        while (_accumulator >= _tickDuration && catchUpCount < _options.MaxCatchUpTicksPerPump)
        {
            _accumulator -= _tickDuration;
            TickOnce(sink);
            catchUpCount++;
        }
    }

    private void PromoteLongTimers()
    {
        int promoted = 0;
        long wheelEndTick = _currentTick + _wheelSize - 1;

        while (promoted < _maxPromotePerTick && _longHeap.Count > 0)
        {
            if (_longHeap.TryPeek(out int index, out long expireTick, out int version))
            {
                if (expireTick <= wheelEndTick)
                {
                    _longHeap.Dequeue();
                    ref var entry = ref FastArray.At(_pool, index);
                    if ((entry.Flags & TimerFlags.Active) != 0 &&
                        entry.Version == version &&
                        entry.ExpireTick == expireTick &&
                        _longHeap.GetHeapPosition(index) < 0)
                    {
                        PlaceInWheel(index);
                        promoted++;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    private void ProcessCurrentSlot(IExpiredTimerSink<TPayload> sink)
    {
        int slot = (int)(_currentTick & _wheelMask);
        int current = _wheelHeads[slot];
        _wheelHeads[slot] = -1;
        _wheelTails[slot] = -1;

        Exception? firstException = null;
        int processedInTick = 0;

        current = ProcessTimerList(current, sink, ref processedInTick, ref firstException);

        if (current != -1)
            MoveToOverdue(current);

        if (processedInTick < _maxExpiredPerTick)
            ProcessOverdueQueue(sink, ref processedInTick, ref firstException);

        if (firstException != null)
            throw firstException;
    }

    private int ProcessTimerList(int head, IExpiredTimerSink<TPayload> sink, ref int count, ref Exception? firstException)
    {
        int current = head;
        while (current != -1 && count < _maxExpiredPerTick)
        {
            ref var entry = ref FastArray.At(_pool, current);
            int next = entry.Next;
            entry.Next = -1;
            entry.Prev = -1;

            if ((entry.Flags & TimerFlags.Active) != 0)
            {
                count++;
                try
                {
                    if (sink.TryAcceptExpired(in entry.Payload, new TimerHandle(current, entry.Version)))
                    {
                        if ((entry.Flags & TimerFlags.Repeat) != 0)
                            RescheduleRepeatSlow(current, ref entry);
                        else
                            ReleaseTimer(current, ref entry);
                    }
                    else
                    {
                        ReleaseTimer(current, ref entry);
                    }
                }
                catch (Exception ex)
                {
                    ReleaseTimer(current, ref entry);
                    firstException ??= ex;
                }
            }

            current = next;
        }
        return current;
    }

    private void MoveToOverdue(int head)
    {
        int current = head;
        int tail = -1;
        while (current != -1)
        {
            ref var entry = ref FastArray.At(_pool, current);
            entry.SlotIndex = OverdueSlotIndex;
            if (entry.Next == -1)
            {
                tail = current;
                break;
            }
            current = entry.Next;
        }

        if (_overdueTail == -1)
        {
            FastArray.At(_pool, head).Prev = -1;
            _overdueHead = head;
            _overdueTail = tail;
        }
        else
        {
            FastArray.At(_pool, _overdueTail).Next = head;
            FastArray.At(_pool, head).Prev = _overdueTail;
            _overdueTail = tail;
        }
    }

    private void ProcessOverdueQueue(IExpiredTimerSink<TPayload> sink, ref int count, ref Exception? firstException)
    {
        int current = _overdueHead;
        _overdueHead = -1;
        _overdueTail = -1;

        int remaining = ProcessTimerList(current, sink, ref count, ref firstException);

        if (remaining != -1)
            MoveToOverdue(remaining);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RescheduleRepeatSlow(int index, ref TimerEntry<TPayload> entry)
    {
        if (entry.RemainingRepeatCount == 0)
        {
            ReleaseTimer(index, ref entry);
            return;
        }

        if (entry.RemainingRepeatCount > 0) entry.RemainingRepeatCount--;

        long nextExpire;
        if ((entry.Flags & TimerFlags.FixedRate) != 0)
            nextExpire = entry.ExpireTick + entry.IntervalTicks;
        else
            nextExpire = _currentTick + entry.IntervalTicks;

        if (nextExpire <= _currentTick) nextExpire = _currentTick + entry.IntervalTicks;

        entry.ExpireTick = nextExpire;
        PlaceEntry(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long NormalizeDelayTicks(float seconds)
    {
        if (float.IsNaN(seconds) || seconds <= 0) return 1;
        return Math.Max(1, (long)MathF.Ceiling(seconds * _tickDurationReciprocal));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReleaseTimer(int index, ref TimerEntry<TPayload> entry)
    {
        if ((entry.Flags & TimerFlags.Active) == 0)
            return;

        _payloadReleaser?.Release(in entry.Payload);

        entry.Flags = TimerFlags.None;
        entry.Payload = default!;
        entry.ExpireTick = 0;
        entry.IntervalTicks = 0;
        entry.RemainingRepeatCount = 0;
        entry.Next = -1;
        entry.Prev = -1;
        entry.SlotIndex = -1;

        entry.Version++;
        if (entry.Version == 0)
            entry.Version = 1;

        _freeList.Push(index);
    }

    private void PlaceEntry(int index)
    {
        ref var entry = ref FastArray.At(_pool, index);
        long delayTicks = entry.ExpireTick - _currentTick;

        if (delayTicks <= _longTimerThresholdTicks)
        {
            PlaceInWheel(index);
        }
        else
        {
            PlaceInHeap(index);
        }
    }

    private void PlaceInWheel(int index)
    {
        ref var entry = ref FastArray.At(_pool, index);
        int slot = (int)(entry.ExpireTick & _wheelMask);

        entry.SlotIndex = slot;
        entry.Next = -1;

        int tail = _wheelTails[slot];
        entry.Prev = tail;

        if (tail == -1)
            _wheelHeads[slot] = index;
        else
            FastArray.At(_pool, tail).Next = index;

        _wheelTails[slot] = index;
    }

    private void PlaceInHeap(int index)
    {
        ref var entry = ref FastArray.At(_pool, index);
        entry.SlotIndex = -1;
        _longHeap.Enqueue(index, entry.ExpireTick, entry.Version);
    }

    private void RemoveFromStructure(int index)
    {
        ref var entry = ref FastArray.At(_pool, index);
        if (entry.SlotIndex >= 0)
        {
            if (entry.Prev != -1)
                FastArray.At(_pool, entry.Prev).Next = entry.Next;
            else
                _wheelHeads[entry.SlotIndex] = entry.Next;

            if (entry.Next != -1)
                FastArray.At(_pool, entry.Next).Prev = entry.Prev;
            else
                _wheelTails[entry.SlotIndex] = entry.Prev;

            return;
        }

        if (entry.SlotIndex == OverdueSlotIndex)
        {
            if (entry.Prev != -1)
                FastArray.At(_pool, entry.Prev).Next = entry.Next;
            else
                _overdueHead = entry.Next;

            if (entry.Next != -1)
                FastArray.At(_pool, entry.Next).Prev = entry.Prev;
            else
                _overdueTail = entry.Prev;

            return;
        }

        int heapPos = _longHeap.GetHeapPosition(index);
        if (heapPos >= 0)
        {
            _longHeap.RemoveAt(heapPos);
        }
    }

    private void GrowPool()
    {
        int oldSize = _poolSize;
        int newSize = oldSize * 2;
        Array.Resize(ref _pool, newSize);
        for (int i = newSize - 1; i >= oldSize; i--)
        {
            _pool[i].Version = 1;
            _freeList.Push(i);
        }

        _poolSize = newSize;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        for (int i = 0; i < _poolSize; i++)
        {
            ref var entry = ref FastArray.At(_pool, i);
            if ((entry.Flags & TimerFlags.Active) != 0)
            {
                _payloadReleaser?.Release(in entry.Payload);
            }
        }

        Array.Clear(_wheelHeads, 0, _wheelHeads.Length);
        Array.Clear(_wheelTails, 0, _wheelTails.Length);
        _longHeap.Clear();
        _freeList.Clear();
    }
}
