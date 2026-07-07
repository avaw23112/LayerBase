using System.Buffers;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Core.Event;

/// <summary>
/// 全局事件中心，负责事件的订阅管理及同步派发。
/// </summary>
public sealed class EventCenter
{
    private readonly ConcurrentDictionary<int, Action> _bucketCacheResetters = new();
    private readonly ConcurrentDictionary<int, IEventBucketNonGeneric> _eventBuckets = new();
    private readonly object _lock = new();
    private int _isResetting;

    internal PostScheduler? PostScheduler { get; set; }

    internal void SubscribeFlow<T>(int layerIndex, IEventHandler<T> handler) where T : struct
    {
        GetBucket<T>().Add(layerIndex, handler);
    }

    internal void SubscribeAsync<T>(int layerIndex, IEventHandlerAsync<T> handler) where T : struct
    {
        GetBucket<T>().Add(layerIndex, handler);
    }

    internal void SubscribeFlow<T>(int layerIndex, EventHandleDelegate<T> handleDelegate) where T : struct
    {
        GetBucket<T>().Add(layerIndex, handleDelegate);
    }

    internal void SubscribeAsync<T>(int layerIndex, EventHandleDelegateAsync<T> handleDelegate) where T : struct
    {
        GetBucket<T>().Add(layerIndex, handleDelegate);
    }

    internal void SubscribeParallel<T>(int layerIndex, EventNotifyDelegate<T> handleDelegate,
                                       Action<int, int, int, Exception> reportError) where T : struct
    {
        GetBucket<T>().AddParallel(layerIndex, handleDelegate, reportError);
    }

    internal void SubscribeNotify<T>(int layerIndex, EventNotifyDelegate<T> handler) where T : struct
    {
        GetBucket<T>().AddNotify(layerIndex, handler);
    }

    internal void Subscribe<T>(int layerIndex, EventNotifyDelegate<T> handler) where T : struct
    {
        GetBucket<T>().AddSubscribe(layerIndex, handler);
    }

    internal void UnsubscribeFlow<T>(int layerIndex, IEventHandler<T> handler) where T : struct
    {
        GetBucket<T>().Remove(layerIndex, handler);
    }

    internal void UnsubscribeAsync<T>(int layerIndex, IEventHandlerAsync<T> handler) where T : struct
    {
        GetBucket<T>().Remove(layerIndex, handler);
    }

    internal void UnsubscribeNotify<T>(int layerIndex, EventNotifyDelegate<T> handler) where T : struct
    {
        GetBucket<T>().RemoveNotify(layerIndex, handler);
    }

    internal void UnsubscribeParallel<T>(int layerIndex, EventNotifyDelegate<T> handler) where T : struct
    {
        GetBucket<T>().RemoveParallel(layerIndex, handler);
    }

    internal void Unsubscribe<T>(int layerIndex, EventNotifyDelegate<T> handler) where T : struct
    {
        GetBucket<T>().RemoveSubscribe(layerIndex, handler);
    }

    internal void UnsubscribeFlow<T>(int layerIndex, EventHandleDelegate<T> handleDelegate) where T : struct
    {
        GetBucket<T>().Remove(layerIndex, handleDelegate);
    }

    internal void UnsubscribeAsync<T>(int layerIndex, EventHandleDelegateAsync<T> handleDelegate) where T : struct
    {
        GetBucket<T>().Remove(layerIndex, handleDelegate);
    }

    #region Non-generic subscription path (IL2CPP-safe)

    internal void SubscribeFlow(int layerIndex, object handler, Type eventType)
    {
        GetBucket(eventType).AddFlow(layerIndex, handler);
    }

    internal void SubscribeAsync(int layerIndex, object handler, Type eventType)
    {
        GetBucket(eventType).AddAsync(layerIndex, handler);
    }

    internal void SubscribeNotify(int layerIndex, object handler, Type eventType)
    {
        GetBucket(eventType).AddNotify(layerIndex, handler);
    }

    internal void Subscribe(int layerIndex, object handler, Type eventType)
    {
        GetBucket(eventType).AddSubscribe(layerIndex, handler);
    }

    internal void SubscribeParallel(int layerIndex, object handler, Type eventType,
                                     Action<int, int, int, Exception> reportError)
    {
        GetBucket(eventType).AddParallel(layerIndex, handler, reportError);
    }

    internal void UnsubscribeFlow(int layerIndex, object handler, Type eventType)
    {
        GetBucket(eventType).RemoveFlow(layerIndex, handler);
    }

    internal void UnsubscribeAsync(int layerIndex, object handler, Type eventType)
    {
        GetBucket(eventType).RemoveAsync(layerIndex, handler);
    }

    internal void UnsubscribeNotify(int layerIndex, object handler, Type eventType)
    {
        GetBucket(eventType).RemoveNotify(layerIndex, handler);
    }

    internal void Unsubscribe(int layerIndex, object handler, Type eventType)
    {
        GetBucket(eventType).RemoveSubscribe(layerIndex, handler);
    }

    internal void UnsubscribeParallel(int layerIndex, object handler, Type eventType)
    {
        GetBucket(eventType).RemoveParallel(layerIndex, handler);
    }

    #endregion

    /// <summary>
    /// 派发同步事件。
    /// </summary>
    /// <param name="value">事件数据。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState Send<T>(in T value) where T : struct
    {
        if (Volatile.Read(ref _isResetting) == 1) return EventHandledState.Continue;
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) return cached.Dispatch(in value);
        return GetBucket<T>().Dispatch(in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref TElement GetArrayDataRef<TElement>(TElement[] array)
    {
#if NET5_0_OR_GREATER
        return ref MemoryMarshal.GetArrayDataReference(array);
#else
        return ref MemoryMarshal.GetReference(array.AsSpan());
#endif
    }

    internal void Reset()
    {
        if (Interlocked.Exchange(ref _isResetting, 1) == 1) return;

        lock (_lock)
        {
            foreach (var bucket in _eventBuckets.Values)
                if (bucket is IDisposable b)
                    b.Dispose();
            _eventBuckets.Clear();
            foreach (var resetter in _bucketCacheResetters.Values) resetter();
            _bucketCacheResetters.Clear();
        }

        Volatile.Write(ref _isResetting, 0);
    }

    /// <summary>
    /// 预热单个事件类型。
    /// </summary>
    /// <typeparam name="TEvent">要预热的事件类型。</typeparam>
    /// <param name="options">预热参数。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrewarmEvent<TEvent>(in LayerPrewarmOptions options)
        where TEvent : struct
    {
        // 1. 预热 EventTypeId
        if ((options.Targets & LayerPrewarmTargets.EventTypeId) != 0)
        {
            _ = EventTypeId<TEvent>.Id;
        }

        EventBucket<TEvent>? bucket = null;

        // 2. 预热 Bucket
        if ((options.Targets & LayerPrewarmTargets.Bucket) != 0 ||
            (options.Targets & LayerPrewarmTargets.DispatchTable) != 0)
        {
            bucket = GetBucket<TEvent>();
        }

        // 3. 预热派发表
        if ((options.Targets & LayerPrewarmTargets.DispatchTable) != 0)
        {
            bucket ??= GetBucket<TEvent>();
            bucket.PrewarmDispatchTable();
        }

        // 4. 预热 Post 队列
        if ((options.Targets & LayerPrewarmTargets.PostQueue) != 0)
        {
            PostScheduler?.PrewarmEvent<TEvent>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventBucket<T> GetBucket<T>() where T : struct
    {
        var cached = BucketCache<T>.Instance;
        if (cached != null && cached.Owner == this) return cached;
        var typeId = EventTypeId<T>.Id;
        _bucketCacheResetters.TryAdd(typeId, static () => BucketCache<T>.Instance = null);
        var bucket = (EventBucket<T>)_eventBuckets.GetOrAdd(typeId, _ => new EventBucket<T>(this));
        BucketCache<T>.Instance = bucket;
        return bucket;
    }

    private IEventBucketNonGeneric GetBucket(Type eventType)
    {
        var typeId = GetEventTypeId(eventType);
        return _eventBuckets.GetOrAdd(typeId, _ =>
        {
            var bucketType = typeof(EventBucket<>).MakeGenericType(eventType);
            return (IEventBucketNonGeneric)Activator.CreateInstance(bucketType, this);
        });
    }

    private static int GetEventTypeId(Type eventType)
    {
        var idType = typeof(EventTypeId<>).MakeGenericType(eventType);
        var idField = idType.GetField("Id", BindingFlags.Public | BindingFlags.Static);
        return (int)idField!.GetValue(null)!;
    }

    private static class BucketCache<T> where T : struct
    {
        public static EventBucket<T>? Instance;
    }

    private interface IResetable : IDisposable
    {
        void Reset();
    }

    private sealed class EventBucket<T> : IResetable, IEventBucketNonGeneric where T : struct
    {
        private readonly object _lock = new();
        public readonly EventCenter? Owner;
        private bool _disposed;
        private int _isDirty;

        private HandlerBucket<T>?[] _buckets = Array.Empty<HandlerBucket<T>>();

        private EventHandleDelegate<T>[] _syncHandlers = Array.Empty<EventHandleDelegate<T>>();
        private EventHandleDelegateAsync<T>[] _asyncHandlers = Array.Empty<EventHandleDelegateAsync<T>>();
        private EventNotifyDelegate<T>[] _notifyHandlers = Array.Empty<EventNotifyDelegate<T>>();
        private EventNotifyDelegate<T>[] _subscribeHandlers = Array.Empty<EventNotifyDelegate<T>>();

        private FaultTable<T> _faultTable =
            new(Array.Empty<FaultSlot>(), Array.Empty<FaultSlot>(), Array.Empty<FaultSlot>());

        private ParallelHandlerEntry<T>[] _flatParallel = Array.Empty<ParallelHandlerEntry<T>>();

        private int _syncCountTotal;
        private int _asyncCountTotal;
        private int _parallelCountTotal;
        private int _notifyCountTotal;
        private int _notifySafeCountTotal;

        private bool _isSingleSync;
        private bool _isSingleNotify;
        private bool _isSingleNotifySafe;
        private bool _isSmallNotifyFanoutOnly;

        private EventHandleDelegate<T>? _singleSyncHandler;
        private EventNotifyDelegate<T>? _singleNotifyHandler;
        private EventNotifyDelegate<T>? _singleSubscribeHandler;

        private ulong _subscriberMask;
        private ulong _syncMask;
        private ulong _asyncMask;
        private ulong _parallelMask;
        private ulong _notifyMask;
        private ulong _notifySafeMask;

        public EventBucket(EventCenter center)
        {
            Owner = center;
        }

        /// <summary>
        /// 预热当前事件类型的派发表。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrewarmDispatchTable()
        {
            EnsureClean();
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                ReturnArrays();
            }
        }

        public void Reset()
        {
            HandlerBucket<T>?[] snapshot;
            lock (_lock)
            {
                snapshot = new HandlerBucket<T>?[_buckets.Length];
                Array.Copy(_buckets, snapshot, _buckets.Length);
            }

            foreach (var b in snapshot) b?.Reset();
            lock (_lock)
            {
                Rebuild();
            }
        }

        private void ReturnArrays()
        {
            _singleSyncHandler = null;
            _singleNotifyHandler = null;
            _singleSubscribeHandler = null;
            _isSingleSync = false;
            _isSingleNotify = false;
            _isSingleNotifySafe = false;
            _isSmallNotifyFanoutOnly = false;

            _syncCountTotal = 0;
            _asyncCountTotal = 0;
            _parallelCountTotal = 0;
            _notifyCountTotal = 0;
            _notifySafeCountTotal = 0;

            _subscriberMask = 0;
            _syncMask = 0;
            _asyncMask = 0;
            _parallelMask = 0;
            _notifyMask = 0;
            _notifySafeMask = 0;

            ReturnArrayHelper(ref _syncHandlers);
            ReturnArrayHelper(ref _asyncHandlers);
            ReturnArrayHelper(ref _notifyHandlers);
            ReturnArrayHelper(ref _subscribeHandlers);

            ReturnFaultArrays(_faultTable);
            _faultTable = new FaultTable<T>(Array.Empty<FaultSlot>(), Array.Empty<FaultSlot>(),
                Array.Empty<FaultSlot>());

            if (_flatParallel != null && _flatParallel.Length > 0 &&
                _flatParallel != Array.Empty<ParallelHandlerEntry<T>>())
            {
                ArrayPool<ParallelHandlerEntry<T>>.Shared.Return(_flatParallel, true);
                _flatParallel = Array.Empty<ParallelHandlerEntry<T>>();
            }
        }

        private void ReturnArrayHelper<TDelegate>(ref TDelegate[] handlers)
        {
            if (handlers != null && handlers.Length > 0 && handlers != Array.Empty<TDelegate>())
            {
                ArrayPool<TDelegate>.Shared.Return(handlers, true);
                handlers = Array.Empty<TDelegate>();
            }
        }

        private void ReturnFaultArrays(FaultTable<T> table)
        {
            if (table.SyncFaults != null && table.SyncFaults.Length > 0 &&
                table.SyncFaults != Array.Empty<FaultSlot>())
                ArrayPool<FaultSlot>.Shared.Return(table.SyncFaults, true);
            if (table.AsyncFaults != null && table.AsyncFaults.Length > 0 &&
                table.AsyncFaults != Array.Empty<FaultSlot>())
                ArrayPool<FaultSlot>.Shared.Return(table.AsyncFaults, true);
            if (table.SubscribeFaults != null && table.SubscribeFaults.Length > 0 &&
                table.SubscribeFaults != Array.Empty<FaultSlot>())
                ArrayPool<FaultSlot>.Shared.Return(table.SubscribeFaults, true);
        }

        public void MarkDirty()
        {
            Volatile.Write(ref _isDirty, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureClean()
        {
            if (Volatile.Read(ref _isDirty) == 1)
                lock (_lock)
                {
                    if (_isDirty == 1)
                    {
                        Rebuild();
                        Volatile.Write(ref _isDirty, 0);
                    }
                }
        }

        private void Rebuild()
        {
            if (_disposed) return;

            int totalSync = 0, totalAsync = 0, totalParallel = 0, totalNotify = 0, totalSubscribe = 0;
            ulong newMask = 0,
                newSyncMask = 0,
                newAsyncMask = 0,
                newParallelMask = 0,
                newNotifyMask = 0,
                newSubscribeMask = 0;
            for (var i = 0; i < _buckets.Length; i++)
            {
                var b = _buckets[i];
                if (b == null || !b.HasHandlers) continue;
                int bSync = 0, bAsync = 0;
                foreach (var h in b.MasterOrdered)
                {
                    if (h.Circuit.IsDisabled) continue;
                    if (h.SyncHandler != null) bSync++;
                    else if (h.AsyncHandler != null) bAsync++;
                }

                foreach (var h in b.MasterUnordered)
                {
                    if (h.Circuit.IsDisabled) continue;
                    if (h.SyncWrapper != null) bSync++;
                    else if (h.AsyncWrapper != null) bAsync++;
                }

                var bParallel = b.MasterParallel.Count;
                var bNotify = 0;
                foreach (var h in b.MasterNotify)
                    if (!h.Circuit.IsDisabled)
                        bNotify++;
                var bSubscribe = 0;
                foreach (var h in b.MasterSubscribe)
                    if (!h.Circuit.IsDisabled)
                        bSubscribe++;

                totalSync += bSync;
                totalAsync += bAsync;
                totalParallel += bParallel;
                totalNotify += bNotify;
                totalSubscribe += bSubscribe;
                var bit = 1UL << i;
                if (bSync > 0) newSyncMask |= bit;
                if (bAsync > 0) newAsyncMask |= bit;
                if (bParallel > 0) newParallelMask |= bit;
                if (bNotify > 0) newNotifyMask |= bit;
                if (bSubscribe > 0) newSubscribeMask |= bit;
                if (bSync > 0 || bAsync > 0 || bParallel > 0 || bNotify > 0 || bSubscribe > 0) newMask |= bit;
            }

            RentArrays(totalSync, totalAsync, totalNotify, totalSubscribe, totalParallel);

            int sIdx = 0, aIdx = 0, pIdx = 0, nIdx = 0, nsIdx = 0;
            var syncFaults = _faultTable.SyncFaults;
            var asyncFaults = _faultTable.AsyncFaults;
            var subscribeFaults = _faultTable.SubscribeFaults;

            for (var i = 0; i < _buckets.Length; i++)
            {
                var b = _buckets[i];
                if (b == null) continue;

                if (b.HasHandlers)
                {
                    foreach (var h in b.MasterOrdered)
                    {
                        if (h.Circuit.IsDisabled) continue;
                        if (h.SyncHandler != null)
                        {
                            _syncHandlers[sIdx] = h.SyncHandler;
                            syncFaults[sIdx] = new FaultSlot(i, h.Circuit, h.HandlerNameId);
                            sIdx++;
                        }

                        if (h.AsyncHandler != null)
                        {
                            _asyncHandlers[aIdx] = h.AsyncHandler;
                            asyncFaults[aIdx] = new FaultSlot(i, h.Circuit, h.HandlerNameId);
                            aIdx++;
                        }
                    }

                    foreach (var h in b.MasterUnordered)
                    {
                        if (h.Circuit.IsDisabled) continue;
                        if (h.SyncWrapper != null)
                        {
                            _syncHandlers[sIdx] = h.SyncWrapper;
                            syncFaults[sIdx] = new FaultSlot(i, h.Circuit, h.HandlerNameId);
                            sIdx++;
                        }

                        if (h.AsyncWrapper != null)
                        {
                            _asyncHandlers[aIdx] = h.AsyncWrapper;
                            asyncFaults[aIdx] = new FaultSlot(i, h.Circuit, h.HandlerNameId);
                            aIdx++;
                        }
                    }

                    foreach (var h in b.MasterNotify)
                        if (!h.Circuit.IsDisabled)
                        {
                            _notifyHandlers[nIdx] = h.Handler;
                            nIdx++;
                        }

                    foreach (var h in b.MasterSubscribe)
                        if (!h.Circuit.IsDisabled)
                        {
                            _subscribeHandlers[nsIdx] = h.Handler;
                            subscribeFaults[nsIdx] = new FaultSlot(i, h.Circuit, h.HandlerNameId);
                            nsIdx++;
                        }

                    foreach (var h in b.MasterParallel) _flatParallel[pIdx++] = h;
                }
            }

            ClearArrays(sIdx, aIdx, nIdx, nsIdx, pIdx);
            _syncCountTotal = sIdx;
            _asyncCountTotal = aIdx;
            _parallelCountTotal = pIdx;
            _notifyCountTotal = nIdx;
            _notifySafeCountTotal = nsIdx;

            _subscriberMask = newMask;
            _syncMask = newSyncMask;
            _asyncMask = newAsyncMask;
            _parallelMask = newParallelMask;
            _notifyMask = newNotifyMask;
            _notifySafeMask = newSubscribeMask;

            IdentifySpecializations();
        }

        private void RentArrays(int totalSync, int totalAsync, int totalNotify, int totalSubscribe, int totalParallel)
        {
            if (_syncHandlers.Length < totalSync)
            {
                ReturnArraysForRebuild(true, false, false, false, false);
                _syncHandlers = ArrayPool<EventHandleDelegate<T>>.Shared.Rent(totalSync);
                var syncFaults = ArrayPool<FaultSlot>.Shared.Rent(totalSync);
                _faultTable = new FaultTable<T>(syncFaults, _faultTable.AsyncFaults, _faultTable.SubscribeFaults);
            }

            if (_asyncHandlers.Length < totalAsync)
            {
                ReturnArraysForRebuild(false, true, false, false, false);
                _asyncHandlers = ArrayPool<EventHandleDelegateAsync<T>>.Shared.Rent(totalAsync);
                var asyncFaults = ArrayPool<FaultSlot>.Shared.Rent(totalAsync);
                _faultTable = new FaultTable<T>(_faultTable.SyncFaults, asyncFaults, _faultTable.SubscribeFaults);
            }

            if (_notifyHandlers.Length < totalNotify)
            {
                ReturnArraysForRebuild(false, false, true, false, false);
                _notifyHandlers = ArrayPool<EventNotifyDelegate<T>>.Shared.Rent(totalNotify);
            }

            if (_subscribeHandlers.Length < totalSubscribe)
            {
                ReturnArraysForRebuild(false, false, false, true, false);
                _subscribeHandlers = ArrayPool<EventNotifyDelegate<T>>.Shared.Rent(totalSubscribe);
                var subscribeFaults = ArrayPool<FaultSlot>.Shared.Rent(totalSubscribe);
                _faultTable = new FaultTable<T>(_faultTable.SyncFaults, _faultTable.AsyncFaults, subscribeFaults);
            }

            if (_flatParallel.Length < totalParallel)
            {
                if (_flatParallel != Array.Empty<ParallelHandlerEntry<T>>())
                    ArrayPool<ParallelHandlerEntry<T>>.Shared.Return(_flatParallel, true);
                _flatParallel = ArrayPool<ParallelHandlerEntry<T>>.Shared.Rent(totalParallel);
            }
        }

        private void ClearArrays(int sIdx, int aIdx, int nIdx, int nsIdx, int pIdx)
        {
            Array.Clear(_syncHandlers, sIdx, _syncHandlers.Length - sIdx);
            Array.Clear(_asyncHandlers, aIdx, _asyncHandlers.Length - aIdx);
            Array.Clear(_notifyHandlers, nIdx, _notifyHandlers.Length - nIdx);
            Array.Clear(_subscribeHandlers, nsIdx, _subscribeHandlers.Length - nsIdx);
            Array.Clear(_flatParallel, pIdx, _flatParallel.Length - pIdx);

            Array.Clear(_faultTable.SyncFaults, sIdx, _faultTable.SyncFaults.Length - sIdx);
            Array.Clear(_faultTable.AsyncFaults, aIdx, _faultTable.AsyncFaults.Length - aIdx);
            Array.Clear(_faultTable.SubscribeFaults, nsIdx, _faultTable.SubscribeFaults.Length - nsIdx);
        }

        private void ReturnArraysForRebuild(bool sync, bool async, bool notify, bool subscribe, bool parallel)
        {
            if (sync)
            {
                ReturnArrayHelper(ref _syncHandlers);
                if (_faultTable.SyncFaults != Array.Empty<FaultSlot>())
                {
                    ArrayPool<FaultSlot>.Shared.Return(_faultTable.SyncFaults, true);
                    _faultTable = new FaultTable<T>(Array.Empty<FaultSlot>(), _faultTable.AsyncFaults,
                        _faultTable.SubscribeFaults);
                }
            }

            if (async)
            {
                ReturnArrayHelper(ref _asyncHandlers);
                if (_faultTable.AsyncFaults != Array.Empty<FaultSlot>())
                {
                    ArrayPool<FaultSlot>.Shared.Return(_faultTable.AsyncFaults, true);
                    _faultTable = new FaultTable<T>(_faultTable.SyncFaults, Array.Empty<FaultSlot>(),
                        _faultTable.SubscribeFaults);
                }
            }

            if (notify) ReturnArrayHelper(ref _notifyHandlers);

            if (subscribe)
            {
                ReturnArrayHelper(ref _subscribeHandlers);
                if (_faultTable.SubscribeFaults != Array.Empty<FaultSlot>())
                {
                    ArrayPool<FaultSlot>.Shared.Return(_faultTable.SubscribeFaults, true);
                    _faultTable = new FaultTable<T>(_faultTable.SyncFaults, _faultTable.AsyncFaults,
                        Array.Empty<FaultSlot>());
                }
            }
        }

        private void IdentifySpecializations()
        {
            var singleSync = _syncCountTotal == 1 && _asyncCountTotal == 0 && _parallelCountTotal == 0 &&
                             _notifyCountTotal == 0 && _notifySafeCountTotal == 0;
            if (singleSync)
            {
                _singleSyncHandler = _syncHandlers[0];
                _isSingleSync = true;
            }
            else
            {
                _isSingleSync = false;
                _singleSyncHandler = null;
            }

            var singleNotify = _notifyCountTotal == 1 && _asyncCountTotal == 0 && _parallelCountTotal == 0 &&
                               _syncCountTotal == 0 && _notifySafeCountTotal == 0;
            if (singleNotify)
            {
                _singleNotifyHandler = _notifyHandlers[0];
                _isSingleNotify = true;
            }
            else
            {
                _isSingleNotify = false;
                _singleNotifyHandler = null;
            }

            var singleSubscribe = _notifySafeCountTotal == 1 && _asyncCountTotal == 0 && _parallelCountTotal == 0 &&
                                  _syncCountTotal == 0 && _notifyCountTotal == 0;
            if (singleSubscribe)
            {
                _singleSubscribeHandler = _subscribeHandlers[0];
                _isSingleNotifySafe = true;
            }
            else
            {
                _isSingleNotifySafe = false;
                _singleSubscribeHandler = null;
            }

            _isSmallNotifyFanoutOnly = _notifyCountTotal + _notifySafeCountTotal is >= 2 and <= 8 &&
                                       _asyncCountTotal == 0 && _parallelCountTotal == 0 && _syncCountTotal == 0;
        }

        public void Add(int layerIndex, IEventHandler<T> h)
        {
            GetOrCreate(layerIndex).Add(h);
            MarkDirty();
        }

        public void Add(int layerIndex, IEventHandlerAsync<T> h)
        {
            GetOrCreate(layerIndex).Add(h);
            MarkDirty();
        }

        public void AddNotify(int layerIndex, EventNotifyDelegate<T> h)
        {
            GetOrCreate(layerIndex).AddNotify(h);
            MarkDirty();
        }

        public void AddSubscribe(int layerIndex, EventNotifyDelegate<T> h)
        {
            GetOrCreate(layerIndex).AddSubscribe(h);
            MarkDirty();
        }

        public void AddParallel(int layerIndex, IEventHandler<T> h, Action<int, int, int, Exception> re)
        {
            GetOrCreate(layerIndex).AddParallel(h, re);
            MarkDirty();
        }

        public void AddParallel(int layerIndex, EventNotifyDelegate<T> h, Action<int, int, int, Exception> re)
        {
            GetOrCreate(layerIndex).AddParallel(h, re);
            MarkDirty();
        }

        public void Add(int layerIndex, EventHandleDelegate<T> h)
        {
            GetOrCreate(layerIndex).Add(h);
            MarkDirty();
        }

        public void Add(int layerIndex, EventHandleDelegateAsync<T> h)
        {
            GetOrCreate(layerIndex).Add(h);
            MarkDirty();
        }

        public void Remove(int layerIndex, IEventHandler<T> h)
        {
            if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null)
            {
                _buckets[layerIndex]!.Remove(h);
                MarkDirty();
            }
        }

        public void Remove(int layerIndex, IEventHandlerAsync<T> h)
        {
            if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null)
            {
                _buckets[layerIndex]!.Remove(h);
                MarkDirty();
            }
        }

        public void RemoveParallel(int layerIndex, EventNotifyDelegate<T> h)
        {
            if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null)
            {
                _buckets[layerIndex]!.RemoveParallel(h);
                MarkDirty();
            }
        }

        public void RemoveParallel(int layerIndex, IEventHandler<T> h)
        {
            if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null)
            {
                _buckets[layerIndex]!.RemoveParallel(h);
                MarkDirty();
            }
        }

        public void RemoveNotify(int layerIndex, EventNotifyDelegate<T> h)
        {
            if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null)
            {
                _buckets[layerIndex]!.RemoveNotify(h);
                MarkDirty();
            }
        }

        public void RemoveSubscribe(int layerIndex, EventNotifyDelegate<T> h)
        {
            if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null)
            {
                _buckets[layerIndex]!.RemoveSubscribe(h);
                MarkDirty();
            }
        }

        public void Remove(int layerIndex, EventHandleDelegate<T> h)
        {
            if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null)
            {
                _buckets[layerIndex]!.Remove(h);
                MarkDirty();
            }
        }

        public void Remove(int layerIndex, EventHandleDelegateAsync<T> h)
        {
            if (layerIndex >= 0 && layerIndex < _buckets.Length && _buckets[layerIndex] != null)
            {
                _buckets[layerIndex]!.Remove(h);
                MarkDirty();
            }
        }

        #region IEventBucketNonGeneric
        void IEventBucketNonGeneric.AddFlow(int layerIndex, object handler)
        {
            if (handler is IEventHandler<T> h)
                Add(layerIndex, h);
            else
                Add(layerIndex, (EventHandleDelegate<T>)handler);
        }

        void IEventBucketNonGeneric.AddAsync(int layerIndex, object handler)
        {
            if (handler is IEventHandlerAsync<T> h)
                Add(layerIndex, h);
            else
                Add(layerIndex, (EventHandleDelegateAsync<T>)handler);
        }

        void IEventBucketNonGeneric.AddNotify(int layerIndex, object handler)
        {
            AddNotify(layerIndex, (EventNotifyDelegate<T>)handler);
        }

        void IEventBucketNonGeneric.AddSubscribe(int layerIndex, object handler)
        {
            AddSubscribe(layerIndex, (EventNotifyDelegate<T>)handler);
        }

        void IEventBucketNonGeneric.AddParallel(int layerIndex, object handler,
                                                 Action<int, int, int, Exception> reportError)
        {
            if (handler is IEventHandler<T> h)
                AddParallel(layerIndex, h, reportError);
            else
                AddParallel(layerIndex, (EventNotifyDelegate<T>)handler, reportError);
        }

        void IEventBucketNonGeneric.RemoveFlow(int layerIndex, object handler)
        {
            if (handler is IEventHandler<T> h)
                Remove(layerIndex, h);
            else if (handler is EventHandleDelegate<T> d)
                Remove(layerIndex, d);
        }

        void IEventBucketNonGeneric.RemoveAsync(int layerIndex, object handler)
        {
            if (handler is IEventHandlerAsync<T> h)
                Remove(layerIndex, h);
            else if (handler is EventHandleDelegateAsync<T> d)
                Remove(layerIndex, d);
        }

        void IEventBucketNonGeneric.RemoveNotify(int layerIndex, object handler)
        {
            RemoveNotify(layerIndex, (EventNotifyDelegate<T>)handler);
        }

        void IEventBucketNonGeneric.RemoveSubscribe(int layerIndex, object handler)
        {
            RemoveSubscribe(layerIndex, (EventNotifyDelegate<T>)handler);
        }

        void IEventBucketNonGeneric.RemoveParallel(int layerIndex, object handler)
        {
            if (handler is IEventHandler<T> h)
                RemoveParallel(layerIndex, h);
            else
                RemoveParallel(layerIndex, (EventNotifyDelegate<T>)handler);
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventHandledState Dispatch(in T value)
        {
            EnsureClean();

            if (_isSingleNotify) return DispatchSingleNotify(in value);
            if (_isSmallNotifyFanoutOnly && _notifyCountTotal > 0)
            {
                DispatchSmallNotifyFanout(0, _notifyCountTotal, in value);
                return EventHandledState.Continue;
            }

            if (_isSingleNotifySafe) return DispatchSingleNotifySafe(in value);
            if (_isSingleSync) return DispatchSingleSync(in value);

            if (_notifyMask != 0) DispatchNotify(0, _notifyCountTotal, in value);
            if (_notifySafeMask != 0) DispatchNotifySafe(0, _notifySafeCountTotal, in value);
            var res = EventHandledState.Continue;
            if (_syncMask != 0)
            {
                res = DispatchSync(0, _syncCountTotal, in value);
                if (res == EventHandledState.Handled) return res;
            }

            if (_asyncMask != 0) DispatchAsync(0, _asyncCountTotal, in value);
            if (_parallelMask != 0)
                for (var j = 0; j < _parallelCountTotal; j++)
                    _flatParallel[j].Enqueue(-1, in value);

            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchSingleSync(in T value)
        {
            try
            {
                return _singleSyncHandler(in value);
            }
            catch (Exception ex)
            {
                HandleFault(FaultKind.Sync, 0, in value, ex);
                return EventHandledState.Continue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchSingleNotify(in T value)
        {
            _singleNotifyHandler(in value);
            return EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchSingleNotifySafe(in T value)
        {
            try
            {
                _singleSubscribeHandler(in value);
            }
            catch (Exception ex)
            {
                HandleFault(FaultKind.Subscribe, 0, in value, ex);
            }

            return EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchSmallNotifyFanout(int start, int count, in T value)
        {
            ref var hBase = ref GetArrayDataRef(_notifyHandlers);
            Unsafe.Add(ref hBase, start)(in value);
            Unsafe.Add(ref hBase, start + 1)(in value);
            if (count == 2) return;
            Unsafe.Add(ref hBase, start + 2)(in value);
            if (count == 3) return;
            Unsafe.Add(ref hBase, start + 3)(in value);
            if (count == 4) return;
            Unsafe.Add(ref hBase, start + 4)(in value);
            if (count == 5) return;
            Unsafe.Add(ref hBase, start + 5)(in value);
            if (count == 6) return;
            Unsafe.Add(ref hBase, start + 6)(in value);
            if (count == 7) return;
            Unsafe.Add(ref hBase, start + 7)(in value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchNotify(int start, int end, in T e)
        {
            if (start >= end) return;
            ref var hBase = ref GetArrayDataRef(_notifyHandlers);
            var i = start;
            for (; i <= end - 8; i += 8)
            {
                Unsafe.Add(ref hBase, i)(in e);
                Unsafe.Add(ref hBase, i + 1)(in e);
                Unsafe.Add(ref hBase, i + 2)(in e);
                Unsafe.Add(ref hBase, i + 3)(in e);
                Unsafe.Add(ref hBase, i + 4)(in e);
                Unsafe.Add(ref hBase, i + 5)(in e);
                Unsafe.Add(ref hBase, i + 6)(in e);
                Unsafe.Add(ref hBase, i + 7)(in e);
            }

            for (; i <= end - 2; i += 2)
            {
                Unsafe.Add(ref hBase, i)(in e);
                Unsafe.Add(ref hBase, i + 1)(in e);
            }

            for (; i < end; i++) Unsafe.Add(ref hBase, i)(in e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchNotifySafe(int start, int end, in T value)
        {
            if (start >= end) return;
            ref var hBase = ref GetArrayDataRef(_subscribeHandlers);
            var i = start;
            var currentIndex = start;
            try
            {
                for (; i <= end - 4; i += 4)
                {
                    currentIndex = i;
                    Unsafe.Add(ref hBase, i)(in value);
                    currentIndex = i + 1;
                    Unsafe.Add(ref hBase, i + 1)(in value);
                    currentIndex = i + 2;
                    Unsafe.Add(ref hBase, i + 2)(in value);
                    currentIndex = i + 3;
                    Unsafe.Add(ref hBase, i + 3)(in value);
                }

                for (; i < end; i++)
                {
                    currentIndex = i;
                    Unsafe.Add(ref hBase, i)(in value);
                }
            }
            catch (Exception e)
            {
                HandleFault(FaultKind.Subscribe, currentIndex, in value, e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchSync(int start, int end, in T value)
        {
            if (start >= end) return EventHandledState.Continue;
            ref var hBase = ref GetArrayDataRef(_syncHandlers);
            var combinedState = 0;
            var i = start;
            var currentIndex = start;
            try
            {
                for (; i <= end - 4; i += 4)
                {
                    currentIndex = i;
                    var r1 = Unsafe.Add(ref hBase, i)(in value);
                    if (r1 == EventHandledState.Handled) return EventHandledState.Handled;
                    currentIndex = i + 1;
                    var r2 = Unsafe.Add(ref hBase, i + 1)(in value);
                    if (r2 == EventHandledState.Handled) return EventHandledState.Handled;
                    currentIndex = i + 2;
                    var r3 = Unsafe.Add(ref hBase, i + 2)(in value);
                    if (r3 == EventHandledState.Handled) return EventHandledState.Handled;
                    currentIndex = i + 3;
                    var r4 = Unsafe.Add(ref hBase, i + 3)(in value);
                    if (r4 == EventHandledState.Handled) return EventHandledState.Handled;
                    combinedState |= (int)r1 | (int)r2 | (int)r3 | (int)r4;
                }

                for (; i < end; i++)
                {
                    currentIndex = i;
                    var state = Unsafe.Add(ref hBase, i)(in value);
                    if (state == EventHandledState.Handled) return EventHandledState.Handled;
                    combinedState |= (int)state;
                }
            }
            catch (Exception e)
            {
                HandleFault(FaultKind.Sync, currentIndex, in value, e);
                return EventHandledState.Continue;
            }

            return (combinedState & 2) != 0 ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchAsync(int start, int end, in T value)
        {
            var hs = _asyncHandlers;
            var ft = _faultTable;
            for (var i = start; i < end; i++)
                AsyncFaultContext<T>.Observe(this, ft, FaultKind.Async, i, in value, hs[i](value));
        }

        internal void HandleFault(FaultKind kind, int index, in T value, Exception e)
        {
            HandleFault(_faultTable, kind, index, in value, e);
        }

        internal void HandleFault(FaultTable<T> faultTable, FaultKind kind, int index, in T value, Exception e)
        {
            EventMetaDataHandler.OnEventExpectation(value, e);

            var slot = kind switch
                       {
                           FaultKind.Sync      => faultTable.SyncFaults[index],
                           FaultKind.Async     => faultTable.AsyncFaults[index],
                           FaultKind.Subscribe => faultTable.SubscribeFaults[index],
                           _                   => default
                       };

            if (slot.Circuit == null || !slot.Circuit.TryDisable()) return;

            var handlerName = EventDiagnosticSymbols.Resolve(slot.HandlerNameId);
            var eventName = EventDiagnosticSymbols.Resolve(faultTable.EventNameId);

            LayerHub.ReportLayerEventError(slot.LayerIndex, handlerName, eventName, e);
            MarkDirty();
        }

        private HandlerBucket<T> GetOrCreate(int layerIndex)
        {
            if (layerIndex >= _buckets.Length)
                lock (_lock)
                {
                    if (layerIndex >= _buckets.Length)
                    {
                        var next = new HandlerBucket<T>?[Math.Max(layerIndex + 1, _buckets.Length * 2)];
                        Array.Copy(_buckets, next, _buckets.Length);
                        _buckets = next;
                    }
                }

            var b = _buckets[layerIndex];
            if (b == null)
                lock (_lock)
                {
                    b = _buckets[layerIndex] ??= new HandlerBucket<T>(MarkDirty);
                }

            return b;
        }
    }

    private sealed class AsyncFaultContext<T> where T : struct
    {
        private const int MAX_POOL_SIZE = 1024;
        private static readonly ConcurrentQueue<AsyncFaultContext<T>> s_pool = new();
        private static int s_poolCount;
        private readonly Action _continuation;
        private EventBucket<T>? _owner;
        private FaultTable<T>? _capturedFaultTable;
        private FaultKind _kind;
        private int _faultIndex;
        private T _payload;
        private LBTask _task;

        private AsyncFaultContext()
        {
            _continuation = Complete;
        }

        public static void Observe(EventBucket<T> owner,   FaultTable<T> faultTable, FaultKind kind, int faultIndex,
                                   in T           payload, LBTask        task)
        {
            if (!s_pool.TryDequeue(out var context)) context = new AsyncFaultContext<T>();
            else Interlocked.Decrement(ref s_poolCount);
            context._owner = owner;
            context._capturedFaultTable = faultTable;
            context._kind = kind;
            context._faultIndex = faultIndex;
            context._payload = payload;
            context._task = task;
            task.GetAwaiter().OnCompleted(context._continuation);
        }

        private void Complete()
        {
            try
            {
                _task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                if (_owner != null && _capturedFaultTable != null)
                {
                    _owner.HandleFault(_capturedFaultTable, _kind, _faultIndex, in _payload, ex);
                }
            }
            finally
            {
                _owner = null;
                _capturedFaultTable = null;
                _kind = default;
                _faultIndex = -1;
                _payload = default;
                _task = default;
                if (Interlocked.Increment(ref s_poolCount) <= MAX_POOL_SIZE) s_pool.Enqueue(this);
                else Interlocked.Decrement(ref s_poolCount);
            }
        }
    }
}