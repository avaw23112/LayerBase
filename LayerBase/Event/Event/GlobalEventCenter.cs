using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.UnmanagedList;
using LayerBase.Layers;

namespace LayerBase.Core.Event
{
    public enum Propagation
    {
        Global,
        Bubble,
        Drop
    }

    internal sealed class GlobalEventCenter
    {
        private readonly ConcurrentDictionary<int, IEventBucket> _eventBuckets = new();
        private IEventQueue[] _layerSlots = Array.Empty<IEventQueue>();
        private readonly object _lock = new();

        private string[] _layerNames = Array.Empty<string>();
        
        // 预计算的位图掩码，用于极致优化 Post 速度
        private ulong[] _bubbleMasks = Array.Empty<ulong>();
        private ulong[] _dropMasks = Array.Empty<ulong>();

        internal void EnsureSlots(int count, string name)
        {
            if (_layerSlots.Length < count || (count > 0 && _layerNames.Length < count))
            {
                lock (_lock)
                {
                    if (_layerSlots.Length < count)
                    {
                        var newSlots = new IEventQueue[count];
                        Array.Copy(_layerSlots, newSlots, _layerSlots.Length);
                        for (int i = _layerSlots.Length; i < count; i++)
                        {
                            newSlots[i] = new LayerEventQueue(this, i);
                        }
                        _layerSlots = newSlots;

                        // 预计算掩码：Bubble(0->i), Drop(i->N)
                        var newBubble = new ulong[count];
                        var newDrop = new ulong[count];
                        for (int i = 0; i < count; i++)
                        {
                            newBubble[i] = (1UL << (i + 1)) - 1;
                            newDrop[i] = ~((1UL << i) - 1);
                        }
                        _bubbleMasks = newBubble;
                        _dropMasks = newDrop;
                    }

                    if (_layerNames.Length < count)
                    {
                        var newNames = new string[count];
                        Array.Copy(_layerNames, newNames, _layerNames.Length);
                        for (int i = _layerNames.Length; i < count; i++)
                        {
                            newNames[i] = "UnknownLayer";
                        }
                        _layerNames = newNames;
                    }
                }
            }
            if (count > 0) _layerNames[count - 1] = name;
        }

        internal void Subscribe<T>(int layerIndex, IEventHandler<T> handler) where T : struct
            => GetBucket<T>().Add(layerIndex, handler);

        internal void SubscribeAsync<T>(int layerIndex, IEventHandlerAsync<T> handler) where T : struct
            => GetBucket<T>().Add(layerIndex, handler);

        internal void SubscribeParallel<T>(int layerIndex, IEventHandler<T> handler, Action<string, string, string, Exception> reportError) where T : struct
            => GetBucket<T>().AddParallel(layerIndex, handler, reportError);

        internal void Subscribe<T>(int layerIndex, EventHandleDelegate<T> handleDelegate) where T : struct
            => GetBucket<T>().Add(layerIndex, handleDelegate);

        internal void SubscribeAsync<T>(int layerIndex, EventHandleDelegateAsync<T> handleDelegate) where T : struct
            => GetBucket<T>().Add(layerIndex, handleDelegate);

        internal void SubscribeParallel<T>(int layerIndex, EventHandleDelegate<T> handleDelegate, Action<string, string, string, Exception> reportError) where T : struct
            => GetBucket<T>().AddParallel(layerIndex, handleDelegate, reportError);

        internal EventHandledState Send<T>(in T value, int sourceIndex, Propagation propagation) where T : struct
        {
            return GetBucket<T>().Dispatch(value, sourceIndex, propagation);
        }

        internal EventHandledState SendLocal<T>(int layerIndex, in T value) where T : struct
        {
            return GetBucket<T>().DispatchLocal(layerIndex, new Event<T>(value));
        }

        internal void Post<T>(in T value, int sourceIndex, Propagation propagation) where T : struct
        {
            GetBucket<T>().Post(value, sourceIndex, propagation);
        }

        internal void PostLocal<T>(int layerIndex, in T value) where T : struct
        {
            GetBucket<T>().PostLocal(layerIndex, value);
        }

        internal void EnqueueToLayer<T>(int layerIndex, in T value) where T : struct
        {
            if (layerIndex >= 0 && layerIndex < _layerSlots.Length)
            {
                _layerSlots[layerIndex].Enqueue(value);
            }
        }

        internal void EnqueueEventInternal<T>(int layerIndex, in Event<T> @event) where T : struct
        {
            if (layerIndex >= 0 && layerIndex < _layerSlots.Length)
            {
                _layerSlots[layerIndex].EnqueueEvent(@event);
            }
        }

        internal void PumpLayer(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < _layerSlots.Length)
            {
                _layerSlots[layerIndex].Pump();
            }
        }

        internal EventHandledState DispatchLocal<T>(int layerIndex, in Event<T> @event) where T : struct
        {
            return GetBucket<T>().DispatchLocal(layerIndex, in @event);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int FindFirstBit(ulong mask)
        {
            if (mask == 0) return -1;
#if NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER || NET8_0_OR_GREATER
            return System.Numerics.BitOperations.TrailingZeroCount(mask);
#else
            // 高性能 De Bruijn 序列回退算法 (netstandard2.1)
            return TrailingZeroCountFallback(mask);
#endif
        }

#if !NETCOREAPP3_0_OR_GREATER && !NET5_0_OR_GREATER && !NET8_0_OR_GREATER
        private static readonly byte[] DeBruijnTable = {
            0, 1, 56, 2, 57, 49, 28, 3, 61, 58, 42, 50, 38, 29, 17, 4,
            62, 47, 59, 36, 45, 43, 51, 22, 53, 39, 33, 30, 24, 18, 12, 5,
            63, 55, 48, 27, 60, 41, 37, 16, 46, 35, 44, 21, 52, 32, 23, 11,
            54, 26, 40, 15, 34, 20, 31, 10, 25, 14, 19, 9, 13, 8, 7, 6
        };

        private static int TrailingZeroCountFallback(ulong v)
        {
            return DeBruijnTable[((ulong)((long)v & -(long)v) * 0x03F79D71B4CB0A89UL) >> 58];
        }
#endif

        internal void Reset()
        {
            _eventBuckets.Clear();
            _layerSlots = Array.Empty<IEventQueue>();
            _layerNames = Array.Empty<string>();
            _bubbleMasks = Array.Empty<ulong>();
            _dropMasks = Array.Empty<ulong>();
        }

        private EventBucket<T> GetBucket<T>() where T : struct
        {
            var bucket = BucketCache<T>.Instance;
            if (bucket != null && bucket.Owner == this) return bucket;

            int typeId = EventTypeId<T>.Id;
            bucket = (EventBucket<T>)_eventBuckets.GetOrAdd(typeId, _ => new EventBucket<T>(this));
            BucketCache<T>.Instance = bucket;
            return bucket;
        }

        private static class BucketCache<T> where T : struct
        {
            public static EventBucket<T>? Instance;
        }

        private interface IEventBucket { }

        private sealed class EventBucket<T> : IEventBucket where T : struct
        {
            public readonly GlobalEventCenter Owner;
            private HandlerBucket<T>?[] _buckets = Array.Empty<HandlerBucket<T>>();
            private ulong _subscriberMask;
            private readonly object _lock = new();

            public EventBucket(GlobalEventCenter center)
            {
                Owner = center;
            }

            public void Add(int layerIndex, IEventHandler<T> handler)
            {
                GetOrCreateHandlerBucket(layerIndex).Add(handler);
                UpdateMask(layerIndex);
            }

            public void Add(int layerIndex, IEventHandlerAsync<T> handler)
            {
                GetOrCreateHandlerBucket(layerIndex).Add(handler);
                UpdateMask(layerIndex);
            }

            public void AddParallel(int layerIndex, IEventHandler<T> handler, Action<string, string, string, Exception> reportError)
            {
                GetOrCreateHandlerBucket(layerIndex).AddParallel(handler, reportError);
                UpdateMask(layerIndex);
            }

            public void Add(int layerIndex, EventHandleDelegate<T> handleDelegate)
            {
                GetOrCreateHandlerBucket(layerIndex).Add(handleDelegate);
                UpdateMask(layerIndex);
            }

            public void Add(int layerIndex, EventHandleDelegateAsync<T> handleDelegate)
            {
                GetOrCreateHandlerBucket(layerIndex).Add(handleDelegate);
                UpdateMask(layerIndex);
            }

            public void AddParallel(int layerIndex, EventHandleDelegate<T> handleDelegate, Action<string, string, string, Exception> reportError)
            {
                GetOrCreateHandlerBucket(layerIndex).AddParallel(handleDelegate, reportError);
                UpdateMask(layerIndex);
            }

            private HandlerBucket<T> GetOrCreateHandlerBucket(int layerIndex)
            {
                if (layerIndex >= _buckets.Length)
                {
                    lock (_lock)
                    {
                        if (layerIndex >= _buckets.Length)
                        {
                            var newBuckets = new HandlerBucket<T>?[Math.Max(layerIndex + 1, _buckets.Length * 2)];
                            Array.Copy(_buckets, newBuckets, _buckets.Length);
                            Volatile.Write(ref _buckets, newBuckets);
                        }
                    }
                }

                var bucket = _buckets[layerIndex];
                if (bucket == null)
                {
                    lock (_lock)
                    {
                        bucket = _buckets[layerIndex];
                        if (bucket == null)
                        {
                            bucket = new HandlerBucket<T>();
                            _buckets[layerIndex] = bucket;
                        }
                    }
                }
                return bucket;
            }

            private void UpdateMask(int layerIndex)
            {
                if (layerIndex < 64)
                {
                    lock (_lock)
                    {
                        _subscriberMask |= (1UL << layerIndex);
                    }
                }
            }

            public EventHandledState Dispatch(in T value, int sourceIndex, Propagation propagation)
            {
                var buckets = Volatile.Read(ref _buckets);
                if (buckets.Length == 0) return EventHandledState.Continue;

                int start, end, step;
                GetRange(sourceIndex, propagation, buckets.Length, out start, out end, out step);

                bool handledAndContinueSeen = false;
                var @event = new Event<T>(value);
                
                for (int i = start; i <= end; i++)
                {
                    if (i >= buckets.Length) break;
                    var bucket = buckets[i];
                    if (bucket == null) continue;

                    string layerName = i < Owner._layerNames.Length ? Owner._layerNames[i] : "UnknownLayer";
                    var state = bucket.Dispatch(layerName, in @event);
                    if (state == EventHandledState.Handled) return EventHandledState.Handled;
                    if (state == EventHandledState.HandledAndContinue) handledAndContinueSeen = true;
                }

                return handledAndContinueSeen ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
            }

            public void Post(in T value, int sourceIndex, Propagation propagation)
            {
                ulong mask = Volatile.Read(ref _subscriberMask);
                if (mask == 0) return;

                // 极致优化：利用预计算掩码，跳过运行时范围计算
                ulong targetMask = mask;
                switch (propagation)
                {
                    case Propagation.Bubble:
                        if (sourceIndex < Owner._bubbleMasks.Length)
                            targetMask &= Owner._bubbleMasks[sourceIndex];
                        break;
                    case Propagation.Drop:
                        if (sourceIndex < Owner._dropMasks.Length)
                            targetMask &= Owner._dropMasks[sourceIndex];
                        break;
                }

                if (targetMask != 0)
                {
                    int firstLayer = Owner.FindFirstBit(targetMask);
                    var @event = new Event<T>(value);
                    @event.TargetMask = targetMask;
                    Owner.EnqueueEventInternal(firstLayer, in @event);
                }
            }

            public void PostLocal(int layerIndex, in T value)
            {
                ulong mask = Volatile.Read(ref _subscriberMask);
                if ((mask & (1UL << layerIndex)) != 0)
                {
                    var @event = new Event<T>(value);
                    @event.TargetMask = (1UL << layerIndex);
                    Owner.EnqueueEventInternal(layerIndex, in @event);
                }
            }

            public EventHandledState DispatchLocal(int layerIndex, in Event<T> @event)
            {
                var buckets = Volatile.Read(ref _buckets);
                if (layerIndex >= 0 && layerIndex < buckets.Length)
                {
                    var bucket = buckets[layerIndex];
                    if (bucket != null)
                    {
                        string layerName = layerIndex < Owner._layerNames.Length ? Owner._layerNames[layerIndex] : "UnknownLayer";
                        return bucket.Dispatch(layerName, in @event);
                    }
                }
                return EventHandledState.Continue;
            }

            private void GetRange(int sourceIndex, Propagation propagation, int max, out int start, out int end, out int step)
            {
                // 注意：在统一线性架构下，step 始终为 1
                step = 1;
                switch (propagation)
                {
                    case Propagation.Bubble:
                        start = 0;
                        end = sourceIndex;
                        break;
                    case Propagation.Drop:
                        start = sourceIndex;
                        end = max - 1;
                        break;
                    case Propagation.Global:
                    default:
                        start = 0;
                        end = max - 1;
                        break;
                }
            }
        }

        private interface IEventQueue
        {
            void Enqueue<T>(in T value) where T : struct;
            void EnqueueEvent<T>(in Event<T> @event) where T : struct;
            void Pump();
        }

        private sealed class LayerEventQueue : IEventQueue
        {
            private readonly GlobalEventCenter _center;
            private readonly int _layerIndex;
            private readonly ConcurrentDictionary<int, IUnmanagedList> _queuesByType = new();

            public LayerEventQueue(GlobalEventCenter center, int layerIndex)
            {
                _center = center;
                _layerIndex = layerIndex;
            }

            public void Enqueue<T>(in T value) where T : struct
            {
                int typeId = EventTypeId<T>.Id;
                if (!_queuesByType.TryGetValue(typeId, out var list))
                {
                    list = _queuesByType.GetOrAdd(typeId, _ => new UnmanagedList<T>(_center, _layerIndex));
                }
                ((UnmanagedList<T>)list).Post(new Event<T>(value));
            }

            public void EnqueueEvent<T>(in Event<T> @event) where T : struct
            {
                int typeId = EventTypeId<T>.Id;
                if (!_queuesByType.TryGetValue(typeId, out var list))
                {
                    list = _queuesByType.GetOrAdd(typeId, _ => new UnmanagedList<T>(_center, _layerIndex));
                }
                ((UnmanagedList<T>)list).Post(@event);
            }

            public void Pump()
            {
                if (_queuesByType.Count == 0) return;

                foreach (var list in _queuesByType.Values)
                {
                    list.Pump();
                }
            }
        }
    }
}
