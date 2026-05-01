using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Numerics;
using LayerBase.Core.DataStruct;

namespace LayerBase.Core.Event;

public sealed class PostScheduler : IDisposable
{
    private readonly int _runtimeId;
    private readonly PostSchedulerOptions _options;
    public PostSchedulerOptions Options => _options;
    private readonly RingBuffer<PostItem> _readyQueue;
    private readonly RingBuffer<PostItem> _nextQueue;
    private readonly EventPayloadStorage _payloadStorage;
    private readonly EventCenter _eventCenter;
    private readonly EventRuntimePolicyTable _policyTable;
    
    // Optimized Buffers
    private ulong[] _dirtyPendingBits = Array.Empty<ulong>();
    private ulong[] _latestPendingBits = Array.Empty<ulong>();
    
    // Coalesced: Data Coalescing (Payload merging) - Keep for now, but in slow path
    private readonly Dictionary<CoalescedSlotKey, CoalescedSlot> _coalescedBuffer = new();
    private readonly List<CoalescedSlotKey> _pendingCoalesced = new();
    
    // Latest: Data Coalescing (Last payload only)
    private PayloadHandle[] _latestBuffer = new PayloadHandle[256];
    
    private int[] _pendingCount = new int[256];
    private PostTypePlan[] _postPlans = Array.Empty<PostTypePlan>();
    private PostBitmap _postBitmap = new();
    
    private readonly object _bufferLock = new();
    private long _sequenceCounter;
    private bool _disposed;
    private bool _isPumping;
    private readonly BackpressurePolicy _defaultBackpressure;

    public PostScheduler(int runtimeId, EventCenter eventCenter, PostSchedulerOptions options, EventRuntimePolicyTable policyTable)
    {
        _runtimeId = runtimeId;
        _eventCenter = eventCenter;
        _options = options;
        _policyTable = policyTable;
        _defaultBackpressure = options.DefaultBackpressure;
        _readyQueue = new RingBuffer<PostItem>(options.ReadyCapacity);
        _nextQueue = new RingBuffer<PostItem>(options.NextCapacity);
        _payloadStorage = new EventPayloadStorage();
        
        for (int i = 0; i < _latestBuffer.Length; i++) _latestBuffer[i] = PayloadHandle.Invalid;
    }

    public void BuildPlans(ReadOnlySpan<PostTypePlan> plans)
    {
        var maxTypeId = EventTypeIdAllocator.MaxId;
        foreach (var p in plans) if (p.EventTypeId > maxTypeId) maxTypeId = p.EventTypeId;

        _postPlans = new PostTypePlan[maxTypeId + 1];
        // Initialize with default plans for all IDs
        for (int i = 0; i < _postPlans.Length; i++)
        {
            _postPlans[i] = new PostTypePlan(i, PostDeliveryMode.Normal, _defaultBackpressure, 0, _defaultBackpressure);
        }

        foreach (var p in plans)
        {
            _postPlans[p.EventTypeId] = p;
        }

        _postBitmap.Build(_postPlans); // Build from full array

        int segmentCount = (maxTypeId >> 6) + 1;
        _dirtyPendingBits = new ulong[segmentCount];
        _latestPendingBits = new ulong[segmentCount];

        if (maxTypeId >= _latestBuffer.Length)
        {
            var oldLatest = _latestBuffer;
            _latestBuffer = new PayloadHandle[maxTypeId + 1];
            Array.Copy(oldLatest, _latestBuffer, oldLatest.Length);
            for (int i = oldLatest.Length; i < _latestBuffer.Length; i++) _latestBuffer[i] = PayloadHandle.Invalid;
        }

        if (maxTypeId >= _pendingCount.Length)
        {
            Array.Resize(ref _pendingCount, maxTypeId + 1);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult TryPost<T>(in T value, EventPostPolicy? policyOverride = null) where T : struct
    {
        if (_disposed) return FailSchedulerDisposed();

        if (policyOverride.HasValue)
            return TryPostOverride(in value, policyOverride.Value);

        var typeId = EventTypeId<T>.Id;

        if (!_postBitmap.IsSpecial(typeId))
            return EnqueueNormalFast(typeId, in value);

        return TryPostSpecial(typeId, in value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult TryPostOverride<T>(in T value, EventPostPolicy policy) where T : struct
    {
        var typeId = EventTypeId<T>.Id;
        switch (policy.Mode)
        {
            case PostDeliveryMode.Normal:
                var plan = new PostTypePlan(typeId, policy.Mode, policy.Backpressure, policy.MaxPending, _defaultBackpressure);
                return EnqueueNormalWithPlan(typeId, in value, in plan);
            case PostDeliveryMode.DirtySignal:
                return MarkDirtyById<T>(typeId);
            case PostDeliveryMode.Coalesced:
                return EnqueueCoalescedInternal(typeId, in value);
            case PostDeliveryMode.Latest:
                return EnqueueLatestInternal(typeId, in value);
            default:
                return PostResult.Failure("Unknown delivery mode");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult FailSchedulerDisposed()
    {
        return PostResult.Failure("Scheduler disposed");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult EnqueueNormalFast<T>(int typeId, in T value) where T : struct
    {
        var store = _payloadStorage.GetStoreFast<T>(_runtimeId);
        var handle = store.Add(in value);
        var sequenceId = Interlocked.Increment(ref _sequenceCounter);
        var item = new PostItem(typeId, handle, sequenceId, _defaultBackpressure);

        var targetQueue = _isPumping ? _nextQueue : _readyQueue;
        if (targetQueue.TryEnqueue(in item))
            return PostResult.Success;

        return HandleQueueFullSlow(in item, store);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult HandleQueueFullSlow<T>(in PostItem item, EventStore<T> store) where T : struct
    {
        return HandleQueueFullInternal(in item);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult TryPostSpecial<T>(int typeId, in T value) where T : struct
    {
        if (_postBitmap.IsDirty(typeId))
            return MarkDirtyById<T>(typeId);

        if (_postBitmap.IsLatest(typeId))
            return EnqueueLatestInternal(typeId, in value);

        if (_postBitmap.IsCoalesced(typeId))
            return EnqueueCoalescedInternal(typeId, in value);

        // Normal + TrackPending or custom backpressure
        ref readonly var plan = ref GetPlan(typeId);
        return EnqueueNormalWithPlan(typeId, in value, in plan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref readonly PostTypePlan GetPlan(int typeId)
    {
        return ref FastArray.At(_postPlans, typeId);
    }

    private PostResult EnqueueNormalWithPlan<T>(int typeId, in T value, in PostTypePlan plan) where T : struct
    {
        if (plan.TrackPending)
        {
            lock (_bufferLock)
            {
                if (typeId < _pendingCount.Length && FastArray.At(_pendingCount, typeId) >= plan.MaxPending)
                {
                    return PostResult.Failure($"Max pending reached for event type {typeId}");
                }
            }
        }

        var store = _payloadStorage.GetStoreFast<T>(_runtimeId);
        var handle = store.Add(in value);
        var sequenceId = Interlocked.Increment(ref _sequenceCounter);
        var item = new PostItem(typeId, handle, sequenceId, plan.Backpressure);

        var result = EnqueueItemWithPolicy(in item);
        if (result.IsSuccess && plan.TrackPending)
        {
            lock (_bufferLock)
            {
                FastArray.At(_pendingCount, typeId)++;
            }
        }
        return result;
    }

    public PostResult MarkDirty<T>() where T : struct
    {
        if (_disposed) return FailSchedulerDisposed();
        var typeId = EventTypeId<T>.Id;
        return MarkDirtyById<T>(typeId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult MarkDirtyById<T>(int typeId) where T : struct
    {
        var segment = typeId >> 6;
        var bit = 1UL << (typeId & 63);

        lock (_bufferLock)
        {
            if (segment < _dirtyPendingBits.Length && (FastArray.At(_dirtyPendingBits, segment) & bit) == 0)
            {
                FastArray.At(_dirtyPendingBits, segment) |= bit;
                _payloadStorage.EnsureStore<T>(_runtimeId);
            }
        }
        return PostResult.Coalesced();
    }

    private PostResult EnqueueLatestInternal<T>(int typeId, in T value) where T : struct
    {
        var segment = typeId >> 6;
        var bit = 1UL << (typeId & 63);

        lock (_bufferLock)
        {
            if (typeId < _latestBuffer.Length)
            {
                ref var handleRef = ref FastArray.At(_latestBuffer, typeId);
                if (!handleRef.IsInvalid)
                {
                    _payloadStorage.Release(handleRef);
                }
                
                handleRef = _payloadStorage.Store(_runtimeId, value);
                if (segment < _latestPendingBits.Length)
                {
                    FastArray.At(_latestPendingBits, segment) |= bit;
                }
                return PostResult.Success;
            }
            return PostResult.Failure("Event type not registered during build.");
        }
    }

    private PostResult EnqueueCoalescedInternal<T>(int typeId, in T value) where T : struct
    {
        var meta = _policyTable.GetMetaData<T>(typeId);
        int coalesceKey = meta?.GetPostCoalesceKey(value) ?? 0;
        var slotKey = new CoalescedSlotKey(typeId, coalesceKey);

        lock (_bufferLock)
        {
            if (_coalescedBuffer.TryGetValue(slotKey, out var slot))
            {
                ref T current = ref _payloadStorage.GetRef<T>(_runtimeId, slot.PayloadHandle);
                if (meta != null && meta.TryMergePostEvent(ref current, in value))
                {
                    slot.LastSequenceId = Interlocked.Increment(ref _sequenceCounter);
                    slot.MergeCount++;
                    _coalescedBuffer[slotKey] = slot;
                    return PostResult.Coalesced();
                }

                // Merge failed
                if (typeId < _postPlans.Length)
                {
                    ref readonly var plan = ref GetPlan(typeId);
                    return HandleMergeFailureInternal(slotKey, value, plan.Backpressure);
                }

                return PostResult.Failure("Merge failed and plan not found.");
            }

            // New slot
            var handle = _payloadStorage.Store(_runtimeId, value);
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

    private PostResult HandleMergeFailureInternal<T>(CoalescedSlotKey slotKey, in T value, BackpressurePolicy backpressure) where T : struct
    {
        return PostResult.Failure("Merge failed");
    }

    private PostResult EnqueueItemWithPolicy(in PostItem item)
    {
        var targetQueue = _isPumping ? _nextQueue : _readyQueue;
        if (targetQueue.TryEnqueue(in item))
            return PostResult.Success;

        return HandleQueueFullInternal(in item);
    }

    private PostResult HandleQueueFullInternal(in PostItem item)
    {
        var targetQueue = _isPumping ? _nextQueue : _readyQueue;
        switch (item.Policy)
        {
            case BackpressurePolicy.RejectNew:
                _payloadStorage.Release(item.PayloadHandle);
                return PostResult.Failure("Queue full");
            case BackpressurePolicy.DropNewest:
                _payloadStorage.Release(item.PayloadHandle);
                return PostResult.Success; 
            case BackpressurePolicy.DropOldest:
                if (targetQueue.TryDequeue(out var oldItem))
                {
                    DecrementPendingCount(oldItem.EventTypeId);
                    _payloadStorage.Release(oldItem.PayloadHandle);
                    if (targetQueue.TryEnqueue(item))
                        return PostResult.Success;
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
        if (typeId >= _postPlans.Length) return;
        
        var plan = _postPlans[typeId];
        if (plan.TrackPending)
        {
            lock (_bufferLock)
            {
                if (typeId < _pendingCount.Length) FastArray.At(_pendingCount, typeId)--;
            }
        }
    }

    private int FlushBuffers()
    {
        int count = 0;
        lock (_bufferLock)
        {
            // 1. Dirty Signals
            for (int i = 0; i < _dirtyPendingBits.Length; i++)
            {
                var bits = FastArray.At(_dirtyPendingBits, i);
                if (bits == 0) continue;
                FastArray.At(_dirtyPendingBits, i) = 0;

                while (bits != 0)
                {
                    var bitIndex = BitHelper.TrailingZeroCount(bits);
                    var typeId = (i << 6) + bitIndex;
                    _payloadStorage.DispatchDefault(typeId, _eventCenter);
                    count++;
                    bits &= bits - 1;
                }
            }
            
            // 2. Coalesced (Data Merging)
            if (_pendingCoalesced.Count > 0)
            {
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
            for (int i = 0; i < _latestPendingBits.Length; i++)
            {
                var bits = FastArray.At(_latestPendingBits, i);
                if (bits == 0) continue;
                FastArray.At(_latestPendingBits, i) = 0;

                while (bits != 0)
                {
                    var bitIndex = BitHelper.TrailingZeroCount(bits);
                    var typeId = (i << 6) + bitIndex;
                    ref var handleRef = ref FastArray.At(_latestBuffer, typeId);
                    var handle = handleRef;
                    handleRef = PayloadHandle.Invalid;
                    _payloadStorage.Dispatch(handle, _eventCenter);
                    _payloadStorage.Release(handle);
                    count++;
                    bits &= bits - 1;
                }
            }
        }
        return count;
    }

    public PostPumpStats Pump()
    {
        if (_disposed) return new PostPumpStats(0, 0, 0, 0);

        long startTimestamp = 0;
        if (_options.MaxMillisecondsPerPump > 0) startTimestamp = Stopwatch.GetTimestamp();
        
        int processed = 0;
        int wavesProcessed = 0;
        
        processed += FlushBuffers();

        _isPumping = true;
        try
        {
            if (_readyQueue.IsEmpty && !_nextQueue.IsEmpty) PromoteNextToReady();

            while (!_readyQueue.IsEmpty)
            {
                wavesProcessed++;
                int currentWaveCount = _readyQueue.Count;
                for (int i = 0; i < currentWaveCount; i++)
                {
                    if (!_readyQueue.TryDequeue(out var item)) break;
                    
                    DispatchItem(in item);
                    processed++;

                    if (_options.MaxEventsPerPump > 0 && processed >= _options.MaxEventsPerPump)
                        goto EndPump;

                    if (_options.MaxMillisecondsPerPump > 0 && processed % _options.TimeCheckInterval == 0)
                    {
                        var elapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
                        if (elapsedMs >= _options.MaxMillisecondsPerPump)
                            goto EndPump;
                    }
                }
                
                if (wavesProcessed < _options.MaxWavesPerPump && !_nextQueue.IsEmpty)
                    PromoteNextToReady();
                else
                    break;
            }
        }
        finally
        {
            _isPumping = false;
        }

    EndPump:
        var totalElapsedMs = 0.0;
        if (startTimestamp != 0) totalElapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
        
        return new PostPumpStats(processed, totalElapsedMs, _readyQueue.Count + _nextQueue.Count, wavesProcessed);
    }

    private void PromoteNextToReady()
    {
        while (!_nextQueue.IsEmpty && !_readyQueue.IsFull)
        {
            if (_nextQueue.TryDequeue(out var item))
                _readyQueue.TryEnqueue(in item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DispatchItem(in PostItem item)
    {
        _payloadStorage.Dispatch(item.PayloadHandle, _eventCenter);
        _payloadStorage.Release(item.PayloadHandle);
        DecrementPendingCount(item.EventTypeId);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        lock (_bufferLock)
        {
            for (int i = 0; i < _latestPendingBits.Length; i++)
            {
                var bits = FastArray.At(_latestPendingBits, i);
                while (bits != 0)
                {
                    var bitIndex = BitHelper.TrailingZeroCount(bits);
                    var typeId = (i << 6) + bitIndex;
                    _payloadStorage.Release(_latestBuffer[typeId]);
                    bits &= bits - 1;
                }
            }

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
