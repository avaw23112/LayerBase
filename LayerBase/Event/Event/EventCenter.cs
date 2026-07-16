using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
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
    private EventBucketBase?[] _eventBuckets = Array.Empty<EventBucketBase?>();

    internal PostScheduler? PostScheduler { get; set; }

    internal void SubscribeFlow<T>(int layerIndex, IEventHandler<T> handler) where T : struct
    {
        GetOrCreateBucket<T>().Add(layerIndex, handler);
    }

    internal void SubscribeAsync<T>(int layerIndex, IEventHandlerAsync<T> handler) where T : struct
    {
        GetOrCreateBucket<T>().Add(layerIndex, handler);
    }

    internal void SubscribeFlow<T>(int layerIndex, EventHandleDelegate<T> handleDelegate) where T : struct
    {
        GetOrCreateBucket<T>().Add(layerIndex, handleDelegate);
    }

    internal void SubscribeAsync<T>(int layerIndex, EventHandleDelegateAsync<T> handleDelegate) where T : struct
    {
        GetOrCreateBucket<T>().Add(layerIndex, handleDelegate);
    }

    internal void SubscribeNotify<T>(int layerIndex, EventNotifyDelegate<T> handler) where T : struct
    {
        GetOrCreateBucket<T>().AddNotify(layerIndex, handler);
    }

    internal void Subscribe<T>(int layerIndex, EventNotifyDelegate<T> handler) where T : struct
    {
        GetOrCreateBucket<T>().AddSubscribe(layerIndex, handler);
    }

    internal void UnsubscribeFlow<T>(int layerIndex, IEventHandler<T> handler) where T : struct
    {
        TryGetBucket<T>()?.Remove(layerIndex, handler);
    }

    internal void UnsubscribeAsync<T>(int layerIndex, IEventHandlerAsync<T> handler) where T : struct
    {
        TryGetBucket<T>()?.Remove(layerIndex, handler);
    }

    internal void UnsubscribeNotify<T>(int layerIndex, EventNotifyDelegate<T> handler) where T : struct
    {
        TryGetBucket<T>()?.RemoveNotify(layerIndex, handler);
    }

    internal void Unsubscribe<T>(int layerIndex, EventNotifyDelegate<T> handler) where T : struct
    {
        TryGetBucket<T>()?.RemoveSubscribe(layerIndex, handler);
    }

    internal void UnsubscribeFlow<T>(int layerIndex, EventHandleDelegate<T> handleDelegate) where T : struct
    {
        TryGetBucket<T>()?.Remove(layerIndex, handleDelegate);
    }

    internal void UnsubscribeAsync<T>(int layerIndex, EventHandleDelegateAsync<T> handleDelegate) where T : struct
    {
        TryGetBucket<T>()?.Remove(layerIndex, handleDelegate);
    }

    /// <summary>
    /// 派发同步事件。
    /// </summary>
    /// <param name="value">事件数据。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventHandledState Send<T>(in T value) where T : struct
    {
        var bucket = TryGetBucket<T>();
        return bucket == null ? EventHandledState.Continue : bucket.Dispatch(in value);
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
        for (var i = 0; i < _eventBuckets.Length; i++)
        {
            _eventBuckets[i]?.Dispose();
            _eventBuckets[i] = null;
        }
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
            bucket = GetOrCreateBucket<TEvent>();
        }

        // 3. 预热派发表
        if ((options.Targets & LayerPrewarmTargets.DispatchTable) != 0)
        {
            bucket ??= GetOrCreateBucket<TEvent>();
            bucket.PrewarmDispatchTable();
        }

        // 4. 预热 Post 队列
        if ((options.Targets & LayerPrewarmTargets.PostQueue) != 0)
        {
            PostScheduler?.PrewarmEvent<TEvent>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventBucket<T>? TryGetBucket<T>() where T : struct
    {
        var typeId = EventTypeId<T>.Id;
        if ((uint)typeId >= (uint)_eventBuckets.Length) return null;
        return (EventBucket<T>?)_eventBuckets[typeId];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventBucket<T> GetOrCreateBucket<T>() where T : struct
    {
        var typeId = EventTypeId<T>.Id;
        if ((uint)typeId < (uint)_eventBuckets.Length &&
            _eventBuckets[typeId] is EventBucket<T> existing)
        {
            return existing;
        }

        EnsureBucketCapacity(typeId);
        var created = new EventBucket<T>();
        _eventBuckets[typeId] = created;
        return created;
    }

    private void EnsureBucketCapacity(int typeId)
    {
        if ((uint)typeId < (uint)_eventBuckets.Length) return;

        var nextSize = _eventBuckets.Length == 0 ? 4 : _eventBuckets.Length;
        while (nextSize <= typeId)
            nextSize <<= 1;

        Array.Resize(ref _eventBuckets, nextSize);
    }

    private abstract class EventBucketBase : IDisposable
    {
        public abstract void Dispose();
    }

    private sealed class EventBucket<T> : EventBucketBase where T : struct
    {
        private bool _disposed;
        private int _isDirty;

        private HandlerBucket<T>?[] _buckets = Array.Empty<HandlerBucket<T>>();

        private EventHandleDelegate<T>[] _syncHandlers = Array.Empty<EventHandleDelegate<T>>();
        private EventHandleDelegateAsync<T>[] _asyncHandlers = Array.Empty<EventHandleDelegateAsync<T>>();
        private EventNotifyDelegate<T>[] _notifyHandlers = Array.Empty<EventNotifyDelegate<T>>();
        private EventNotifyDelegate<T>[] _subscribeHandlers = Array.Empty<EventNotifyDelegate<T>>();

        private FaultTable<T> _faultTable =
            new(Array.Empty<FaultSlot>(), Array.Empty<FaultSlot>(), Array.Empty<FaultSlot>());

        private int _syncCountTotal;
        private int _asyncCountTotal;
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
        private ulong _notifyMask;
        private ulong _notifySafeMask;

        /// <summary>
        /// 预热当前事件类型的派发表。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrewarmDispatchTable()
        {
            EnsureClean();
        }

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ReturnArrays();
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
            _notifyCountTotal = 0;
            _notifySafeCountTotal = 0;

            _subscriberMask = 0;
            _syncMask = 0;
            _asyncMask = 0;
            _notifyMask = 0;
            _notifySafeMask = 0;

            ReturnArrayHelper(ref _syncHandlers);
            ReturnArrayHelper(ref _asyncHandlers);
            ReturnArrayHelper(ref _notifyHandlers);
            ReturnArrayHelper(ref _subscribeHandlers);

            ReturnFaultArrays(_faultTable);
            _faultTable = new FaultTable<T>(Array.Empty<FaultSlot>(), Array.Empty<FaultSlot>(),
                Array.Empty<FaultSlot>());
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
            if (Volatile.Read(ref _isDirty) == 0) return;
            if (Interlocked.Exchange(ref _isDirty, 0) != 0) Rebuild();
        }

        private void Rebuild()
        {
            if (_disposed) return;

            int totalSync = 0, totalAsync = 0, totalNotify = 0, totalSubscribe = 0;
            ulong newMask = 0,
                newSyncMask = 0,
                newAsyncMask = 0,
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
                totalNotify += bNotify;
                totalSubscribe += bSubscribe;
                var bit = 1UL << i;
                if (bSync > 0) newSyncMask |= bit;
                if (bAsync > 0) newAsyncMask |= bit;
                if (bNotify > 0) newNotifyMask |= bit;
                if (bSubscribe > 0) newSubscribeMask |= bit;
                if (bSync > 0 || bAsync > 0 || bNotify > 0 || bSubscribe > 0) newMask |= bit;
            }

            RentArrays(totalSync, totalAsync, totalNotify, totalSubscribe);

            int sIdx = 0, aIdx = 0, nIdx = 0, nsIdx = 0;
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
                }
            }

            ClearArrays(sIdx, aIdx, nIdx, nsIdx);
            _syncCountTotal = sIdx;
            _asyncCountTotal = aIdx;
            _notifyCountTotal = nIdx;
            _notifySafeCountTotal = nsIdx;

            _subscriberMask = newMask;
            _syncMask = newSyncMask;
            _asyncMask = newAsyncMask;
            _notifyMask = newNotifyMask;
            _notifySafeMask = newSubscribeMask;

            IdentifySpecializations();
        }

        private void RentArrays(int totalSync, int totalAsync, int totalNotify, int totalSubscribe)
        {
            if (_syncHandlers.Length < totalSync)
            {
                ReturnArraysForRebuild(true, false, false, false);
                _syncHandlers = ArrayPool<EventHandleDelegate<T>>.Shared.Rent(totalSync);
                var syncFaults = ArrayPool<FaultSlot>.Shared.Rent(totalSync);
                _faultTable = new FaultTable<T>(syncFaults, _faultTable.AsyncFaults, _faultTable.SubscribeFaults);
            }

            if (_asyncHandlers.Length < totalAsync)
            {
                ReturnArraysForRebuild(false, true, false, false);
                _asyncHandlers = ArrayPool<EventHandleDelegateAsync<T>>.Shared.Rent(totalAsync);
                var asyncFaults = ArrayPool<FaultSlot>.Shared.Rent(totalAsync);
                _faultTable = new FaultTable<T>(_faultTable.SyncFaults, asyncFaults, _faultTable.SubscribeFaults);
            }

            if (_notifyHandlers.Length < totalNotify)
            {
                ReturnArraysForRebuild(false, false, true, false);
                _notifyHandlers = ArrayPool<EventNotifyDelegate<T>>.Shared.Rent(totalNotify);
            }

            if (_subscribeHandlers.Length < totalSubscribe)
            {
                ReturnArraysForRebuild(false, false, false, true);
                _subscribeHandlers = ArrayPool<EventNotifyDelegate<T>>.Shared.Rent(totalSubscribe);
                var subscribeFaults = ArrayPool<FaultSlot>.Shared.Rent(totalSubscribe);
                _faultTable = new FaultTable<T>(_faultTable.SyncFaults, _faultTable.AsyncFaults, subscribeFaults);
            }
        }

        private void ClearArrays(int sIdx, int aIdx, int nIdx, int nsIdx)
        {
            Array.Clear(_syncHandlers, sIdx, _syncHandlers.Length - sIdx);
            Array.Clear(_asyncHandlers, aIdx, _asyncHandlers.Length - aIdx);
            Array.Clear(_notifyHandlers, nIdx, _notifyHandlers.Length - nIdx);
            Array.Clear(_subscribeHandlers, nsIdx, _subscribeHandlers.Length - nsIdx);

            Array.Clear(_faultTable.SyncFaults, sIdx, _faultTable.SyncFaults.Length - sIdx);
            Array.Clear(_faultTable.AsyncFaults, aIdx, _faultTable.AsyncFaults.Length - aIdx);
            Array.Clear(_faultTable.SubscribeFaults, nsIdx, _faultTable.SubscribeFaults.Length - nsIdx);
        }

        private void ReturnArraysForRebuild(bool sync, bool async, bool notify, bool subscribe)
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
            var singleSync = _syncCountTotal == 1 && _asyncCountTotal == 0 &&
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

            var singleNotify = _notifyCountTotal == 1 && _asyncCountTotal == 0 &&
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

            var singleSubscribe = _notifySafeCountTotal == 1 && _asyncCountTotal == 0 &&
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

            _isSmallNotifyFanoutOnly = _notifyCountTotal is >= 2 and <= 8 &&
                                       _notifySafeCountTotal == 0 &&
                                       _asyncCountTotal == 0 && _syncCountTotal == 0;
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
            EventHandleDelegateAsync<T>[] handlers = _asyncHandlers;
            FaultTable<T> faultTable = _faultTable;

            for (int i = start; i < end; i++)
            {
                LBTask task;

                try
                {
                    task = handlers[i](value);
                }
                catch (Exception ex)
                {
                    HandleFault(
                        faultTable,
                        FaultKind.Async,
                        i,
                        in value,
                        ex);
                    continue;
                }

                var awaiter = task.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    try
                    {
                        awaiter.GetResult();
                    }
                    catch (Exception ex)
                    {
                        HandleFault(
                            faultTable,
                            FaultKind.Async,
                            i,
                            in value,
                            ex);
                    }

                    continue;
                }

                AsyncFaultContext<T>.ObserveIncomplete(
                    this,
                    faultTable.AsyncFaults[i],
                    faultTable.EventNameId,
                    in value,
                    task);
            }
        }

        internal void HandleFault(FaultKind kind, int index, in T value, Exception e)
        {
            HandleFault(_faultTable, kind, index, in value, e);
        }

        internal void HandleFault(FaultTable<T> faultTable, FaultKind kind, int index, in T value, Exception e)
        {
            var slot = GetFaultSlot(faultTable, kind, index);
            HandleFault(slot, faultTable.EventNameId, in value, e);
        }

        internal void HandleFault(FaultSlot slot, int eventNameId, in T value, Exception e)
        {
            EventMetaDataHandler.OnEventExpectation(value, e);
            if (slot.Circuit == null || !slot.Circuit.TryDisable()) return;

            var handlerName = EventDiagnosticSymbols.Resolve(slot.HandlerNameId);
            var eventName = EventDiagnosticSymbols.Resolve(eventNameId);

            MarkDirty();
            LayerHub.ReportLayerEventError(slot.LayerIndex, handlerName, eventName, e);
        }

        private static FaultSlot GetFaultSlot(FaultTable<T> faultTable, FaultKind kind, int index)
        {
            return kind switch
                   {
                       FaultKind.Sync      => faultTable.SyncFaults[index],
                       FaultKind.Async     => faultTable.AsyncFaults[index],
                       FaultKind.Subscribe => faultTable.SubscribeFaults[index],
                       _                   => default
                   };
        }

        private HandlerBucket<T> GetOrCreate(int layerIndex)
        {
            if (layerIndex >= _buckets.Length)
            {
                var nextSize = _buckets.Length == 0 ? 4 : _buckets.Length;
                while (nextSize <= layerIndex)
                    nextSize <<= 1;
                Array.Resize(ref _buckets, nextSize);
            }

            var b = _buckets[layerIndex];
            if (b == null)
                b = _buckets[layerIndex] = new HandlerBucket<T>(MarkDirty);

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
        private FaultSlot _faultSlot;
        private int _eventNameId;
        private int _active;
        private T _payload;
        private LBTask _task;

        private AsyncFaultContext()
        {
            _continuation = Complete;
        }

        public static void ObserveIncomplete(
            EventBucket<T> owner,
            FaultSlot faultSlot,
            int eventNameId,
            in T payload,
            LBTask task)
        {
            Debug.Assert(
                !task.GetAwaiter().IsCompleted,
                "ObserveIncomplete must only receive an incomplete LBTask.");

            if (!s_pool.TryDequeue(out var context))
                context = new AsyncFaultContext<T>();
            else
                Interlocked.Decrement(ref s_poolCount);

            context._owner = owner;
            context._faultSlot = faultSlot;
            context._eventNameId = eventNameId;
            context._payload = payload;
            context._task = task;
            Volatile.Write(ref context._active, 1);
            task.GetAwaiter().OnCompleted(context._continuation);
        }

        private void Complete()
        {
            if (Interlocked.Exchange(ref _active, 0) == 0) return;

            try
            {
                _task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                if (_owner != null)
                {
                    _owner.HandleFault(_faultSlot, _eventNameId, in _payload, ex);
                }
            }
            finally
            {
                _owner = null;
                _faultSlot = default;
                _eventNameId = 0;
                _payload = default;
                _task = default;
                if (Interlocked.Increment(ref s_poolCount) <= MAX_POOL_SIZE) s_pool.Enqueue(this);
                else Interlocked.Decrement(ref s_poolCount);
            }
        }
    }
}
