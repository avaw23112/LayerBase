using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.Delay;
using LayerBase.LayerHub;

namespace LayerBase.Layers
{
    /// <summary>
    /// Layer 基类，负责事件订阅、DI 服务和延迟事件能力。
    /// </summary>
    public abstract class Layer : Node, IUpdate
    {
        private readonly List<IUpdate> m_serviceUpdates = new List<IUpdate>();
        private readonly List<IDelayPublisherUpdater> m_delayPublisherUpdates = new List<IDelayPublisherUpdater>();
        private readonly Dictionary<int, object> m_delayPublishers = new Dictionary<int, object>();
        private List<Action>? m_pendingSubscriptions;

        // Layer 级 DI 容器配置与运行时 provider。
        private readonly ServiceCollection m_serviceCollection;
        private ServiceProvider? m_serviceProvider;

        protected Layer()
        {
            m_serviceCollection = new ServiceCollection();
            LayerServiceRegistry.Apply(this);
        }

        public int RouteIndex { get; private set; } = -1;

        /// <summary>
        /// 标记该层是否有活跃的逻辑更新（Service Update 或 Delay Publisher）。
        /// 用于 Pump 阶段的位图屏蔽优化。
        /// </summary>
        public virtual bool HasActiveLogic => m_serviceUpdates.Count > 0 || m_delayPublishers.Count > 0;

        public virtual void Update()
        {
        }

        /// <summary>
        /// 推进当前 Layer：事件槽位、服务更新和 Layer 更新。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Pump()
        {
            LayerHub.LayerHub.EventCenter.PumpLayer(RouteIndex);
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

        public List<IAutoSubscribe> DiscoveredSubscribers { get; private set; } = new List<IAutoSubscribe>();

        /// <summary>
        /// 构建 Layer 级 DI 容器，并激活所有自动订阅。
        /// </summary>
        public void Build()
        {
            var descriptors = m_serviceCollection.ToDescriptors();
            var newProvider = new ServiceProvider(descriptors, this);
            var oldProvider = Interlocked.Exchange(ref m_serviceProvider, newProvider);
            oldProvider?.Dispose();

            // 1. 严格按注册顺序触发所有 Manager 的自动绑定
            DiscoveredSubscribers = newProvider.InitializeAutoSubscriptions(this, descriptors);

            // 2. 扫描并缓存所有实现了 IUpdate 的服务实例，用于后续 Pump
            m_serviceUpdates.Clear();
            foreach (var desc in descriptors)
            {
                var instance = newProvider.GetService(desc.ServiceType);
                if (instance is IUpdate updatable)
                {
                    m_serviceUpdates.Add(updatable);
                }
            }
        }

        internal void SetRouteIndex(int routeIndex)
        {
            RouteIndex = routeIndex;
            if (m_pendingSubscriptions != null)
            {
                foreach (var sub in m_pendingSubscriptions)
                {
                    sub();
                }
                m_pendingSubscriptions = null;
            }
        }

        // -----------------Events Subscription-------------------
        private void RegisterOrDelay(Action sub)
        {
            if (RouteIndex == -1)
            {
                m_pendingSubscriptions ??= new List<Action>();
                m_pendingSubscriptions.Add(sub);
            }
            else
            {
                sub();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subscribe<Value>(EventHandleDelegate<Value> eventHandleDelegate) where Value : struct
        {
            RegisterOrDelay(() => LayerHub.LayerHub.EventCenter.Subscribe<Value>(RouteIndex, eventHandleDelegate));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeAsync<Value>(EventHandleDelegateAsync<Value> eventHandleDelegateAsync) where Value : struct
        {
            RegisterOrDelay(() => LayerHub.LayerHub.EventCenter.SubscribeAsync<Value>(RouteIndex, eventHandleDelegateAsync));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subscribe<Value>(IEventHandler<Value> eventHandler) where Value : struct
        {
            RegisterOrDelay(() => LayerHub.LayerHub.EventCenter.Subscribe<Value>(RouteIndex, eventHandler));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeParallel<Value>(IEventHandler<Value> eventHandler) where Value : struct
        {
            RegisterOrDelay(() => LayerBase.LayerHub.LayerHub.EventCenter.SubscribeParallel<Value>(RouteIndex, eventHandler, LayerBase.LayerHub.LayerHub.ReportLayerEventError));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeParallel<Value>(EventHandleDelegate<Value> eventHandleDelegate) where Value : struct
        {
            RegisterOrDelay(() => LayerBase.LayerHub.LayerHub.EventCenter.SubscribeParallel<Value>(RouteIndex, eventHandleDelegate, LayerBase.LayerHub.LayerHub.ReportLayerEventError));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeAsync<Value>(IEventHandlerAsync<Value> eventHandler) where Value : struct
        {
            RegisterOrDelay(() => LayerHub.LayerHub.EventCenter.SubscribeAsync<Value>(RouteIndex, eventHandler));
        }

        // -----------------Events Dispatch (Synchronous)-------------------

        public EventHandledState SendLocal<Value>(in Value value) where Value : struct
        {
            return LayerHub.LayerHub.EventCenter.SendLocal(RouteIndex, value);
        }

        public void SendBubble<Value>(in Value value) where Value : struct
        {
            LayerHub.LayerHub.EventCenter.Send(value, RouteIndex, Propagation.Bubble);
        }

        public void SendDrop<Value>(in Value value) where Value : struct
        {
            LayerHub.LayerHub.EventCenter.Send(value, RouteIndex, Propagation.Drop);
        }

        public void SendGlobal<Value>(in Value value) where Value : struct
        {
            LayerHub.LayerHub.EventCenter.Send(value, 0, Propagation.Global);
        }

        // -----------------Events Dispatch (Asynchronous)-------------------

        public void PostLocal<Value>(in Value value) where Value : struct
        {
            LayerHub.LayerHub.EventCenter.PostLocal(RouteIndex, value);
        }

        public void PostBubble<Value>(in Value value) where Value : struct
        {
            LayerHub.LayerHub.EventCenter.Post(value, RouteIndex, Propagation.Bubble);
        }

        public void PostDrop<Value>(in Value value) where Value : struct
        {
            LayerHub.LayerHub.EventCenter.Post(value, RouteIndex, Propagation.Drop);
        }

        public void PostGlobal<Value>(in Value value) where Value : struct
        {
            LayerHub.LayerHub.EventCenter.Post(value, 0, Propagation.Global);
        }

        // -----------------Delay Events-------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDelayPublisher<T> SubscribeDelay<T>() where T : struct
        {
            return GetOrCreateDelayPublisher<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelayLocal<T>(in T value, float ttlSeconds, int contractLayer = 0) where T : struct
        {
            PublishDelayLocal(in value, ttlSeconds, DelayDirection.None, contractLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelayGlobal<T>(in T value, float ttlSeconds, int contractLayer = 0) where T : struct
        {
            PublishDelayLocal(in value, ttlSeconds, DelayDirection.BroadCast, contractLayer);
            PublishDelayToHigherLayers(in value, ttlSeconds, DelayDirection.BroadCast, contractLayer);
            PublishDelayToLowerLayers(in value, ttlSeconds, DelayDirection.BroadCast, contractLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelayBubble<T>(in T value, float ttlSeconds, int contractLayer = 0) where T : struct
        {
            PublishDelayLocal(in value, ttlSeconds, DelayDirection.Bubble, contractLayer);
            PublishDelayToHigherLayers(in value, ttlSeconds, DelayDirection.Bubble, contractLayer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelayDrop<T>(in T value, float ttlSeconds, int contractLayer = 0) where T : struct
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
