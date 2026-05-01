using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LayerBase.Async;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Core.Event;

public enum Propagation
{
    Global
}

/// <summary>
/// 全局事件中心，负责事件的订阅管理及同步派发。
/// </summary>
public sealed class EventCenter
{
    private readonly ConcurrentDictionary<int, Action> _bucketCacheResetters = new();
    private readonly ConcurrentDictionary<int, object> _eventBuckets = new();
    private readonly object _lock = new();
    private int _isResetting;

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
                                       Action<int, string, string, Exception> reportError) where T : struct
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
                if (bucket is IResetable b)
                    b.Dispose();
            _eventBuckets.Clear();
            foreach (var resetter in _bucketCacheResetters.Values) resetter();
            _bucketCacheResetters.Clear();
        }

        Volatile.Write(ref _isResetting, 0);
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

    private static class BucketCache<T> where T : struct
    {
        public static EventBucket<T>? Instance;
    }

    private interface IResetable : IDisposable
    {
        void Reset();
    }

    private sealed class EventBucketSnapshot<TEvent> where TEvent : struct
    {
        public static readonly EventBucketSnapshot<TEvent> Empty = new(
            Array.Empty<EventHandleDelegate<TEvent>>(),
            Array.Empty<HandlerCircuit>(),
            Array.Empty<string>(),
            Array.Empty<EventHandleDelegateAsync<TEvent>>(),
            Array.Empty<HandlerCircuit>(),
            Array.Empty<string>(),
            Array.Empty<EventNotifyDelegate<TEvent>>(),
            Array.Empty<HandlerCircuit>(),
            Array.Empty<string>(),
            Array.Empty<EventNotifyDelegate<TEvent>>(),
            Array.Empty<HandlerCircuit>(),
            Array.Empty<string>(),
            Array.Empty<ParallelHandlerEntry<TEvent>>(),
            0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0);

        public readonly EventHandleDelegate<TEvent>[] SyncHandlers;
        public readonly HandlerCircuit[] SyncCircuits;
        public readonly string[] SyncNames;
        public readonly EventHandleDelegateAsync<TEvent>[] AsyncHandlers;
        public readonly HandlerCircuit[] AsyncCircuits;
        public readonly string[] AsyncNames;
        public readonly EventNotifyDelegate<TEvent>[] NotifyHandlers;
        public readonly HandlerCircuit[] NotifyCircuits;
        public readonly string[] NotifyNames;
        public readonly EventNotifyDelegate<TEvent>[] SubscribeHandlers;
        public readonly HandlerCircuit[] NotifySafeCircuits;
        public readonly string[] NotifySafeNames;
        public readonly ParallelHandlerEntry<TEvent>[] FlatParallel;
        public readonly int SyncCountTotal;
        public readonly int AsyncCountTotal;
        public readonly int ParallelCountTotal;
        public readonly int NotifyCountTotal;
        public readonly int NotifySafeCountTotal;
        public readonly bool IsSingleSync;
        public readonly bool IsSingleNotify;
        public readonly bool IsSingleNotifySafe;
        public readonly bool IsSmallNotifyFanoutOnly;
        public readonly EventHandleDelegate<TEvent>? SingleSyncHandler;
        public readonly EventNotifyDelegate<TEvent>? SingleNotifyHandler;
        public readonly EventNotifyDelegate<TEvent>? SingleSubscribeHandler;
        public readonly ulong SubscriberMask;
        public readonly ulong SyncMask;
        public readonly ulong AsyncMask;
        public readonly ulong ParallelMask;
        public readonly ulong NotifyMask;
        public readonly ulong NotifySafeMask;

        public EventBucketSnapshot(
            EventHandleDelegate<TEvent>[] syncHandlers,
            HandlerCircuit[] syncCircuits,
            string[] syncNames,
            EventHandleDelegateAsync<TEvent>[] asyncHandlers,
            HandlerCircuit[] asyncCircuits,
            string[] asyncNames,
            EventNotifyDelegate<TEvent>[] notifyHandlers,
            HandlerCircuit[] notifyCircuits,
            string[] notifyNames,
            EventNotifyDelegate<TEvent>[] subscribeHandlers,
            HandlerCircuit[] notifySafeCircuits,
            string[] notifySafeNames,
            ParallelHandlerEntry<TEvent>[] flatParallel,
            int syncCountTotal,
            int asyncCountTotal,
            int parallelCountTotal,
            int notifyCountTotal,
            int notifySafeCountTotal,
            ulong subscriberMask,
            ulong syncMask,
            ulong asyncMask,
            ulong parallelMask,
            ulong notifyMask,
            ulong notifySafeMask)
        {
            SyncHandlers = syncHandlers;
            SyncCircuits = syncCircuits;
            SyncNames = syncNames;
            AsyncHandlers = asyncHandlers;
            AsyncCircuits = asyncCircuits;
            AsyncNames = asyncNames;
            NotifyHandlers = notifyHandlers;
            NotifyCircuits = notifyCircuits;
            NotifyNames = notifyNames;
            SubscribeHandlers = subscribeHandlers;
            NotifySafeCircuits = notifySafeCircuits;
            NotifySafeNames = notifySafeNames;
            FlatParallel = flatParallel;
            SyncCountTotal = syncCountTotal;
            AsyncCountTotal = asyncCountTotal;
            ParallelCountTotal = parallelCountTotal;
            NotifyCountTotal = notifyCountTotal;
            NotifySafeCountTotal = notifySafeCountTotal;
            SubscriberMask = subscriberMask;
            SyncMask = syncMask;
            AsyncMask = asyncMask;
            ParallelMask = parallelMask;
            NotifyMask = notifyMask;
            NotifySafeMask = notifySafeMask;

            IsSingleSync = syncCountTotal == 1 && asyncCountTotal == 0 && parallelCountTotal == 0 &&
                           notifyCountTotal == 0 && notifySafeCountTotal == 0;
            if (IsSingleSync) SingleSyncHandler = syncHandlers[0];

            IsSingleNotify = notifyCountTotal == 1 && asyncCountTotal == 0 && parallelCountTotal == 0 &&
                             syncCountTotal == 0 && notifySafeCountTotal == 0;
            if (IsSingleNotify) SingleNotifyHandler = notifyHandlers[0];

            IsSingleNotifySafe = notifySafeCountTotal == 1 && asyncCountTotal == 0 && parallelCountTotal == 0 &&
                                 syncCountTotal == 0 && notifyCountTotal == 0;
            if (IsSingleNotifySafe) SingleSubscribeHandler = subscribeHandlers[0];

            IsSmallNotifyFanoutOnly = notifyCountTotal + notifySafeCountTotal is >= 2 and <= 8 &&
                                      asyncCountTotal == 0 && parallelCountTotal == 0 && syncCountTotal == 0;
        }
    }

    private sealed class EventBucket<T> : IResetable where T : struct
    {
        private readonly object _lock = new();
        public readonly EventCenter? Owner;
        private EventBucketSnapshot<T> _snapshot = EventBucketSnapshot<T>.Empty;
        private HandlerCircuit[] _asyncCircuits = Array.Empty<HandlerCircuit>();

        private EventHandleDelegateAsync<T>[] _asyncHandlers = Array.Empty<EventHandleDelegateAsync<T>>();
        private string[] _asyncNames = Array.Empty<string>();
        private HandlerBucket<T>?[] _buckets = Array.Empty<HandlerBucket<T>>();
        private bool _disposed;

        private ParallelHandlerEntry<T>[] _flatParallel = Array.Empty<ParallelHandlerEntry<T>>();

        private int _isDirty,
            _syncCountTotal,
            _asyncCountTotal,
            _parallelCountTotal,
            _notifyCountTotal,
            _notifySafeCountTotal;

        private bool _isSingleSync, _isSingleNotify, _isSingleNotifySafe;
        private bool _isSmallNotifyFanoutOnly;
        private HandlerCircuit[] _notifyCircuits = Array.Empty<HandlerCircuit>();
        private EventNotifyDelegate<T>[] _notifyHandlers = Array.Empty<EventNotifyDelegate<T>>();
        private string[] _notifyNames = Array.Empty<string>();

        private HandlerCircuit[] _notifySafeCircuits = Array.Empty<HandlerCircuit>();
        private string[] _notifySafeNames = Array.Empty<string>();

        private HandlerCircuit? _singleNotifyCircuit;
        private EventNotifyDelegate<T>? _singleNotifyHandler;
        private string? _singleNotifyName;

        private HandlerCircuit? _singleSubscribeCircuit;
        private EventNotifyDelegate<T>? _singleSubscribeHandler;
        private string? _singleSubscribeName;
        private HandlerCircuit? _singleSyncCircuit;
        private EventHandleDelegate<T>? _singleSyncHandler;
        private string? _singleSyncName;
        private EventNotifyDelegate<T>[] _subscribeHandlers = Array.Empty<EventNotifyDelegate<T>>();

        private ulong _subscriberMask, _syncMask, _asyncMask, _parallelMask, _notifyMask, _notifySafeMask;
        private HandlerCircuit[] _syncCircuits = Array.Empty<HandlerCircuit>();

        private EventHandleDelegate<T>[] _syncHandlers = Array.Empty<EventHandleDelegate<T>>();
        private string[] _syncNames = Array.Empty<string>();

        public EventBucket(EventCenter center)
        {
            Owner = center;
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
            Volatile.Write(ref _snapshot, EventBucketSnapshot<T>.Empty);
            _singleSyncHandler = null;
            _singleSyncCircuit = null;
            _singleSyncName = null;
            _singleNotifyHandler = null;
            _singleNotifyCircuit = null;
            _singleNotifyName = null;
            _singleSubscribeHandler = null;
            _singleSubscribeCircuit = null;
            _singleSubscribeName = null;
            _isSingleSync = false;
            _isSingleNotify = false;
            _isSingleNotifySafe = false;
            _isSmallNotifyFanoutOnly = false;

            ReturnArrayHelper(ref _syncHandlers, ref _syncCircuits, ref _syncNames);
            ReturnArrayHelper(ref _asyncHandlers, ref _asyncCircuits, ref _asyncNames);
            ReturnArrayHelper(ref _notifyHandlers, ref _notifyCircuits, ref _notifyNames);
            ReturnArrayHelper(ref _subscribeHandlers, ref _notifySafeCircuits, ref _notifySafeNames);

            if (_flatParallel != null && _flatParallel.Length > 0 &&
                _flatParallel != Array.Empty<ParallelHandlerEntry<T>>())
            {
                ArrayPool<ParallelHandlerEntry<T>>.Shared.Return(_flatParallel, true);
                _flatParallel = Array.Empty<ParallelHandlerEntry<T>>();
            }
        }

        private void ReturnArrayHelper<TDelegate>(ref TDelegate[] handlers, ref HandlerCircuit[] circuits,
                                                  ref string[]    names)
        {
            if (handlers != null && handlers.Length > 0 && handlers != Array.Empty<TDelegate>())
            {
                ArrayPool<TDelegate>.Shared.Return(handlers, true);
                handlers = Array.Empty<TDelegate>();
            }

            if (circuits != null && circuits.Length > 0 && circuits != Array.Empty<HandlerCircuit>())
            {
                ArrayPool<HandlerCircuit>.Shared.Return(circuits, true);
                circuits = Array.Empty<HandlerCircuit>();
            }

            if (names != null && names.Length > 0 && names != Array.Empty<string>())
            {
                ArrayPool<string>.Shared.Return(names, true);
                names = Array.Empty<string>();
            }
        }

        public void MarkDirty()
        {
            Interlocked.Exchange(ref _isDirty, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventBucketSnapshot<T> EnsureClean()
        {
            if (Volatile.Read(ref _isDirty) == 1)
                lock (_lock)
                {
                    if (_isDirty == 1) Rebuild();
                }

            return _snapshot;
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
                            _syncCircuits[sIdx] = h.Circuit;
                            _syncNames[sIdx] = h.FullName;
                            sIdx++;
                        }

                        if (h.AsyncHandler != null)
                        {
                            _asyncHandlers[aIdx] = h.AsyncHandler;
                            _asyncCircuits[aIdx] = h.Circuit;
                            _asyncNames[aIdx] = h.FullName;
                            aIdx++;
                        }
                    }

                    foreach (var h in b.MasterUnordered)
                    {
                        if (h.Circuit.IsDisabled) continue;
                        if (h.SyncWrapper != null)
                        {
                            _syncHandlers[sIdx] = h.SyncWrapper;
                            _syncCircuits[sIdx] = h.Circuit;
                            _syncNames[sIdx] = h.FullName;
                            sIdx++;
                        }

                        if (h.AsyncWrapper != null)
                        {
                            _asyncHandlers[aIdx] = h.AsyncWrapper;
                            _asyncCircuits[aIdx] = h.Circuit;
                            _asyncNames[aIdx] = h.FullName;
                            aIdx++;
                        }
                    }

                    foreach (var h in b.MasterNotify)
                        if (!h.Circuit.IsDisabled)
                        {
                            _notifyHandlers[nIdx] = h.Handler;
                            _notifyCircuits[nIdx] = h.Circuit;
                            _notifyNames[nIdx] = h.FullName;
                            nIdx++;
                        }

                    foreach (var h in b.MasterSubscribe)
                        if (!h.Circuit.IsDisabled)
                        {
                            _subscribeHandlers[nsIdx] = h.Handler;
                            _notifySafeCircuits[nsIdx] = h.Circuit;
                            _notifySafeNames[nsIdx] = h.FullName;
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

            IdentifySpecializations();

            _subscriberMask = newMask;
            _syncMask = newSyncMask;
            _asyncMask = newAsyncMask;
            _parallelMask = newParallelMask;
            _notifyMask = newNotifyMask;
            _notifySafeMask = newSubscribeMask;
            PublishDispatchSnapshot(newMask, newSyncMask, newAsyncMask, newParallelMask, newNotifyMask,
                newSubscribeMask);
            Volatile.Write(ref _isDirty, 0);
        }

        private void PublishDispatchSnapshot(ulong subscriberMask, ulong syncMask, ulong asyncMask, ulong parallelMask,
                                             ulong notifyMask,     ulong notifySafeMask)
        {
            var syncHandlers = CopyPrefix(_syncHandlers, _syncCountTotal);
            var syncCircuits = CopyPrefix(_syncCircuits, _syncCountTotal);
            var syncNames = CopyPrefix(_syncNames, _syncCountTotal);
            var asyncHandlers = CopyPrefix(_asyncHandlers, _asyncCountTotal);
            var asyncCircuits = CopyPrefix(_asyncCircuits, _asyncCountTotal);
            var asyncNames = CopyPrefix(_asyncNames, _asyncCountTotal);
            var notifyHandlers = CopyPrefix(_notifyHandlers, _notifyCountTotal);
            var notifyCircuits = CopyPrefix(_notifyCircuits, _notifyCountTotal);
            var notifyNames = CopyPrefix(_notifyNames, _notifyCountTotal);
            var subscribeHandlers = CopyPrefix(_subscribeHandlers, _notifySafeCountTotal);
            var notifySafeCircuits = CopyPrefix(_notifySafeCircuits, _notifySafeCountTotal);
            var notifySafeNames = CopyPrefix(_notifySafeNames, _notifySafeCountTotal);
            var flatParallel = CopyPrefix(_flatParallel, _parallelCountTotal);

            Volatile.Write(ref _snapshot, new EventBucketSnapshot<T>(
                syncHandlers,
                syncCircuits,
                syncNames,
                asyncHandlers,
                asyncCircuits,
                asyncNames,
                notifyHandlers,
                notifyCircuits,
                notifyNames,
                subscribeHandlers,
                notifySafeCircuits,
                notifySafeNames,
                flatParallel,
                _syncCountTotal,
                _asyncCountTotal,
                _parallelCountTotal,
                _notifyCountTotal,
                _notifySafeCountTotal,
                subscriberMask,
                syncMask,
                asyncMask,
                parallelMask,
                notifyMask,
                notifySafeMask));
        }

        private static TElement[] CopyPrefix<TElement>(TElement[] source, int count)
        {
            if (count <= 0) return Array.Empty<TElement>();
            var copy = new TElement[count];
            Array.Copy(source, copy, count);
            return copy;
        }

        private void RentArrays(int totalSync, int totalAsync, int totalNotify, int totalSubscribe, int totalParallel)
        {
            if (_syncHandlers.Length < totalSync)
            {
                ReturnArraysForRebuild(true, false, false, false, false);
                _syncHandlers = ArrayPool<EventHandleDelegate<T>>.Shared.Rent(totalSync);
                _syncCircuits = ArrayPool<HandlerCircuit>.Shared.Rent(totalSync);
                _syncNames = ArrayPool<string>.Shared.Rent(totalSync);
            }

            if (_asyncHandlers.Length < totalAsync)
            {
                ReturnArraysForRebuild(false, true, false, false, false);
                _asyncHandlers = ArrayPool<EventHandleDelegateAsync<T>>.Shared.Rent(totalAsync);
                _asyncCircuits = ArrayPool<HandlerCircuit>.Shared.Rent(totalAsync);
                _asyncNames = ArrayPool<string>.Shared.Rent(totalAsync);
            }

            if (_notifyHandlers.Length < totalNotify)
            {
                ReturnArraysForRebuild(false, false, true, false, false);
                _notifyHandlers = ArrayPool<EventNotifyDelegate<T>>.Shared.Rent(totalNotify);
                _notifyCircuits = ArrayPool<HandlerCircuit>.Shared.Rent(totalNotify);
                _notifyNames = ArrayPool<string>.Shared.Rent(totalNotify);
            }

            if (_subscribeHandlers.Length < totalSubscribe)
            {
                ReturnArraysForRebuild(false, false, false, true, false);
                _subscribeHandlers = ArrayPool<EventNotifyDelegate<T>>.Shared.Rent(totalSubscribe);
                _notifySafeCircuits = ArrayPool<HandlerCircuit>.Shared.Rent(totalSubscribe);
                _notifySafeNames = ArrayPool<string>.Shared.Rent(totalSubscribe);
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
            Array.Clear(_syncCircuits, sIdx, _syncCircuits.Length - sIdx);
            Array.Clear(_syncNames, sIdx, _syncNames.Length - sIdx);
            Array.Clear(_asyncHandlers, aIdx, _asyncHandlers.Length - aIdx);
            Array.Clear(_asyncCircuits, aIdx, _asyncCircuits.Length - aIdx);
            Array.Clear(_asyncNames, aIdx, _asyncNames.Length - aIdx);
            Array.Clear(_notifyHandlers, nIdx, _notifyHandlers.Length - nIdx);
            Array.Clear(_notifyCircuits, nIdx, _notifyCircuits.Length - nIdx);
            Array.Clear(_notifyNames, nIdx, _notifyNames.Length - nIdx);
            Array.Clear(_subscribeHandlers, nsIdx, _subscribeHandlers.Length - nsIdx);
            Array.Clear(_notifySafeCircuits, nsIdx, _notifySafeCircuits.Length - nsIdx);
            Array.Clear(_notifySafeNames, nsIdx, _notifySafeNames.Length - nsIdx);
            Array.Clear(_flatParallel, pIdx, _flatParallel.Length - pIdx);
        }

        private void IdentifySpecializations()
        {
            _singleSyncHandler = null;
            _singleNotifyHandler = null;
            _singleSubscribeHandler = null;
            _isSingleSync = _syncCountTotal == 1 && _asyncCountTotal == 0 && _parallelCountTotal == 0 &&
                            _notifyCountTotal == 0 && _notifySafeCountTotal == 0;
            if (_isSingleSync)
            {
                _singleSyncHandler = _syncHandlers[0];
                _singleSyncCircuit = _syncCircuits[0];
                _singleSyncName = _syncNames[0];
            }

            _isSingleNotify = _notifyCountTotal == 1 && _asyncCountTotal == 0 && _parallelCountTotal == 0 &&
                              _syncCountTotal == 0 && _notifySafeCountTotal == 0;
            if (_isSingleNotify)
            {
                _singleNotifyHandler = _notifyHandlers[0];
                _singleNotifyCircuit = _notifyCircuits[0];
                _singleNotifyName = _notifyNames[0];
            }

            _isSingleNotifySafe = _notifySafeCountTotal == 1 && _asyncCountTotal == 0 && _parallelCountTotal == 0 &&
                                  _syncCountTotal == 0 && _notifyCountTotal == 0;
            if (_isSingleNotifySafe)
            {
                _singleSubscribeHandler = _subscribeHandlers[0];
                _singleSubscribeCircuit = _notifySafeCircuits[0];
                _singleSubscribeName = _notifySafeNames[0];
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

        public void AddParallel(int layerIndex, IEventHandler<T> h, Action<int, string, string, Exception> re)
        {
            GetOrCreate(layerIndex).AddParallel(h, re);
        }

        public void AddParallel(int layerIndex, EventNotifyDelegate<T> h, Action<int, string, string, Exception> re)
        {
            GetOrCreate(layerIndex).AddParallel(h, re);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventHandledState Dispatch(in T value)
        {
            var snapshot = EnsureClean();

            if (snapshot.IsSingleNotify) return DispatchSingleNotify(snapshot, in value);
            if (snapshot.IsSmallNotifyFanoutOnly && snapshot.NotifyCountTotal > 0)
            {
                DispatchSmallNotifyFanout(snapshot, 0, snapshot.NotifyCountTotal, in value);
                return EventHandledState.Continue;
            }

            if (snapshot.IsSingleNotifySafe) return DispatchSingleNotifySafe(snapshot, in value);
            if (snapshot.IsSingleSync) return DispatchSingleSync(snapshot, in value);

            if (snapshot.NotifyMask != 0) DispatchNotify(snapshot, 0, snapshot.NotifyCountTotal, in value);
            if (snapshot.NotifySafeMask != 0) DispatchNotifySafe(snapshot, 0, snapshot.NotifySafeCountTotal, in value);
            var res = EventHandledState.Continue;
            if (snapshot.SyncMask != 0)
            {
                res = DispatchSync(snapshot, 0, snapshot.SyncCountTotal, in value);
                if (res == EventHandledState.Handled) return res;
            }

            if (snapshot.AsyncMask != 0) DispatchAsync(snapshot, 0, snapshot.AsyncCountTotal, in value);
            if (snapshot.ParallelMask != 0)
                for (var j = 0; j < snapshot.ParallelCountTotal; j++)
                    snapshot.FlatParallel[j].Enqueue(-1, in value);

            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchSingleSync(EventBucketSnapshot<T> snapshot, in T value)
        {
            try
            {
                return snapshot.SingleSyncHandler!(in value);
            }
            catch (Exception ex)
            {
                HandleFault(snapshot, 0, 0, in value, ex);
                return EventHandledState.Continue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchSingleNotify(EventBucketSnapshot<T> snapshot, in T value)
        {
            snapshot.SingleNotifyHandler!(in value);
            return EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchSingleNotifySafe(EventBucketSnapshot<T> snapshot, in T value)
        {
            try
            {
                snapshot.SingleSubscribeHandler!(in value);
            }
            catch (Exception ex)
            {
                HandleFault(snapshot, 0, 2, in value, ex);
            }

            return EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchSmallNotifyFanout(EventBucketSnapshot<T> snapshot, int start, int count, in T value)
        {
            ref var hBase = ref GetArrayDataRef(snapshot.NotifyHandlers);
            Unsafe.Add(ref hBase, start)(in value);
            Unsafe.Add(ref hBase, start + 1)(in value);
            if (count == 2) return; Unsafe.Add(ref hBase, start + 2)(in value);
            if (count == 3) return; Unsafe.Add(ref hBase, start + 3)(in value);
            if (count == 4) return; Unsafe.Add(ref hBase, start + 4)(in value);
            if (count == 5) return; Unsafe.Add(ref hBase, start + 5)(in value);
            if (count == 6) return; Unsafe.Add(ref hBase, start + 6)(in value);
            if (count == 7) return; Unsafe.Add(ref hBase, start + 7)(in value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchNotify(EventBucketSnapshot<T> snapshot, int start, int end, in T e)
        {
            if (start >= end) return;
            ref var hBase = ref GetArrayDataRef(snapshot.NotifyHandlers);
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
        private void DispatchNotifySafe(EventBucketSnapshot<T> snapshot, int start, int end, in T value)
        {
            if (start >= end) return;
            ref var hBase = ref GetArrayDataRef(snapshot.SubscribeHandlers);
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
                HandleFault(snapshot, currentIndex, 2, in value, e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventHandledState DispatchSync(EventBucketSnapshot<T> snapshot, int start, int end, in T value)
        {
            if (start >= end) return EventHandledState.Continue;
            ref var hBase = ref GetArrayDataRef(snapshot.SyncHandlers);
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
                HandleFault(snapshot, currentIndex, 0, in value, e);
                return EventHandledState.Continue;
            }

            return (combinedState & 2) != 0 ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchAsync(EventBucketSnapshot<T> snapshot, int start, int end, in T value)
        {
            var hs = snapshot.AsyncHandlers;
            var i = start;
            try
            {
                for (; i < end; i++)
                    AsyncFaultContext<T>.Observe(this, snapshot.AsyncCircuits[i], snapshot.AsyncNames[i], in value, hs[i](value));
            }
            catch (Exception e)
            {
                HandleFault(snapshot, i, 1, in value, e);
            }
        }

        private void HandleFault(EventBucketSnapshot<T> snapshot, int index, int type, in T value, Exception e)
        {
            HandlerCircuit? circuit = null;
            string? name = null;
            if (type == 0)
            {
                if (index >= 0 && index < snapshot.SyncCountTotal)
                {
                    circuit = snapshot.SyncCircuits[index];
                    name = snapshot.SyncNames[index];
                }
            }
            else if (type == 1)
            {
                if (index >= 0 && index < snapshot.AsyncCountTotal)
                {
                    circuit = snapshot.AsyncCircuits[index];
                    name = snapshot.AsyncNames[index];
                }
            }
            else if (type == 2)
            {
                if (index >= 0 && index < snapshot.NotifySafeCountTotal)
                {
                    circuit = snapshot.NotifySafeCircuits[index];
                    name = snapshot.NotifySafeNames[index];
                }
            }

            EventMetaDataHandler.OnEventExpectation(value, e);
            if (circuit != null && circuit.TryDisable())
            {
                LayerHub.ReportLayerEventError(-1, name ?? "Unknown", typeof(T).Name, e);
                MarkDirty();
            }
        }

        private void ReturnArraysForRebuild(bool sync, bool async, bool notify, bool subscribe, bool parallel)
        {
            if (sync) ReturnArrayHelper(ref _syncHandlers, ref _syncCircuits, ref _syncNames);
            if (async) ReturnArrayHelper(ref _asyncHandlers, ref _asyncCircuits, ref _asyncNames);
            if (notify) ReturnArrayHelper(ref _notifyHandlers, ref _notifyCircuits, ref _notifyNames);
            if (subscribe) ReturnArrayHelper(ref _subscribeHandlers, ref _notifySafeCircuits, ref _notifySafeNames);
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
        private HandlerCircuit? _circuit;
        private string? _handlerFullName;
        private EventBucket<T>? _owner;
        private T _payload;
        private LBTask _task;

        private AsyncFaultContext()
        {
            _continuation = Complete;
        }

        public static void Observe(EventBucket<T> owner, HandlerCircuit circuit, string handlerFullName,
                                   in T           payload, LBTask task)
        {
            if (!s_pool.TryDequeue(out var context)) context = new AsyncFaultContext<T>();
            else Interlocked.Decrement(ref s_poolCount);
            context._owner = owner;
            context._circuit = circuit;
            context._handlerFullName = handlerFullName;
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
                EventMetaDataHandler.OnEventExpectation(_payload, ex);
                if (_circuit != null && _circuit.TryDisable())
                {
                    LayerHub.ReportLayerEventError(-1, _handlerFullName!, typeof(T).Name, ex);
                    _owner?.MarkDirty();
                }
            }
            finally
            {
                _owner = null;
                _circuit = null;
                _handlerFullName = null;
                _payload = default;
                _task = default;
                if (Interlocked.Increment(ref s_poolCount) <= MAX_POOL_SIZE) s_pool.Enqueue(this);
                else Interlocked.Decrement(ref s_poolCount);
            }
        }
    }
}
