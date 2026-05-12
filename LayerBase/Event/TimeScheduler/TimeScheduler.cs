using System.Diagnostics;
using System.Runtime.CompilerServices;
using LayerBase.Core.DataStruct;

namespace LayerBase.Core.Event;

public sealed class TimeScheduler<TPayload> : IDisposable
{
    private readonly TimeSchedulerOptions _options;
    private TimerEntry<TPayload>[] _pool;
    private readonly int[] _wheel;
    private readonly LongTimerHeap _longHeap;
    private readonly IntStack _freeList;

    private int _poolSize;
    private long _currentTick;
    private double _accumulator;
    private bool _disposed;

    private readonly int _wheelSize;
    private readonly int _wheelMask;
    private readonly float _tickDuration;
    private readonly float _tickDurationReciprocal;
    private readonly int _maxPromotePerTick;
    private readonly int _maxExpiredPerTick;
    private readonly TimerFlags _defaultRepeatFlags;

    public TimeScheduler(TimeSchedulerOptions options)
    {
        _options = options;

        var plan = new TimerWheelPlan(options.WheelSize, options.TickDurationSeconds);
        _wheelSize = plan.WheelSize;
        _wheelMask = plan.WheelMask;
        _tickDuration = plan.TickDurationSeconds;
        _tickDurationReciprocal = plan.TickDurationReciprocal;

        _maxPromotePerTick = options.MaxPromotePerTick;
        _maxExpiredPerTick = options.MaxExpiredPerTick;

        _pool = new TimerEntry<TPayload>[options.InitialTimerCapacity];
        _wheel = new int[_wheelSize];
        Array.Fill(_wheel, -1);

        _freeList = new IntStack(options.InitialTimerCapacity);
        for (int i = options.InitialTimerCapacity - 1; i >= 0; i--)
        {
            _pool[i].Version = 1;
            _freeList.Push(i);
        }

        _poolSize = options.InitialTimerCapacity;
        _longHeap = new LongTimerHeap(16);

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
        entry.Flags = TimerFlags.None;
        entry.Payload = default!;
        entry.Version++;
        if (entry.Version == 0) entry.Version = 1;
        _freeList.Push(handle.Index);

        return true;
    }

    public void Tick(float deltaTime, IExpiredTimerSink<TPayload> sink)
    {
        if (_disposed) return;

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
        while (_accumulator >= _tickDuration)
        {
            _accumulator -= _tickDuration;
            TickOnce(sink);
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
                        entry.SlotIndex < 0)
                    {
                        entry.SlotIndex = -1;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessCurrentSlot(IExpiredTimerSink<TPayload> sink)
    {
        int slot = (int)(_currentTick & _wheelMask);
        ref var wheelSlotRef = ref FastArray.At(_wheel, slot);
        int current = wheelSlotRef;
        wheelSlotRef = -1;

        int processedInTick = 0;

        while (current != -1 && processedInTick < _maxExpiredPerTick)
        {
            ref var entry = ref FastArray.At(_pool, current);
            int next = entry.Next;

            if ((entry.Flags & TimerFlags.Active) != 0)
            {
                if (sink.TryAcceptExpired(in entry.Payload, new TimerHandle(current, entry.Version)))
                {
                    processedInTick++;

                    if ((entry.Flags & TimerFlags.Repeat) != 0)
                    {
                        RescheduleRepeatSlow(current, ref entry);
                    }
                    else
                    {
                        ReleaseTimer(current, ref entry);
                    }
                }
                else
                {
                    ReleaseTimer(current, ref entry);
                }
            }

            current = next;
        }

        if (current != -1)
        {
            RequeueRemainingForNextTick(current);
        }
    }

    private void RequeueRemainingForNextTick(int head)
    {
        var targetSlot = (int)((_currentTick + 1) & _wheelMask);
        FastArray.At(_pool, head).Prev = -1;

        var tail = head;
        while (true)
        {
            FastArray.At(_pool, tail).SlotIndex = targetSlot;
            if (FastArray.At(_pool, tail).Next == -1) break;
            tail = FastArray.At(_pool, tail).Next;
        }

        ref var targetHead = ref FastArray.At(_wheel, targetSlot);
        FastArray.At(_pool, tail).Next = targetHead;
        if (targetHead != -1)
        {
            FastArray.At(_pool, targetHead).Prev = tail;
        }

        targetHead = head;
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
        entry.Flags = TimerFlags.None;
        entry.Payload = default!;
        entry.Version++;
        if (entry.Version == 0) entry.Version = 1;
        _freeList.Push(index);
    }

    private void PlaceEntry(int index)
    {
        ref var entry = ref FastArray.At(_pool, index);
        long delayTicks = entry.ExpireTick - _currentTick;

        if (delayTicks <= _wheelSize)
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
        ref var wheelSlotRef = ref FastArray.At(_wheel, slot);
        entry.Next = wheelSlotRef;
        entry.Prev = -1;

        if (wheelSlotRef != -1)
        {
            FastArray.At(_pool, wheelSlotRef).Prev = index;
        }

        wheelSlotRef = index;
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
                FastArray.At(_wheel, entry.SlotIndex) = entry.Next;

            if (entry.Next != -1)
                FastArray.At(_pool, entry.Next).Prev = entry.Prev;
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
        Array.Clear(_wheel, 0, _wheel.Length);
        _longHeap.Clear();
        _freeList.Clear();
        for (int i = 0; i < _pool.Length; i++) _pool[i].Payload = default!;
    }
}