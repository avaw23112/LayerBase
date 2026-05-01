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
    
    private BitArray _coalescedBuffer = new(256);
    private readonly List<int> _pendingCoalesced = new();
    
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
            case PostDeliveryMode.Coalesced:
                return EnqueueCoalesced<T>(typeId);
            case PostDeliveryMode.Latest:
                return EnqueueLatest(typeId, value);
            default:
                return PostResult.Failure("Unknown delivery mode");
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

    private PostResult EnqueueCoalesced<T>(int typeId) where T : struct
    {
        lock (_bufferLock)
        {
            if (typeId >= _coalescedBuffer.Length) ExpandBuffers(typeId);
            if (!_coalescedBuffer[typeId])
            {
                _coalescedBuffer.Set(typeId, true);
                _pendingCoalesced.Add(typeId);
                _payloadStorage.EnsureStore<T>();
            }
            return PostResult.Success;
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
            if (_pendingCoalesced.Count > 0)
            {
                foreach (var typeId in _pendingCoalesced)
                {
                    _payloadStorage.DispatchDefault(typeId, _eventCenter);
                    _coalescedBuffer.Set(typeId, false);
                    count++;
                }
                _pendingCoalesced.Clear();
            }
            
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

    private void ExpandBuffers(int minCapacity)
    {
        int newSize = Math.Max(minCapacity + 1, _coalescedBuffer.Length * 2);
        _coalescedBuffer.Length = newSize;
        
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
        }
        
        _payloadStorage.Dispose();
        _readyQueue.Clear();
        _nextQueue.Clear();
    }
}
