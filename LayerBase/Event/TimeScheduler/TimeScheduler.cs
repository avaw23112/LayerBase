using System.Diagnostics;
using System.Runtime.CompilerServices;
using LayerBase.Core.DataStruct;

namespace LayerBase.Core.Event;

public sealed class TimeScheduler<TPayload> : IDisposable
{
    private readonly TimeSchedulerOptions _options;
    private TimerEntry<TPayload>[] _pool;
    private readonly int[] _wheel;
    private readonly LayerBase.Core.DataStruct.PriorityQueue<int, long> _heap = new();
    private readonly Stack<int> _freeList = new();
    
    private int _poolSize;
    private long _currentTick;
    private double _accumulator;
    private bool _disposed;
    
    private readonly long _wheelSpanTicks;
    private readonly float _tickDuration;

    public TimeScheduler(TimeSchedulerOptions options)
    {
        _options = options;
        _tickDuration = options.TickDurationSeconds;
        _wheelSpanTicks = options.WheelSize;
        
        _pool = new TimerEntry<TPayload>[options.InitialTimerCapacity];
        _wheel = new int[options.WheelSize];
        Array.Fill(_wheel, -1);
        
        for (int i = options.InitialTimerCapacity - 1; i >= 0; i--)
        {
            _pool[i].Version = 1;
            _freeList.Push(i);
        }
        _poolSize = options.InitialTimerCapacity;
    }

    public TimerHandle Schedule(in TPayload payload, float delaySeconds, int repeatCount = 0, float intervalSeconds = 0, TimerRepeatMode? repeatMode = null, TimerCatchUpPolicy? catchUpPolicy = null)
    {
        if (_disposed) return TimerHandle.Invalid;

        if (_freeList.Count == 0) GrowPool();
        int index = _freeList.Pop();
        
        ref var entry = ref _pool[index];
        entry.Payload = payload;
        entry.Active = true;
        entry.RemainingRepeatCount = repeatCount;
        entry.RepeatMode = repeatCount == 0 ? TimerRepeatMode.Once : (repeatMode ?? _options.DefaultRepeatMode);
        entry.CatchUpPolicy = catchUpPolicy ?? _options.DefaultCatchUpPolicy;
        
        long delayTicks = (long)Math.Ceiling(delaySeconds / _tickDuration);
        entry.ExpireTick = _currentTick + delayTicks;
        entry.IntervalTicks = (long)Math.Ceiling(intervalSeconds / _tickDuration);
        
        PlaceEntry(index);
        
        return new TimerHandle(index, entry.Version);
    }

    public bool Cancel(TimerHandle handle)
    {
        if (handle.IsInvalid || handle.Index >= _poolSize) return false;
        
        ref var entry = ref _pool[handle.Index];
        if (!entry.Active || entry.Version != handle.Version) return false;
        
        RemoveFromStructure(handle.Index);
        entry.Active = false;
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
        while (_accumulator >= _tickDuration)
        {
            _accumulator -= _tickDuration;
            _currentTick++;
            
            PromoteLongTimers();
            ProcessCurrentSlot(sink);
        }
    }

    private void PromoteLongTimers()
    {
        int promoted = 0;
        long wheelEndTick = _currentTick + _options.WheelSize - 1;
        
        while (promoted < _options.MaxPromotePerTick && _heap.Count > 0)
        {
            if (_heap.TryPeek(out int index, out long expireTick))
            {
                if (expireTick <= wheelEndTick)
                {
                    _heap.Dequeue();
                    ref var entry = ref _pool[index];
                    if (entry.Active)
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

    private void ProcessCurrentSlot(IExpiredTimerSink<TPayload> sink)
    {
        int slot = (int)(_currentTick % _options.WheelSize);
        int head = _wheel[slot];
        _wheel[slot] = -1;

        int current = head;
        int processedInTick = 0;

        while (current != -1 && processedInTick < _options.MaxExpiredPerTick)
        {
            ref var entry = ref _pool[current];
            int next = entry.Next;
            
            if (entry.Active)
            {
                if (sink.TryAcceptExpired(entry.Payload, new TimerHandle(current, entry.Version)))
                {
                    processedInTick++;
                    
                    if (entry.RemainingRepeatCount != 0)
                    {
                        if (entry.RemainingRepeatCount > 0) entry.RemainingRepeatCount--;
                        
                        long nextExpire;
                        if (entry.RepeatMode == TimerRepeatMode.FixedRate)
                            nextExpire = entry.ExpireTick + entry.IntervalTicks;
                        else
                            nextExpire = _currentTick + entry.IntervalTicks;
                            
                        if (nextExpire <= _currentTick) nextExpire = _currentTick + entry.IntervalTicks;
                        
                        entry.ExpireTick = nextExpire;
                        PlaceEntry(current);
                    }
                    else
                    {
                        entry.Active = false;
                        entry.Payload = default!;
                        entry.Version++;
                        if (entry.Version == 0) entry.Version = 1;
                        _freeList.Push(current);
                    }
                }
                else
                {
                    entry.Active = false;
                    entry.Payload = default!;
                    entry.Version++;
                    if (entry.Version == 0) entry.Version = 1;
                    _freeList.Push(current);
                }
            }
            
            current = next;
        }
        
        if (current != -1)
        {
            _wheel[slot] = current;
            _pool[current].Prev = -1;
        }
    }

    private void PlaceEntry(int index)
    {
        ref var entry = ref _pool[index];
        long delayTicks = entry.ExpireTick - _currentTick;
        
        if (delayTicks <= _wheelSpanTicks)
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
        ref var entry = ref _pool[index];
        int slot = (int)(entry.ExpireTick % _options.WheelSize);
        
        entry.SlotIndex = slot;
        entry.Next = _wheel[slot];
        entry.Prev = -1;
        
        if (_wheel[slot] != -1)
        {
            _pool[_wheel[slot]].Prev = index;
        }
        _wheel[slot] = index;
    }

    private void PlaceInHeap(int index)
    {
        ref var entry = ref _pool[index];
        entry.SlotIndex = -1;
        _heap.Enqueue(index, entry.ExpireTick);
    }

    private void RemoveFromStructure(int index)
    {
        ref var entry = ref _pool[index];
        if (entry.SlotIndex >= 0)
        {
            if (entry.Prev != -1)
                _pool[entry.Prev].Next = entry.Next;
            else
                _wheel[entry.SlotIndex] = entry.Next;
                
            if (entry.Next != -1)
                _pool[entry.Next].Prev = entry.Prev;
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
        _heap.Clear();
        _freeList.Clear();
        for (int i = 0; i < _pool.Length; i++) _pool[i].Payload = default!;
    }
}
