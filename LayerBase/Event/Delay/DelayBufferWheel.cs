using System.Runtime.CompilerServices;
using LayerBase.Event.Delay;

namespace LayerBase.Core.Event;

internal sealed class DelayBufferWheel
{
    private readonly DelayBufferOptions _options;
    private readonly DelayPublisherManager _manager;
    private DelayExpireEntry[] _pool;
    private readonly int[] _wheel;
    private readonly Stack<int> _freeList = new();
    
    private int _poolSize;
    private long _currentTick;
    private double _accumulator;
    private readonly float _tickDuration;

    public DelayBufferWheel(DelayBufferOptions options, DelayPublisherManager manager)
    {
        _options = options;
        _manager = manager;
        _tickDuration = options.TickDurationSeconds;
        
        _pool = new DelayExpireEntry[options.InitialCapacity];
        _wheel = new int[options.WheelSize];
        Array.Fill(_wheel, -1);
        
        for (int i = options.InitialCapacity - 1; i >= 0; i--)
        {
            _pool[i].EntryVersion = 1;
            _freeList.Push(i);
        }
        _poolSize = options.InitialCapacity;
    }

    public DelayTimerHandle Schedule(int publisherId, int valueVersion, float ttlSeconds)
    {
        if (_freeList.Count == 0) GrowPool();
        int index = _freeList.Pop();
        
        ref var entry = ref _pool[index];
        entry.PublisherId = publisherId;
        entry.ValueVersion = valueVersion;
        entry.Active = true;
        
        long delayTicks = (long)Math.Ceiling(ttlSeconds / _tickDuration);
        entry.ExpireTick = _currentTick + delayTicks;
        
        int slot = (int)(entry.ExpireTick % _options.WheelSize);
        entry.SlotIndex = slot;
        entry.Next = _wheel[slot];
        entry.Prev = -1;
        
        if (_wheel[slot] != -1)
        {
            _pool[_wheel[slot]].Prev = index;
        }
        _wheel[slot] = index;
        
        return new DelayTimerHandle(index, entry.EntryVersion);
    }

    public bool Cancel(DelayTimerHandle handle)
    {
        if (!handle.IsValid || handle.Index >= _poolSize) return false;
        
        ref var entry = ref _pool[handle.Index];
        if (!entry.Active || entry.EntryVersion != handle.Version) return false;
        
        RemoveFromWheel(handle.Index);
        entry.Active = false;
        entry.EntryVersion++;
        if (entry.EntryVersion == 0) entry.EntryVersion = 1;
        _freeList.Push(handle.Index);
        
        return true;
    }

    public void Tick(float deltaTime)
    {
        _accumulator += deltaTime;
        while (_accumulator >= _tickDuration)
        {
            _accumulator -= _tickDuration;
            _currentTick++;
            
            int slot = (int)(_currentTick % _options.WheelSize);
            int head = _wheel[slot];
            _wheel[slot] = -1;

            int current = head;
            int processed = 0;
            while (current != -1 && processed < _options.MaxExpiredPerTick)
            {
                ref var entry = ref _pool[current];
                int next = entry.Next;
                
                if (entry.Active)
                {
                    // Check if it's actually due (not just same slot but future cycle)
                    // Given that DelayBuffer is for short TTL, we might assume cycle 0?
                    // No, let's check ExpireTick to be safe.
                    if (entry.ExpireTick <= _currentTick)
                    {
                        _manager.ExpirePublisher(entry.PublisherId, entry.ValueVersion);
                        
                        entry.Active = false;
                        entry.EntryVersion++;
                        if (entry.EntryVersion == 0) entry.EntryVersion = 1;
                        _freeList.Push(current);
                        processed++;
                    }
                    else
                    {
                        // Put back in wheel for next cycle
                        entry.Next = _wheel[slot];
                        entry.Prev = -1;
                        if (_wheel[slot] != -1) _pool[_wheel[slot]].Prev = current;
                        _wheel[slot] = current;
                    }
                }
                
                current = next;
            }

            if (current != -1)
            {
                RequeueRemaining(slot, current);
            }
        }
    }

    private void RequeueRemaining(int slot, int head)
    {
        var targetSlot = (slot + 1) % _options.WheelSize;
        _pool[head].Prev = -1;
        var tail = head;
        while (true)
        {
            _pool[tail].SlotIndex = targetSlot;
            if (_pool[tail].Next == -1) break;
            tail = _pool[tail].Next;
        }

        _pool[tail].Next = _wheel[targetSlot];
        if (_wheel[targetSlot] != -1)
        {
            _pool[_wheel[targetSlot]].Prev = tail;
        }

        _wheel[targetSlot] = head;
    }

    private void RemoveFromWheel(int index)
    {
        ref var entry = ref _pool[index];
        if (entry.Prev != -1)
            _pool[entry.Prev].Next = entry.Next;
        else
            _wheel[entry.SlotIndex] = entry.Next;
            
        if (entry.Next != -1)
            _pool[entry.Next].Prev = entry.Prev;
    }

    private void GrowPool()
    {
        int oldSize = _poolSize;
        int newSize = oldSize * 2;
        Array.Resize(ref _pool, newSize);
        for (int i = newSize - 1; i >= oldSize; i--)
        {
            _pool[i].EntryVersion = 1;
            _freeList.Push(i);
        }
        _poolSize = newSize;
    }
}
