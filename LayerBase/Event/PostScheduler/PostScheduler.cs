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

    // Snapshot Buffers for Reentrancy Safety
    private ulong[] _dirtySnapshotBits = Array.Empty<ulong>();
    private ulong[] _latestSnapshotBits = Array.Empty<ulong>();
    private readonly List<CoalescedSlot> _snapshotCoalesced = new();
    private PayloadHandle[] _latestSnapshotBuffer = new PayloadHandle[256];

    // Coalesced: Data Coalescing (Payload merging) - Keep for now, but in slow path
    private readonly Dictionary<CoalescedSlotKey, CoalescedSlot> _coalescedBuffer = new();
    private readonly List<CoalescedSlotKey> _pendingCoalesced = new();

    // Latest: Data Coalescing (Last payload only)
    private PayloadHandle[] _latestBuffer = new PayloadHandle[256];

    private int[] _pendingCount = new int[256];
    private PostTypePlan[] _postPlans = Array.Empty<PostTypePlan>();
    private PostBitmap _postBitmap = new();

    private readonly object _bufferLock = new();
    private readonly object _queueLock = new();
    private long _sequenceCounter;
    private int _sealedMaxEventTypeId = -1;
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
        _sealedMaxEventTypeId = maxTypeId;

        _postPlans = new PostTypePlan[maxTypeId + 1];
        // Initialize with default plans for all IDs
        for (int i = 0; i < _postPlans.Length; i++)
        {
            _postPlans[i] = new PostTypePlan(i, PostDeliveryMode.Normal, _defaultBackpressure, 0, _defaultBackpressure, MergeFailurePolicy.Reject);
        }

        foreach (var p in plans)
        {
            _postPlans[p.EventTypeId] = p;
        }

        _postBitmap.Build(_postPlans); // Build from full array

        int segmentCount = (maxTypeId >> 6) + 1;
        _dirtyPendingBits = new ulong[segmentCount];
        _latestPendingBits = new ulong[segmentCount];
        _dirtySnapshotBits = new ulong[segmentCount];
        _latestSnapshotBits = new ulong[segmentCount];

        if (maxTypeId >= _latestBuffer.Length)
        {
            var oldLatest = _latestBuffer;
            _latestBuffer = new PayloadHandle[maxTypeId + 1];
            Array.Copy(oldLatest, _latestBuffer, oldLatest.Length);
            for (int i = oldLatest.Length; i < _latestBuffer.Length; i++) _latestBuffer[i] = PayloadHandle.Invalid;
            
            _latestSnapshotBuffer = new PayloadHandle[maxTypeId + 1];
            for (int i = 0; i < _latestSnapshotBuffer.Length; i++) _latestSnapshotBuffer[i] = PayloadHandle.Invalid;
        }

        if (maxTypeId >= _pendingCount.Length)
        {
            Array.Resize(ref _pendingCount, maxTypeId + 1);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult TryPostLatest<T>(in T value) where T : struct
    {
        if (_disposed) return FailSchedulerDisposed();
        var typeId = EventTypeId<T>.Id;
        if (!IsKnownEventType(typeId)) return FailEventTypeNotRegistered<T>();
        return EnqueueLatestInternal(typeId, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult TryPostCoalesced<T>(in T value) where T : struct
    {
        if (_disposed) return FailSchedulerDisposed();
        var typeId = EventTypeId<T>.Id;
        if (!IsKnownEventType(typeId)) return FailEventTypeNotRegistered<T>();
        return EnqueueCoalescedInternal(typeId, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult TryPost<T>(in T value, EventPostPolicy? policyOverride = null) where T : struct
    {
        if (_disposed) return FailSchedulerDisposed();

        var typeId = EventTypeId<T>.Id;
        if (!IsKnownEventType(typeId)) return FailEventTypeNotRegistered<T>();

        if (policyOverride.HasValue)
            return TryPostOverride(in value, policyOverride.Value);

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
                var plan = new PostTypePlan(typeId, policy.Mode, policy.Backpressure, policy.MaxPending, _defaultBackpressure, policy.MergeFailure);
                return EnqueueNormalWithPlan(typeId, in value, in plan);
            case PostDeliveryMode.DirtySignal:
                if (!IsKnownEventType(typeId)) return FailEventTypeNotRegistered<T>();
                return MarkDirtyById<T>(typeId);
            case PostDeliveryMode.Coalesced:
                if (!IsKnownEventType(typeId)) return FailEventTypeNotRegistered<T>();
                return EnqueueCoalescedInternal(typeId, in value);
            case PostDeliveryMode.Latest:
                if (!IsKnownEventType(typeId)) return FailEventTypeNotRegistered<T>();
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
            return PostResult.Enqueued();

        return HandleQueueFullSlow(in item, store, targetQueue);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult HandleQueueFullSlow<T>(in PostItem item, EventStore<T> store, RingBuffer<PostItem> targetQueue) where T : struct
    {
        return HandleQueueFullInternal(in item, targetQueue);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult TryPostSpecial<T>(int typeId, in T value) where T : struct
    {
        if (!IsKnownEventType(typeId))
            return FailEventTypeNotRegistered<T>();

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
            if (typeId < _pendingCount.Length && FastArray.At(_pendingCount, typeId) >= plan.MaxPending)
            {
                return PostResult.Failure($"Max pending reached for event type {typeId}");
            }

            FastArray.At(_pendingCount, typeId)++;
        }

        var store = _payloadStorage.GetStoreFast<T>(_runtimeId);
        var handle = store.Add(in value);
        var sequenceId = Interlocked.Increment(ref _sequenceCounter);
        var item = new PostItem(typeId, handle, sequenceId, plan.Backpressure);

        var result = EnqueueItemWithPolicy(in item);
        if (!result.CountsAsPending && plan.TrackPending)
        {
            DecrementPendingCount(typeId);
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
        if (!IsKnownEventType(typeId))
            return FailEventTypeNotRegistered<T>();

        var segment = typeId >> 6;
        var bit = 1UL << (typeId & 63);

        if (segment >= _dirtyPendingBits.Length)
            return PostResult.Failure($"Dirty buffer is not initialized for event type {typeof(T).Name}.");

        if ((FastArray.At(_dirtyPendingBits, segment) & bit) == 0)
        {
            FastArray.At(_dirtyPendingBits, segment) |= bit;
            _payloadStorage.EnsureStore<T>(_runtimeId);
        }
        return PostResult.Coalesced();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsKnownEventType(int typeId)
    {
        return typeId <= _sealedMaxEventTypeId;
    }

    public void AddSpecialPolicy(int typeId, EventPostPolicy policy)
    {
        if (typeId >= _postPlans.Length)
        {
            var newSize = Math.Max(typeId + 1, _postPlans.Length * 2);
            var newPlans = new PostTypePlan[newSize];
            Array.Copy(_postPlans, newPlans, _postPlans.Length);
            for (int i = _postPlans.Length; i < newPlans.Length; i++)
            {
                newPlans[i] = new PostTypePlan(i, PostDeliveryMode.Normal, _defaultBackpressure, 0, _defaultBackpressure, MergeFailurePolicy.Reject);
            }
            _postPlans = newPlans;
        }
        
        _postPlans[typeId] = new PostTypePlan(typeId, policy.Mode, policy.Backpressure, policy.MaxPending, _defaultBackpressure, policy.MergeFailure);
        _postBitmap.Build(_postPlans);
        if (typeId > _sealedMaxEventTypeId) _sealedMaxEventTypeId = typeId;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PostResult FailEventTypeNotRegistered<T>() where T : struct
    {
        return PostResult.Failure($"Event type {typeof(T).Name} was not registered before Build.");
    }

    private PostResult EnqueueLatestInternal<T>(int typeId, in T value) where T : struct
    {
        var segment = typeId >> 6;
        var bit = 1UL << (typeId & 63);
 
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

    private PostResult EnqueueCoalescedInternal<T>(int typeId, in T value) where T : struct
    {
        var meta = _policyTable.GetMetaData<T>(typeId);
        int coalesceKey = meta?.GetPostCoalesceKey(value) ?? 0;
        var slotKey = new CoalescedSlotKey(typeId, coalesceKey);
        
        bool fallbackToNormal = false;
        PostTypePlan fallbackPlan = default;

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
            ref readonly var planRef = ref GetPlan(typeId);
            fallbackPlan = planRef;

            var result = HandleMergeFailureInternalLocked(
                slotKey: slotKey,
                slot: slot,
                value: in value,
                plan: in fallbackPlan,
                fallbackToNormal: out fallbackToNormal);

            if (!fallbackToNormal)
            {
                return result;
            }
        }
        else
        {
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
        
        if (fallbackToNormal)
        {
            return EnqueueNormalWithPlan(typeId, in value, in fallbackPlan);
        }

        return PostResult.Failure("Merge failed");
    }

    private PostResult HandleMergeFailureInternalLocked<T>(
        CoalescedSlotKey slotKey,
        CoalescedSlot slot,
        in T value,
        in PostTypePlan plan,
        out bool fallbackToNormal)
        where T : struct
    {
        fallbackToNormal = false;

        switch (plan.MergeFailure)
        {
            case MergeFailurePolicy.Reject:
                return PostResult.Failure("Merge failed.");

            case MergeFailurePolicy.FallbackToLatest:
                _payloadStorage.Release(slot.PayloadHandle);
                slot.PayloadHandle = _payloadStorage.Store(_runtimeId, value);
                slot.LastSequenceId = Interlocked.Increment(ref _sequenceCounter);
                slot.MergeCount = 1;
                slot.Active = true;
                _coalescedBuffer[slotKey] = slot;
                return PostResult.Coalesced();

            case MergeFailurePolicy.FallbackToNormal:
                fallbackToNormal = true;
                return PostResult.Enqueued();

            default:
                return PostResult.Failure($"Unsupported merge failure policy: {plan.MergeFailure}.");
        }
    }

    private PostResult EnqueueItemWithPolicy(in PostItem item)
    {
        var targetQueue = _isPumping ? _nextQueue : _readyQueue;
        if (targetQueue.TryEnqueue(in item))
            return PostResult.Enqueued();

        return HandleQueueFullInternal(in item, targetQueue);
    }

    private PostResult HandleQueueFullInternal(in PostItem item, RingBuffer<PostItem> targetQueue)
    {
        switch (item.Policy)
        {
            case BackpressurePolicy.RejectNew:
                _payloadStorage.Release(item.PayloadHandle);
                return PostResult.Failure("Queue full");
            case BackpressurePolicy.DropNewest:
                _payloadStorage.Release(item.PayloadHandle);
                return PostResult.Dropped();
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
            if (typeId < _pendingCount.Length) FastArray.At(_pendingCount, typeId)--;
        }
    }

    private int FlushBuffers()
    {
        int count = 0;
        
        // Dirty Signals Snapshot
        Array.Copy(_dirtyPendingBits, _dirtySnapshotBits, _dirtyPendingBits.Length);
        Array.Clear(_dirtyPendingBits, 0, _dirtyPendingBits.Length);

        // Coalesced Snapshot
        if (_pendingCoalesced.Count > 0)
        {
            // Sort by FirstSequenceId to maintain original order
            _pendingCoalesced.Sort((a, b) => _coalescedBuffer[a].FirstSequenceId.CompareTo(_coalescedBuffer[b].FirstSequenceId));
            foreach (var key in _pendingCoalesced)
            {
                _snapshotCoalesced.Add(_coalescedBuffer[key]);
                _coalescedBuffer.Remove(key);
            }
            _pendingCoalesced.Clear();
        }

        // Latest Snapshot
        Array.Copy(_latestPendingBits, _latestSnapshotBits, _latestPendingBits.Length);
        Array.Clear(_latestPendingBits, 0, _latestPendingBits.Length);
        
        // For latest buffer, we only need to copy handles for those with bits set
        // but for simplicity and safety against race conditions (though we are in lock), 
        // we copy what's needed.
        for (int i = 0; i < _latestSnapshotBits.Length; i++)
        {
            var bits = _latestSnapshotBits[i];
            while (bits != 0)
            {
                var bitIndex = BitHelper.TrailingZeroCount(bits);
                var typeId = (i << 6) + bitIndex;
                _latestSnapshotBuffer[typeId] = _latestBuffer[typeId];
                _latestBuffer[typeId] = PayloadHandle.Invalid;
                bits &= bits - 1;
            }
        }

        // 2. Dispatch outside lock
        
        // Dispatch Dirty Signals
        for (int i = 0; i < _dirtySnapshotBits.Length; i++)
        {
            var bits = _dirtySnapshotBits[i];
            while (bits != 0)
            {
                var bitIndex = BitHelper.TrailingZeroCount(bits);
                var typeId = (i << 6) + bitIndex;
                _payloadStorage.DispatchDefault(typeId, _eventCenter);
                count++;
                bits &= bits - 1;
            }
        }

        // Dispatch Coalesced
        if (_snapshotCoalesced.Count > 0)
        {
            foreach (var slot in _snapshotCoalesced)
            {
                try
                {
                    _payloadStorage.Dispatch(slot.PayloadHandle, _eventCenter);
                }
                finally
                {
                    _payloadStorage.Release(slot.PayloadHandle);
                }
                count++;
            }
            _snapshotCoalesced.Clear();
        }

        // Dispatch Latest
        for (int i = 0; i < _latestSnapshotBits.Length; i++)
        {
            var bits = _latestSnapshotBits[i];
            while (bits != 0)
            {
                var bitIndex = BitHelper.TrailingZeroCount(bits);
                var typeId = (i << 6) + bitIndex;
                var handle = _latestSnapshotBuffer[typeId];
                _latestSnapshotBuffer[typeId] = PayloadHandle.Invalid;
                try
                {
                    _payloadStorage.Dispatch(handle, _eventCenter);
                }
                finally
                {
                    _payloadStorage.Release(handle);
                }
                count++;
                bits &= bits - 1;
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

            while (true)
            {
                if (_readyQueue.IsEmpty) break;
                int currentWaveCount = _readyQueue.Count;

                wavesProcessed++;
                for (int i = 0; i < currentWaveCount; i++)
                {
                    PostItem item;
       
                    if (!_readyQueue.TryDequeue(out item)) break;

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

        int pendingQueueCount = _readyQueue.Count + _nextQueue.Count;
        return new PostPumpStats(processed, totalElapsedMs, pendingQueueCount, wavesProcessed);
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
        try
        {
            _payloadStorage.Dispatch(item.PayloadHandle, _eventCenter);
        }
        finally
        {
            _payloadStorage.Release(item.PayloadHandle);
            DecrementPendingCount(item.EventTypeId);
        }
    }

  
    public void PrewarmEvent<T>() where T : struct
    {
        if (_disposed) return;

        _payloadStorage.EnsureStore<T>(_runtimeId);

        var typeId = EventTypeId<T>.Id;
        if (typeId >= _postPlans.Length)
        {
            if (typeId >= _postPlans.Length)
            {
                var newSize = Math.Max(typeId + 1, _postPlans.Length * 2);
                var newPlans = new PostTypePlan[newSize];
                Array.Copy(_postPlans, newPlans, _postPlans.Length);
                for (int i = _postPlans.Length; i < newPlans.Length; i++)
                {
                    newPlans[i] = new PostTypePlan(i, PostDeliveryMode.Normal, _defaultBackpressure, 0, _defaultBackpressure, MergeFailurePolicy.Reject);
                }
                _postPlans = newPlans;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
  
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


        ReleaseQueuedPayloads(_readyQueue);
        ReleaseQueuedPayloads(_nextQueue);

        _payloadStorage.Dispose();
    }

    private void ReleaseQueuedPayloads(RingBuffer<PostItem> queue)
    {
        while (queue.TryDequeue(out var item))
        {
            _payloadStorage.Release(item.PayloadHandle);
            DecrementPendingCount(item.EventTypeId);
        }
    }
}
