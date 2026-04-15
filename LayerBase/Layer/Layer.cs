using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.PolledEventContainer;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.Delay;

namespace LayerBase.Layers
{
    /// <summary>
    /// Layer 基类，负责事件分发、DI 服务和延迟事件能力。
    /// </summary>
    public abstract class Layer : Node, IUpdate
    {
        private readonly EventDispatcher m_eventDispatcher;
        private readonly PooledEventContainer m_pooledEventContainer;
        private readonly List<IUpdate> m_serviceUpdates = new List<IUpdate>();
        private readonly List<IDelayPublisherUpdater> m_delayPublisherUpdates = new List<IDelayPublisherUpdater>();
        private readonly Dictionary<int, object> m_delayPublishers = new Dictionary<int, object>();

        // Layer 级 DI 容器配置与运行时 provider。
        private readonly ServiceCollection m_serviceCollection;
        private ServiceProvider? m_serviceProvider;
        private DirectEventBus? m_eventBus;

        protected Layer()
        {
            m_eventDispatcher = new EventDispatcher(GetType().Name);
            m_eventDispatcher.ErrorReporter = LayerBase.LayerHub.LayerHub.ReportLayerEventError;
            m_pooledEventContainer = new PooledEventContainer(this);
            m_serviceCollection = new ServiceCollection();
            LayerServiceRegistry.Apply(this);
        }

        internal int RouteIndex { get; private set; } = -1;

        public virtual void Update()
        {
        }

        /// <summary>
        /// 推进当前 Layer：事件容器、服务更新和 Layer 更新。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Pump()
        {
            m_pooledEventContainer.Pump();
            PumpServices();
            Update();
        }

        // -----------------DI-------------------
        public T GetService<T>()
        {
            var provider = Volatile.Read(ref m_serviceProvider);
            if (provider == null)
                throw new NullReferenceException("DI 容器尚未构建，请在 Build 完成后再调用 GetService。");
            return provider.Get<T>();
        }

        public void Dispose()
        {
            var provider = Interlocked.Exchange(ref m_serviceProvider, null);
            provider?.Dispose();
            DisposeDelayPublishers();
        }

        private void DisposeDelayPublishers()
        {
            if (m_delayPublisherUpdates.Count == 0)
            {
                return;
            }

            DelayPublisherManager.Instance.UnregisterRange(m_delayPublisherUpdates);
            for (int i = 0; i < m_delayPublisherUpdates.Count; i++)
            {
                m_delayPublisherUpdates[i].Reset();
            }

            m_delayPublisherUpdates.Clear();
            m_delayPublishers.Clear();
        }

        /// <summary>
        /// 注册服务模块并收集该模块声明的依赖。
        /// </summary>
        public void RegisterService(IService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            ServiceLayerBinder.Attach(service, this);
            service.ConfigureServices(m_serviceCollection);
            if (service is IUpdate updatable && !m_serviceUpdates.Contains(updatable))
            {
                m_serviceUpdates.Add(updatable);
            }
        }

        /// <summary>
        /// 构建 Layer 级 DI 容器。
        /// </summary>
        public void Build()
        {
            var newProvider = new ServiceProvider(m_serviceCollection.ToDescriptors(), this);
            var oldProvider = Interlocked.Exchange(ref m_serviceProvider, newProvider);
            oldProvider?.Dispose();
        }

        internal void SetEventBus(DirectEventBus eventBus, int routeIndex)
        {
            m_eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            RouteIndex = routeIndex;
        }

        private void InvalidateRoutes()
        {
            m_eventBus?.Invalidate();
        }

        // -----------------Events-------------------
        /// <summary>
        /// 订阅同步事件委托。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subscribe<Value>(EventHandleDelegate<Value> eventHandleDelegate) where Value : struct
        {
            m_eventDispatcher.Subscribe(eventHandleDelegate);
            InvalidateRoutes();
        }

        /// <summary>
        /// 订阅异步事件委托。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeAsync<Value>(EventHandleDelegateAsync<Value> eventHandleDelegateAsync) where Value : struct
        {
            m_eventDispatcher.SubscribeAsync(eventHandleDelegateAsync);
            InvalidateRoutes();
        }

        /// <summary>
        /// 订阅同步事件处理器实例。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subscribe<Value>(IEventHandler<Value> eventHandler) where Value : struct
        {
            m_eventDispatcher.Subscribe(eventHandler);
            InvalidateRoutes();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeParallel<Value>(IEventHandler<Value> eventHandler) where Value : struct
        {
            m_eventDispatcher.SubscribeParallel(eventHandler);
            InvalidateRoutes();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeParallel<Value>(EventHandleDelegate<Value> eventHandleDelegate) where Value : struct
        {
            m_eventDispatcher.SubscribeParallel(eventHandleDelegate);
            InvalidateRoutes();
        }

        /// <summary>
        /// 订阅异步事件处理器实例。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeAsync<Value>(IEventHandlerAsync<Value> eventHandler) where Value : struct
        {
            m_eventDispatcher.SubscribeAsync(eventHandler);
            InvalidateRoutes();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EventHandledState Dispatch<Value>(in Event<Value> @event) where Value : struct
        {
            return m_eventDispatcher.Dispatch(@event);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool HasHandlers<Value>() where Value : struct
        {
            return m_eventDispatcher.HasHandlers<Value>();
        }

        internal void EnqueueEvent<Value>(in Event<Value> @event) where Value : struct
        {
            m_pooledEventContainer.Post(@event);
        }

        internal void NotifyQueuedEventProcessed<Value>(
            in Event<Value> @event,
            EventHandledState handledState) where Value : struct
        {
            if (handledState == EventHandledState.Handled)
            {
                return;
            }

            m_eventBus?.PostContinuation(this, in @event);
        }

        public void Post<Value>(in Value value) where Value : struct
        {
            Event<Value> @event = new Event<Value>(value);
            @event.MarkBroadCast();
            PostInternal(in @event);
        }

        public void PostDrop<Value>(in Value value) where Value : struct
        {
            Event<Value> @event = new Event<Value>(value);
            @event.MarkDrop();
            PostInternal(in @event);
        }

        public void PostBubble<Value>(in Value value) where Value : struct
        {
            Event<Value> @event = new Event<Value>(value);
            @event.MarkBubble();
            PostInternal(in @event);
        }

        private void PostInternal<Value>(in Event<Value> @event) where Value : struct
        {
            if (!@event.IsVaild()) return;

            if (m_eventBus != null)
            {
                m_eventBus.PostLocal(this, in @event);
                return;
            }

            EnqueueEvent(in @event);
        }

        /// <summary>
        /// 向上冒泡事件。
        /// </summary>
        public void Bubble<Value>(in Value value) where Value : struct
        {
            Event<Value> @event = new Event<Value>(value);
            @event.MarkBubble();
            PublishInternal(in @event);
        }

        /// <summary>
        /// 向下下沉事件。
        /// </summary>
        public void Drop<Value>(in Value value) where Value : struct
        {
            Event<Value> @event = new Event<Value>(value);
            @event.MarkDrop();
            PublishInternal(in @event);
        }

        /// <summary>
        /// 广播事件到当前 Layer、上层和下层。
        /// </summary>
        public void BroadCast<Value>(in Value value) where Value : struct
        {
            Event<Value> @event = new Event<Value>(value);
            @event.MarkBroadCast();
            PublishInternal(in @event);
        }

        private EventHandledState PublishInternal<Value>(in Event<Value> @event) where Value : struct
        {
            if (!@event.IsVaild())
            {
                return EventHandledState.Handled;
            }

            return m_eventBus != null
                ? m_eventBus.Publish(this, in @event)
                : Dispatch(in @event);
        }

        // -----------------Delay Events-------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDelayPublisher<T> SubscribeDelay<T>() where T : struct
        {
            return GetOrCreateDelayPublisher<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Delay<T>(in T value, float ttlSeconds, int contractLayer = 0) where T : struct
        {
            PublishDelayLocal(in value, ttlSeconds, DelayDirection.None, contractLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BroadCastDelay<T>(in T value, float ttlSeconds, int contractLayer = 0) where T : struct
        {
            PublishDelayLocal(in value, ttlSeconds, DelayDirection.BroadCast, contractLayer);
            PublishDelayToHigherLayers(in value, ttlSeconds, DelayDirection.BroadCast, contractLayer);
            PublishDelayToLowerLayers(in value, ttlSeconds, DelayDirection.BroadCast, contractLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BubbleDelay<T>(in T value, float ttlSeconds, int contractLayer = 0) where T : struct
        {
            PublishDelayLocal(in value, ttlSeconds, DelayDirection.Bubble, contractLayer);
            PublishDelayToHigherLayers(in value, ttlSeconds, DelayDirection.Bubble, contractLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DropDelay<T>(in T value, float ttlSeconds, int contractLayer = 0) where T : struct
        {
            PublishDelayLocal(in value, ttlSeconds, DelayDirection.Drop, contractLayer);
            PublishDelayToLowerLayers(in value, ttlSeconds, DelayDirection.Drop, contractLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PublishDelayLocal<T>(
            in T value,
            float ttlSeconds,
            DelayDirection direction,
            int contractLayer) where T : struct
        {
            var publisher = GetOrCreateDelayPublisher<T>();
            publisher.Publish(in value, ttlSeconds, direction, contractLayer);
            DelayPublisherManager.Instance.NotifyPublished(this, contractLayer, publisher);
        }

        private void PublishDelayToHigherLayers<T>(
            in T value,
            float ttlSeconds,
            DelayDirection direction,
            int contractLayer) where T : struct
        {
            Layer? layer = Previous as Layer;
            while (layer != null && !ReferenceEquals(layer, this))
            {
                layer.PublishDelayLocal(in value, ttlSeconds, direction, contractLayer);
                layer = layer.Previous as Layer;
            }
        }

        private void PublishDelayToLowerLayers<T>(
            in T value,
            float ttlSeconds,
            DelayDirection direction,
            int contractLayer) where T : struct
        {
            Layer? layer = NextNode as Layer;
            while (layer != null && !ReferenceEquals(layer, this))
            {
                layer.PublishDelayLocal(in value, ttlSeconds, direction, contractLayer);
                layer = layer.NextNode as Layer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DelayPublisher<T> GetOrCreateDelayPublisher<T>() where T : struct
        {
            int typeId = EventTypeId<T>.Id;
            if (!m_delayPublishers.TryGetValue(typeId, out var publisherObj))
            {
                var publisher = new DelayPublisher<T>(this);
                m_delayPublishers.Add(typeId, publisher);
                m_delayPublisherUpdates.Add(publisher);
                DelayPublisherManager.Instance.Register(publisher);
                return publisher;
            }

            return (DelayPublisher<T>)publisherObj;
        }

        private void PumpServices()
        {
            for (int i = 0; i < m_serviceUpdates.Count; i++)
            {
                m_serviceUpdates[i].Update();
            }
        }
    }
}
