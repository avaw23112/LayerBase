using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LayerBase.Core.DataStruct;

namespace LayerBase.Core.Event;

public sealed class PostScheduler : IDisposable
{
    private readonly PostSchedulerOptions _options;
    private readonly RingBuffer<PostItem> _readyQueue;
    private readonly RingBuffer<PostItem> _nextQueue;
    private readonly EventPayloadStorage _payloadStorage;
    private readonly GlobalEventCenter _eventCenter;
    private readonly EventRuntimePolicyTable _policyTable;
    
    // DirtySignal: Signal Coalescing (Signal only, no payload)
    private BitArray _dirtySignalBuffer = new(256);
    private readonly List<int> _pendingDirtySignals = new();
    
    // Coalesced: Data Coalescing (Payload merging)
    private readonly Dictionary<CoalescedSlotKey, CoalescedSlot> _coalescedBuffer = new();
    private readonly List<CoalescedSlotKey> _pendingCoalesced = new();
    
    // Latest: Data Coalescing (Last payload only)
    private PayloadHandle[] _latestBuffer = new PayloadHandle[256];
    private readonly List<int> _pendingLatest = new();
    
    private int[] _pendingCount = new int[256];
    
    private readonly object _bufferLock = new();
    private long _sequenceCounter;
    private bool _disposed;
    private bool _isPumping;

    public PostScheduler(GlobalEventCenter eventCenter, PostSchedulerOptions options, EventRuntimePolicyTable policyTable)
    {
        _eventCenter = eventCenter;
        _options = options;
        _policyTable = policyTable;
        _readyQueue = new RingBuffer<PostItem>(options.ReadyCapacity);
        _nextQueue = new RingBuffer<PostItem>(options.NextCapacity);
        _payloadStorage = new EventPayloadStorage();
        
        for (int i = 0; i < _latestBuffer.Length; i++) _latestBuffer[i] = PayloadHandle.Invalid;
    }

    public PostResult TryPost<T>(in T value, EventPostPolicy? policyOverride = null) where T : struct
    {
        if (_disposed) return PostResult.Failure("Scheduler disposed");

        var typeId = EventTypeId<T>.Id;
        var policy = policyOverride ?? _policyTable.GetPostPolicy(typeId);
        
        switch (policy.Mode)
        {
            case PostDeliveryMode.Normal:
                return EnqueueNormal(typeId, value, policy);
            case PostDeliveryMode.DirtySignal:
                return MarkDirty<T>();
            case PostDeliveryMode.Coalesced:
                return EnqueueCoalesced(typeId, value, policy);
            case PostDeliveryMode.Latest:
                return EnqueueLatest(typeId, value);
            default:
                return PostResult.Failure("Unknown delivery mode");
        }
    }

    public PostResult MarkDirty<T>() where T : struct
    {
        if (_disposed) return PostResult.Failure("Scheduler disposed");
        
        var typeId = EventTypeId<T>.Id;
        lock (_bufferLock)
        {
            if (typeId >= _dirtySignalBuffer.Length) ExpandDirtySignalBuffer(typeId);
            if (!_dirtySignalBuffer[typeId])
            {
                _dirtySignalBuffer.Set(typeId, true);
                _pendingDirtySignals.Add(typeId);
                _payloadStorage.EnsureStore<T>();
            }
            return PostResult.Coalesced();
        }
    }

    private PostResult EnqueueNormal<T>(int typeId, in T value, EventPostPolicy policy) where T : struct
    {
        if (policy.MaxPending > 0)
        {
            lock (_bufferLock)
            {
                if (typeId < _pendingCount.Length && _pendingCount[typeId] >= policy.MaxPending)
                {
                    return PostResult.Failure($"Max pending reached for event type {typeId}");
                }
            }
        }

        var handle = _payloadStorage.Store(value);
        var sequenceId = Interlocked.Increment(ref _sequenceCounter);
        var item = new PostItem(typeId, handle, sequenceId, policy.Backpressure);

        var result = EnqueueItem(item);
        if (result.IsSuccess)
        {
            lock (_bufferLock)
            {
                if (typeId >= _pendingCount.Length) ExpandPendingCount(typeId);
                _pendingCount[typeId]++;
            }
        }
        return result;
    }

    private PostResult EnqueueCoalesced<T>(int typeId, in T value, EventPostPolicy policy) where T : struct
    {
        var meta = _policyTable.GetMetaData<T>(typeId);
        int coalesceKey = meta?.GetPostCoalesceKey(value) ?? 0;
        var slotKey = new CoalescedSlotKey(typeId, coalesceKey);

        lock (_bufferLock)
        {
            if (_coalescedBuffer.TryGetValue(slotKey, out var slot))
            {
                ref T current = ref _payloadStorage.GetRef<T>(slot.PayloadHandle);
                if (meta != null && meta.TryMergePostEvent(ref current, in value))
                {
                    slot.LastSequenceId = Interlocked.Increment(ref _sequenceCounter);
                    slot.MergeCount++;
                    _coalescedBuffer[slotKey] = slot;
                    return PostResult.Coalesced();
                }

                // Merge failed
                return HandleMergeFailure(slotKey, value, policy.MergeFailure);
            }

            // New slot
            var handle = _payloadStorage.Store(value);
            var seq = Interlocked.Increment(ref _sequenceCounter);
            var newSlot = new CoalescedSlot
            {
                Key = slotKey,
                PayloadHandle = handle,
                FirstSequenceId = seq,
                LastSequenceId = seq,
                MergeCount = 1,
                Active = true
            };
            _coalescedBuffer[slotKey] = newSlot;
            _pendingCoalesced.Add(slotKey);
            return PostResult.Enqueued();
        }
    }

    private PostResult HandleMergeFailure<T>(CoalescedSlotKey slotKey, in T value, MergeFailurePolicy failurePolicy) where T : struct
    {
        switch (failurePolicy)
        {
            case MergeFailurePolicy.Reject:
                return PostResult.Failure("Merge failed and policy is Reject");
            case MergeFailurePolicy.FallbackToLatest:
                // Effectively overwrite
                if (_coalescedBuffer.TryGetValue(slotKey, out var slot))
                {
                    _payloadStorage.Release(slot.PayloadHandle);
                    slot.PayloadHandle = _payloadStorage.Store(value);
                    slot.LastSequenceId = Interlocked.Increment(ref _sequenceCounter);
                    slot.MergeCount++;
                    _coalescedBuffer[slotKey] = slot;
                    return PostResult.Coalesced();
                }
                return PostResult.Failure("Slot missing during fallback");
            case MergeFailurePolicy.FallbackToNormal:
                // This is complex as it requires mixing modes. For now, we'll just treat it as normal post.
                return EnqueueNormal(slotKey.EventTypeId, value, _policyTable.GetPostPolicy(slotKey.EventTypeId));
            default:
                return PostResult.Failure("Unknown merge failure policy");
        }
    }

    private PostResult EnqueueLatest<T>(int typeId, in T value) where T : struct
    {
        lock (_bufferLock)
        {
            if (typeId >= _latestBuffer.Length) ExpandBuffers(typeId);
            
            if (!_latestBuffer[typeId].IsInvalid)
            {
                _payloadStorage.Release(_latestBuffer[typeId]);
            }
            else
            {
                _pendingLatest.Add(typeId);
            }
            
            _latestBuffer[typeId] = _payloadStorage.Store(value);
            return PostResult.Success;
        }
    }

    private PostResult EnqueueItem(in PostItem item)
    {
        var targetQueue = _isPumping ? _nextQueue : _readyQueue;
        
        if (targetQueue.TryEnqueue(item))
        {
            return PostResult.Success;
        }

        switch (item.Policy)
        {
            case BackpressurePolicy.RejectNew:
                _payloadStorage.Release(item.PayloadHandle);
                return PostResult.Failure("Queue full");
            case BackpressurePolicy.DropNewest:
                _payloadStorage.Release(item.PayloadHandle);
                return PostResult.Success; 
            case BackpressurePolicy.DropOldest:
                if (targetQueue.Count > 0)
                {
                    if (targetQueue.TryDequeue(out var oldItem))
                    {
                        DecrementPendingCount(oldItem.EventTypeId);
                        _payloadStorage.Release(oldItem.PayloadHandle);
                        if (targetQueue.TryEnqueue(item))
                        {
                            return PostResult.Success;
                        }
                    }
                }
                _payloadStorage.Release(item.PayloadHandle);
                return PostResult.Failure("Queue full (even after drop oldest)");
            default:
                _payloadStorage.Release(item.PayloadHandle);
                return PostResult.Failure($"Unsupported backpressure policy: {item.Policy}");
        }
    }

    private void DecrementPendingCount(int typeId)
    {
        lock (_bufferLock)
        {
            if (typeId < _pendingCount.Length) _pendingCount[typeId]--;
        }
    }

    public PostPumpStats Pump()
    {
        if (_disposed) return new PostPumpStats(0, 0, 0, 0);

        var stopwatch = Stopwatch.StartNew();
        int processed = 0;
        int wavesProcessed = 0;
        
        processed += FlushBuffers();

        _isPumping = true;
        try
        {
            if (_readyQueue.IsEmpty && !_nextQueue.IsEmpty)
            {
                PromoteNextToReady();
            }

            while (!_readyQueue.IsEmpty)
            {
                wavesProcessed++;
                int currentWaveCount = _readyQueue.Count;
                for (int i = 0; i < currentWaveCount; i++)
                {
                    if (!_readyQueue.TryDequeue(out var item)) break;
                    
                    DispatchItem(item);
                    processed++;

                    if (_options.MaxEventsPerPump > 0 && processed >= _options.MaxEventsPerPump)
                        goto EndPump;

                    if (_options.MaxMillisecondsPerPump > 0 && processed % _options.TimeCheckInterval == 0)
                    {
                        if (stopwatch.Elapsed.TotalMilliseconds >= _options.MaxMillisecondsPerPump)
                            goto EndPump;
                    }
                }
                
                if (wavesProcessed < _options.MaxWavesPerPump && !_nextQueue.IsEmpty)
                {
                    PromoteNextToReady();
                }
                else
                {
                    break;
                }
            }
        }
        finally
        {
            _isPumping = false;
        }

    EndPump:
        stopwatch.Stop();
        return new PostPumpStats(processed, stopwatch.Elapsed.TotalMilliseconds, _readyQueue.Count + _nextQueue.Count, wavesProcessed);
    }

    private int FlushBuffers()
    {
        int count = 0;
        lock (_bufferLock)
        {
            // 1. Dirty Signals
            if (_pendingDirtySignals.Count > 0)
            {
                foreach (var typeId in _pendingDirtySignals)
                {
                    _payloadStorage.DispatchDefault(typeId, _eventCenter);
                    _dirtySignalBuffer.Set(typeId, false);
                    count++;
                }
                _pendingDirtySignals.Clear();
            }
            
            // 2. Coalesced (Data Merging)
            if (_pendingCoalesced.Count > 0)
            {
                // Sort by FirstSequenceId to maintain original appearance order between slots
                _pendingCoalesced.Sort((a, b) => _coalescedBuffer[a].FirstSequenceId.CompareTo(_coalescedBuffer[b].FirstSequenceId));
                
                foreach (var key in _pendingCoalesced)
                {
                    var slot = _coalescedBuffer[key];
                    _payloadStorage.Dispatch(slot.PayloadHandle, _eventCenter);
                    _payloadStorage.Release(slot.PayloadHandle);
                    _coalescedBuffer.Remove(key);
                    count++;
                }
                _pendingCoalesced.Clear();
            }

            // 3. Latest
            if (_pendingLatest.Count > 0)
            {
                foreach (var typeId in _pendingLatest)
                {
                    var handle = _latestBuffer[typeId];
                    _latestBuffer[typeId] = PayloadHandle.Invalid;
                    _payloadStorage.Dispatch(handle, _eventCenter);
                    _payloadStorage.Release(handle);
                    count++;
                }
                _pendingLatest.Clear();
            }
        }
        return count;
    }

    private void PromoteNextToReady()
    {
        while (!_nextQueue.IsEmpty && !_readyQueue.IsFull)
        {
            _nextQueue.TryDequeue(out var item);
            _readyQueue.TryEnqueue(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DispatchItem(in PostItem item)
    {
        _payloadStorage.Dispatch(item.PayloadHandle, _eventCenter);
        _payloadStorage.Release(item.PayloadHandle);
        DecrementPendingCount(item.EventTypeId);
    }

    private void ExpandDirtySignalBuffer(int minCapacity)
    {
        int newSize = Math.Max(minCapacity + 1, _dirtySignalBuffer.Length * 2);
        _dirtySignalBuffer.Length = newSize;
    }

    private void ExpandBuffers(int minCapacity)
    {
        int newSize = Math.Max(minCapacity + 1, _latestBuffer.Length * 2);
        
        var newLatest = new PayloadHandle[newSize];
        Array.Copy(_latestBuffer, newLatest, _latestBuffer.Length);
        for (int i = _latestBuffer.Length; i < newSize; i++) newLatest[i] = PayloadHandle.Invalid;
        _latestBuffer = newLatest;
    }

    private void ExpandPendingCount(int minCapacity)
    {
        int newSize = Math.Max(minCapacity + 1, _pendingCount.Length * 2);
        Array.Resize(ref _pendingCount, newSize);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        lock (_bufferLock)
        {
            foreach (var typeId in _pendingLatest)
            {
                _payloadStorage.Release(_latestBuffer[typeId]);
            }
            _pendingLatest.Clear();

            foreach (var key in _pendingCoalesced)
            {
                _payloadStorage.Release(_coalescedBuffer[key].PayloadHandle);
            }
            _pendingCoalesced.Clear();
            _coalescedBuffer.Clear();
        }
        
        _payloadStorage.Dispose();
        _readyQueue.Clear();
        _nextQueue.Clear();
    }
}
