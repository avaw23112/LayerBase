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

    private EventBuildPolicyTable _policyTable;

    // Sparse special post bitmaps
    private readonly SparsePendingBitSet _dirtyBits = new();
    private readonly SparsePendingBitSet _latestBits = new();
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

    private long _sequenceCounter;
    private int _sealedMaxEventTypeId = -1;
    private bool _disposed;
    private bool _isPumping;
    private readonly BackpressurePolicy _defaultBackpressure;

    internal int PendingCount => _readyQueue.Count + _nextQueue.Count + _pendingCoalesced.Count;

    internal bool HasSpecialPending =>
        _dirtyBits.HasPending ||
        _latestBits.HasPending ||
        _pendingCoalesced.Count != 0;

    internal bool HasPendingWork =>
        HasSpecialPending ||
        !_readyQueue.IsEmpty ||
        !_nextQueue.IsEmpty;

    public PostScheduler(int                   runtimeId, EventCenter eventCenter, PostSchedulerOptions options,
                         EventBuildPolicyTable policyTable)
    {
        _runtimeId = runtimeId;
        _eventCenter = eventCenter;
        _options = options;
        _policyTable = policyTable;
        _defaultBackpressure = options.DefaultBackpressure;
        _readyQueue = new RingBuffer<PostItem>(options.ReadyCapacity);
        _nextQueue = new RingBuffer<PostItem>(options.NextCapacity);
        _payloadStorage = new EventPayloadStorage(
            diagnosticsMode: options.PayloadDiagnostics);

        for (int i = 0; i < _latestBuffer.Length; i++) _latestBuffer[i] = PayloadHandle.Invalid;
    }

    public void BuildPlans(ReadOnlySpan<PostTypePlan> plans)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PostScheduler));
        }

        if (HasPendingWork)
        {
            throw new InvalidOperationException(
                "PostScheduler plans cannot be replaced while pending work exists.");
        }

        int maxEventTypeId = Math.Max(
            EventTypeIdAllocator.MaxId,
            FindMaxEventTypeId(plans));

        if (maxEventTypeId < 0)
        {
            RebuildPostBitmap();
            return;
        }

        EnsureEventCapacity(maxEventTypeId, rebuildBitmap: false);

        for (int i = 0; i <= maxEventTypeId && i < _postPlans.Length; i++)
        {
            _postPlans[i] = PostTypePlan.Default(i, _defaultBackpressure);
        }

        if (maxEventTypeId < _pendingCount.Length)
        {
            Array.Clear(_pendingCount, 0, maxEventTypeId + 1);
        }

        for (int i = 0; i < _latestBuffer.Length && i <= maxEventTypeId; i++)
        {
            _latestBuffer[i] = PayloadHandle.Invalid;
        }

        ClearPostBitmaps();

        var seenEventIds = new HashSet<int>();

        for (int i = 0; i < plans.Length; i++)
        {
            PostTypePlan plan = plans[i];

            if (plan.EventTypeId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plans),
                    $"EventTypeId cannot be negative: {plan.EventTypeId}.");
            }

            if (!seenEventIds.Add(plan.EventTypeId))
            {
                throw new InvalidOperationException(
                    $"Duplicate PostTypePlan for EventId={plan.EventTypeId}.");
            }

            EnsureEventCapacity(plan.EventTypeId, rebuildBitmap: false);
            _postPlans[plan.EventTypeId] = plan;
        }

        _sealedMaxEventTypeId = maxEventTypeId;
        RebuildPostBitmap();
    }

    private static int FindMaxEventTypeId(ReadOnlySpan<PostTypePlan> plans)
    {
        int maxEventTypeId = -1;

        for (int i = 0; i < plans.Length; i++)
        {
            maxEventTypeId = Math.Max(maxEventTypeId, plans[i].EventTypeId);
        }

        return maxEventTypeId;
    }

    private void ClearPostBitmaps()
    {
        _dirtyBits.ClearPending();
        _dirtyBits.ClearSnapshot();
        _latestBits.ClearPending();
        _latestBits.ClearSnapshot();
        _pendingCoalesced.Clear();
        _coalescedBuffer.Clear();
        _snapshotCoalesced.Clear();
    }

    private void EnsureEventCapacity(int typeId, bool rebuildBitmap = true)
    {
        if (typeId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(typeId));
        }

        var requiredLength = typeId + 1;

        if (_postPlans.Length < requiredLength)
        {
            var oldLength = _postPlans.Length;
            var newLength = BitHelper.NextPowerOfTwo(requiredLength);
            var newPlans = new PostTypePlan[newLength];
            Array.Copy(_postPlans, newPlans, oldLength);

            for (var i = oldLength; i < newPlans.Length; i++)
            {
                newPlans[i] = PostTypePlan.Default(i, _defaultBackpressure);
            }

            _postPlans = newPlans;
        }

        if (_pendingCount.Length < requiredLength)
        {
            var newPending = new int[BitHelper.NextPowerOfTwo(requiredLength)];
            Array.Copy(_pendingCount, newPending, _pendingCount.Length);
            _pendingCount = newPending;
        }

        if (_latestBuffer.Length < requiredLength)
        {
            var oldLength = _latestBuffer.Length;
            var newLength = BitHelper.NextPowerOfTwo(requiredLength);

            var newLatest = new PayloadHandle[newLength];
            Array.Copy(_latestBuffer, newLatest, oldLength);
            for (var i = oldLength; i < newLength; i++)
            {
                newLatest[i] = PayloadHandle.Invalid;
            }

            _latestBuffer = newLatest;

            var newLatestSnapshot = new PayloadHandle[newLength];
            Array.Copy(_latestSnapshotBuffer, newLatestSnapshot, _latestSnapshotBuffer.Length);
            for (var i = _latestSnapshotBuffer.Length; i < newLength; i++)
            {
                newLatestSnapshot[i] = PayloadHandle.Invalid;
            }

            _latestSnapshotBuffer = newLatestSnapshot;
        }

        _dirtyBits.EnsureBitCapacity(requiredLength);
        _latestBits.EnsureBitCapacity(requiredLength);

        if (typeId > _sealedMaxEventTypeId)
        {
            _sealedMaxEventTypeId = typeId;
        }

        if (rebuildBitmap)
        {
            RebuildPostBitmap();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long NextSequenceId()
    {
        unchecked
        {
            return ++_sequenceCounter;
        }
    }

    private void RebuildPostBitmap()
    {
        _postBitmap.Build(_postPlans);
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
        if (!IsKnownEventType(typeId))
        {
            EnsureEventCapacity(typeId, rebuildBitmap: false);
            if (typeId > _sealedMaxEventTypeId)
                _sealedMaxEventTypeId = typeId;
        }

        if (policyOverride.HasValue)
            return TryPostOverride(in value, policyOverride.Value);

        if (!_postBitmap.IsSpecial(typeId))
            return EnqueueNormalFast(typeId, in value);

        return TryPostSpecial(typeId, in value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult TryPostOverride<T>(in T value, EventPostPolicy policy) where T : struct
    {
        EventPostPolicyRules.Validate(in policy, nameof(policy));

        var typeId = EventTypeId<T>.Id;
        EnsureEventCapacity(typeId, rebuildBitmap: false);

        PostTypePlan plan = PostTypePlan.FromPolicy(typeId, in policy, _defaultBackpressure);

        switch (policy.Mode)
        {
            case PostDeliveryMode.Normal:
                return EnqueueNormalWithPlan(typeId, in value, in plan);
            case PostDeliveryMode.DirtySignal:
                if (!IsKnownEventType(typeId)) return FailEventTypeNotRegistered<T>();
                return MarkDirtyById<T>(typeId);
            case PostDeliveryMode.Coalesced:
                if (!IsKnownEventType(typeId)) return FailEventTypeNotRegistered<T>();
                return EnqueueCoalescedWithPlan(typeId, in value, in plan);
            case PostDeliveryMode.Latest:
                if (!IsKnownEventType(typeId)) return FailEventTypeNotRegistered<T>();
                return EnqueueLatestInternal(typeId, in value);
            default:
                return PostResult.Failure();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult FailSchedulerDisposed()
    {
        return PostResult.Failure();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult EnqueueNormalFast<T>(int typeId, in T value) where T : struct
    {
        var store = _payloadStorage.GetStoreFast<T>();
        var handle = store.Add(in value);
        var sequenceId = NextSequenceId();
        var item = new PostItem(typeId, handle, sequenceId, _defaultBackpressure);

        var targetQueue = _isPumping ? _nextQueue : _readyQueue;
        if (targetQueue.TryEnqueue(in item))
            return PostResult.Enqueued();

        return HandleQueueFullSlow(in item, store, targetQueue);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PostResult HandleQueueFullSlow<T>(in PostItem item, EventStore<T> store, RingBuffer<PostItem> targetQueue)
        where T : struct
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
                return PostResult.Failure();
            }

            FastArray.At(_pendingCount, typeId)++;
        }

        var store = _payloadStorage.GetStoreFast<T>();
        var handle = store.Add(in value);
        var sequenceId = NextSequenceId();
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

        if (_dirtyBits.Set(typeId))
            _payloadStorage.EnsureStore<T>(_runtimeId);

        return PostResult.Coalesced();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsKnownEventType(int typeId)
    {
        return typeId <= _sealedMaxEventTypeId;
    }

    public void AddSpecialPolicy(int typeId, EventPostPolicy policy)
    {
        EnsureEventCapacity(typeId, rebuildBitmap: false);

        _postPlans[typeId] = new PostTypePlan(typeId, policy.Mode, policy.Backpressure, policy.MaxPending,
            _defaultBackpressure, policy.MergeFailure);
        RebuildPostBitmap();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PostResult FailEventTypeNotRegistered<T>() where T : struct
    {
        return PostResult.Failure();
    }

    private PostResult EnqueueLatestInternal<T>(int typeId, in T value) where T : struct
    {
        if (typeId < _latestBuffer.Length)
        {
            ref var handleRef = ref FastArray.At(_latestBuffer, typeId);
            if (!handleRef.IsInvalid)
            {
                _payloadStorage.Release(handleRef);
            }

            handleRef = _payloadStorage.Store(_runtimeId, value);
            _latestBits.Set(typeId);

            return PostResult.Success;
        }

        return PostResult.Failure();
    }

    private PostResult EnqueueCoalescedInternal<T>(int typeId, in T value) where T : struct
    {
        ref readonly var planRef = ref GetPlan(typeId);
        return EnqueueCoalescedWithPlan(typeId, in value, in planRef);
    }

    private PostResult EnqueueCoalescedWithPlan<T>(int typeId, in T value, in PostTypePlan plan) where T : struct
    {
        var meta = _policyTable.GetMetaData<T>(typeId);
        int coalesceKey = meta?.GetPostCoalesceKey(value) ?? 0;
        var slotKey = new CoalescedSlotKey(typeId, coalesceKey);

        bool fallbackToNormal = false;
        PostTypePlan fallbackPlan = plan;

        if (_coalescedBuffer.TryGetValue(slotKey, out var slot))
        {
            ref T current = ref _payloadStorage.GetRef<T>(_runtimeId, slot.PayloadHandle);
            if (meta != null && meta.TryMergePostEvent(ref current, in value))
            {
                slot.LastSequenceId = NextSequenceId();
                slot.MergeCount++;
                _coalescedBuffer[slotKey] = slot;
                return PostResult.Coalesced();
            }

            // Merge failed - use the provided plan (override or compiled)
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
            var seq = NextSequenceId();
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

        return PostResult.Failure();
    }

    private PostResult HandleMergeFailureInternalLocked<T>(
        CoalescedSlotKey slotKey,
        CoalescedSlot    slot,
        in  T            value,
        in  PostTypePlan plan,
        out bool         fallbackToNormal)
        where T : struct
    {
        fallbackToNormal = false;

        switch (plan.MergeFailure)
        {
            case MergeFailurePolicy.Reject:
                return PostResult.Failure();

            case MergeFailurePolicy.FallbackToLatest:
                _payloadStorage.Release(slot.PayloadHandle);
                slot.PayloadHandle = _payloadStorage.Store(_runtimeId, value);
                slot.LastSequenceId = NextSequenceId();
                slot.MergeCount = 1;
                slot.Active = true;
                _coalescedBuffer[slotKey] = slot;
                return PostResult.Coalesced();

            case MergeFailurePolicy.FallbackToNormal:
                fallbackToNormal = true;
                return PostResult.Enqueued();

            default:
                return PostResult.Failure();
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
                return PostResult.Failure();
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
                return PostResult.Failure();
            default:
                _payloadStorage.Release(item.PayloadHandle);
                return PostResult.Failure();
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

    public void UpdatePolicyTable(EventBuildPolicyTable policyTable)
    {
        _policyTable = policyTable ?? throw new ArgumentNullException(nameof(policyTable));
    }

    private int FlushBuffers()
    {
        if (!HasSpecialPending)
            return 0;

        int count = 0;

        // 1. Take Snapshots

        // Dirty Signals Snapshot
        _dirtyBits.TakeSnapshot();

        // Coalesced Snapshot
        if (_pendingCoalesced.Count > 0)
        {
            _pendingCoalesced.Sort((a, b) =>
                _coalescedBuffer[a].FirstSequenceId.CompareTo(_coalescedBuffer[b].FirstSequenceId));
            foreach (var key in _pendingCoalesced)
            {
                _snapshotCoalesced.Add(_coalescedBuffer[key]);
                _coalescedBuffer.Remove(key);
            }

            _pendingCoalesced.Clear();
        }

        // Latest Snapshot
        _latestBits.TakeSnapshot();

        ReadOnlySpan<int> latestWords = _latestBits.SnapshotWords;

        for (int i = 0; i < latestWords.Length; i++)
        {
            int wordIndex = latestWords[i];
            ulong bits = _latestBits.GetSnapshotBits(wordIndex);

            while (bits != 0)
            {
                int bitIndex = BitHelper.TrailingZeroCount(bits);
                int typeId = (wordIndex << 6) + bitIndex;

                _latestSnapshotBuffer[typeId] = _latestBuffer[typeId];
                _latestBuffer[typeId] = PayloadHandle.Invalid;

                bits &= bits - 1;
            }
        }

        // 2. Dispatch snapshots
        try
        {
            count += DispatchDirtySnapshotSafely();
            count += DispatchCoalescedSnapshotSafely();
            count += DispatchLatestSnapshotSafely();
            return count;
        }
        finally
        {
            _dirtyBits.ClearSnapshot();
            ReleaseRemainingCoalescedSnapshot();
            _snapshotCoalesced.Clear();
            ReleaseRemainingLatestSnapshot();
            _latestBits.ClearSnapshot();
        }
    }

    private int DispatchDirtySnapshotSafely()
    {
        var processed = 0;

        ReadOnlySpan<int> words = _dirtyBits.SnapshotWords;

        for (int i = 0; i < words.Length; i++)
        {
            int wordIndex = words[i];
            ulong bits = _dirtyBits.GetSnapshotBits(wordIndex);

            while (bits != 0)
            {
                int bitIndex = BitHelper.TrailingZeroCount(bits);
                int typeId = (wordIndex << 6) + bitIndex;

                _payloadStorage.DispatchDefault(typeId, _eventCenter);

                processed++;
                bits &= bits - 1;
            }

            _dirtyBits.ClearSnapshotWord(wordIndex);
        }

        return processed;
    }

    private int DispatchCoalescedSnapshotSafely()
    {
        var processed = 0;
        try
        {
            for (var i = 0; i < _snapshotCoalesced.Count; i++)
            {
                var slot = _snapshotCoalesced[i];
                if (slot.PayloadHandle.IsInvalid) continue;

                try
                {
                    _payloadStorage.Dispatch(slot.PayloadHandle, _eventCenter);
                    processed++;
                }
                finally
                {
                    _payloadStorage.Release(slot.PayloadHandle);

                    // Mark as invalid to prevent double release in outer finally
                    slot.PayloadHandle = PayloadHandle.Invalid;
                    _snapshotCoalesced[i] = slot;
                }
            }
        }
        finally
        {
            ReleaseRemainingCoalescedSnapshot();
            _snapshotCoalesced.Clear();
        }

        return processed;
    }

    private void ReleaseRemainingCoalescedSnapshot()
    {
        foreach (var slot in _snapshotCoalesced)
        {
            if (!slot.PayloadHandle.IsInvalid)
            {
                _payloadStorage.Release(slot.PayloadHandle);
            }
        }
    }

    private int DispatchLatestSnapshotSafely()
    {
        var processed = 0;

        ReadOnlySpan<int> words = _latestBits.SnapshotWords;

        for (int i = 0; i < words.Length; i++)
        {
            int wordIndex = words[i];
            ulong bits = _latestBits.GetSnapshotBits(wordIndex);

            while (bits != 0)
            {
                int bitIndex = BitHelper.TrailingZeroCount(bits);
                int typeId = (wordIndex << 6) + bitIndex;
                bits &= bits - 1;

                var handle = _latestSnapshotBuffer[typeId];
                if (handle.IsInvalid) continue;

                try
                {
                    _payloadStorage.Dispatch(handle, _eventCenter);
                    processed++;
                }
                finally
                {
                    _payloadStorage.Release(handle);
                    _latestSnapshotBuffer[typeId] = PayloadHandle.Invalid;
                }
            }

            _latestBits.ClearSnapshotWord(wordIndex);
        }

        return processed;
    }

    private void ReleaseRemainingLatestSnapshot()
    {
        ReadOnlySpan<int> words = _latestBits.SnapshotWords;

        for (int i = 0; i < words.Length; i++)
        {
            int wordIndex = words[i];
            ulong bits = _latestBits.GetSnapshotBits(wordIndex);

            while (bits != 0)
            {
                int bitIndex = BitHelper.TrailingZeroCount(bits);
                int typeId = (wordIndex << 6) + bitIndex;
                var handle = _latestSnapshotBuffer[typeId];
                if (!handle.IsInvalid)
                {
                    _payloadStorage.Release(handle);
                    _latestSnapshotBuffer[typeId] = PayloadHandle.Invalid;
                }

                bits &= bits - 1;
            }
        }
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
        if (startTimestamp != 0)
            totalElapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;

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

        var typeId = EventTypeId<T>.Id;
        EnsureEventCapacity(typeId, rebuildBitmap: false);

        if (!_postPlans[typeId].IsRegistered)
        {
            _postPlans[typeId] = PostTypePlan.Default(typeId, _defaultBackpressure);
        }

        _payloadStorage.EnsureStore<T>(_runtimeId);
        RebuildPostBitmap();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // 先释放 snapshot 残留，不能先 Clear。
        ReleaseRemainingCoalescedSnapshot();
        _snapshotCoalesced.Clear();
        ReleaseRemainingLatestSnapshot();

        ReadOnlySpan<int> latestPendingWords = _latestBits.PendingWords;

        for (int i = 0; i < latestPendingWords.Length; i++)
        {
            int wordIndex = latestPendingWords[i];
            ulong bits = _latestBits.GetPendingBits(wordIndex);

            while (bits != 0)
            {
                int bitIndex = BitHelper.TrailingZeroCount(bits);
                int typeId = (wordIndex << 6) + bitIndex;
                _payloadStorage.Release(_latestBuffer[typeId]);
                bits &= bits - 1;
            }
        }

        _latestBits.ClearPending();
        _latestBits.ClearSnapshot();

        _dirtyBits.ClearPending();
        _dirtyBits.ClearSnapshot();

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
