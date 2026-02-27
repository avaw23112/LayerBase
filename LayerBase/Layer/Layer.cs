using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.EventStateTrace;
using LayerBase.Core.PolledEventContainer;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.Event.Delay;
using LayerBase.Layers.LayerMetaData;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Layers
{
	/// <summary>
	/// Layer 基类，负责事件分发、DI 服务和延迟事件能力。
	/// </summary>
	public abstract class Layer : Node,IUpdate
	{
		private EventDispatcher m_eventDispatcher;
		private PooledEventContainer m_pooledEventContainer;
		internal EventStateTracer? m_eventStateTracer;
		private EventLogTracer? m_eventLogTracer;
		private readonly List<IUpdate> m_serviceUpdates = new List<IUpdate>();
		private readonly List<IDelayPublisherUpdater> m_delayPublisherUpdates = new List<IDelayPublisherUpdater>();
		private readonly Dictionary<int, object> m_delayPublishers = new Dictionary<int, object>();
		
		// Layer 级 DI 容器配置与运行时 provider。
		private readonly ServiceCollection m_serviceCollection;
		private ServiceProvider? m_serviceProvider;
		
		protected Layer() 
		{
			m_eventDispatcher = new EventDispatcher(this.GetType().Name);
			m_eventDispatcher.ErrorReporter = LayerBase.LayerHub.LayerHub.ReportLayerEventError;
			m_pooledEventContainer = new PooledEventContainer(this);
			m_serviceCollection = new ServiceCollection();
			LayerServiceRegistry.Apply(this);
		}

		public virtual void Update()
		{
			
		}
		
		/// <summary>
		/// 推进当前 Layer：事件容器、追踪器、服务更新和 Layer 更新。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Pump()
		{
			m_pooledEventContainer.Pump();
			m_eventStateTracer?.Pump();
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
		/// <param name="service"></param>
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
		
		// -----------------Tracing-------------------
		
		internal void SetEventTracer(EventStateTracer stateTracer)
		{
			if (stateTracer == null)
			{
				throw new ArgumentNullException(nameof(stateTracer));
			}
			m_eventStateTracer = stateTracer;
			m_eventDispatcher.StateTracer = stateTracer;
		}

		internal void SetEventLogTracer(EventLogTracer logTracer)
		{
			if (logTracer == null)
			{
				throw new ArgumentNullException(nameof(logTracer));
			}
			if (m_eventStateTracer == null)
			{
				throw new InvalidOperationException("请先设置 EventStateTracer，再设置 EventLogTracer。");
			}
			
			m_eventLogTracer = logTracer;
			m_eventDispatcher.LogTracer = logTracer;
			m_eventStateTracer.OnEventCompleted = (ref EventState state) => m_eventLogTracer.Pump(ref state);
		}
		
		public bool TryExportTracing(EventStateToken est,out string log)
		{
			if (m_eventLogTracer == null || m_eventStateTracer==null)
			{
				log = string.Empty;
				return false;
			}
			ref var eventState = ref m_eventStateTracer.Resolve(est);
			return m_eventLogTracer.TryExport(eventState,out log);
		}
		
		// -----------------Tracing-------------------
		
		/// <summary>
		/// 订阅同步事件委托。
		/// </summary>
		/// <typeparam name="Value"></typeparam>
		/// <param name="eventHandleDelegate"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Subscribe<Value>(EventHandleDelegate<Value> eventHandleDelegate) where Value : struct
		{
			m_eventDispatcher.Subscribe(eventHandleDelegate);
		}

		/// <summary>
		/// 订阅异步事件委托。
		/// </summary>
		/// <typeparam name="Value"></typeparam>
		/// <param name="eventHandleDelegateAsync"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubscribeAsync<Value>(EventHandleDelegateAsync<Value> eventHandleDelegateAsync) where Value : struct
		{
			m_eventDispatcher.SubscribeAsync(eventHandleDelegateAsync);
		}
		
		/// <summary>
		/// 订阅同步事件处理器实例。
		/// </summary>
		/// <param name="eventHandler"></param>
		/// <typeparam name="Value"></typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Subscribe<Value>(IEventHandler<Value> eventHandler) where Value : struct
		{
			m_eventDispatcher.Subscribe(eventHandler);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubscribeParallel<Value>(IEventHandler<Value> eventHandler) where Value : struct
		{
			m_eventDispatcher.SubscribeParallel(eventHandler);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubscribeParallel<Value>(EventHandleDelegate<Value> eventHandleDelegate) where Value : struct
		{
			m_eventDispatcher.SubscribeParallel(eventHandleDelegate);
		}

		/// <summary>
		/// 订阅异步事件处理器实例。
		/// </summary>
		/// <param name="eventHandler"></param>
		/// <typeparam name="Value"></typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubscribeAsync<Value>(IEventHandlerAsync<Value> eventHandler) where Value : struct
		{
			m_eventDispatcher.SubscribeAsync(eventHandler);
		}
		
		// --------------------------Buffer Events-------------------
		internal void PostEventToDoubleSide<Value>(in Event<Value> @event) where Value : struct
		{
			PostEventToHigherLayer(in @event);
			PostEventToLowerLayer(in @event);
		}
		internal void PostEventToHigherLayer<Value>(in Event<Value> @event) where Value : struct
		{
			if (!@event.IsVaild()) return;
			Layer preLayer = Prev as Layer;
			if (preLayer != null)
			{
				m_eventStateTracer?.TryIncrementPending(@event.TraceToken);
			}
			preLayer?.m_pooledEventContainer.Post(@event);
		}
		internal void PostEventToLowerLayer<Value>(in Event<Value> @event) where Value : struct
		{
			if (!@event.IsVaild()) return;
			Layer next = Next as Layer;
			if (next != null)
			{
				m_eventStateTracer?.TryIncrementPending(@event.TraceToken);
			}
			next?.m_pooledEventContainer.Post(@event);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal EventHandledState Dispatch<Value>(in Event<Value> @event) where Value : struct
		{
			if (m_eventStateTracer == null)
			{
				return m_eventDispatcher.Dispatch(@event);
			}
			
			ref EventState eventState = ref m_eventStateTracer.Resolve(@event.TraceToken);
			m_eventLogTracer?.TryBeginLayer(ref eventState, GetType().Name);
			return m_eventDispatcher.Dispatch(@event, ref eventState);
		}
		
		public void Post<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkBroadCast();
			TryAttachTrace(ref @event);
			if (!@event.IsVaild()) return;
			m_pooledEventContainer.Post(@event);
		}

		public void PostDrop<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkDrop();
			TryAttachTrace(ref @event);
			if (!@event.IsVaild()) return;
			m_pooledEventContainer.Post(@event);
		}
		
		public void PostBubble<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkBubble();
			TryAttachTrace(ref @event);
			if (!@event.IsVaild()) return;
			m_pooledEventContainer.Post(@event);
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
		private void PublishDelayLocal<T>(in T value, float ttlSeconds, DelayDirection direction, int contractLayer) where T : struct
		{
			var publisher = GetOrCreateDelayPublisher<T>();
			publisher.Publish(in value, ttlSeconds, direction, contractLayer);
			DelayPublisherManager.Instance.NotifyPublished(this, contractLayer, publisher);
		}

		private void PublishDelayToHigherLayers<T>(in T value, float ttlSeconds, DelayDirection direction, int contractLayer) where T : struct
		{
			Layer? layer = Previous as Layer;
			while (layer != null && !ReferenceEquals(layer, this))
			{
				layer.PublishDelayLocal(in value, ttlSeconds, direction, contractLayer);
				layer = layer.Previous as Layer;
			}
		}

		private void PublishDelayToLowerLayers<T>(in T value, float ttlSeconds, DelayDirection direction, int contractLayer) where T : struct
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
		//------------------------------------------------------------
		
		/// <summary>
		/// 向上冒泡事件。
		/// </summary>
		public void Bubble<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkBubble();
			if (!EventMetaData<Value>.IsFrequencyGateOpen)
			{
				EventMetaData<Value>.TimerScheduler.FireOnFrequency(in value, (@event) =>
				{
					@event.MarkBubble();
					TryAttachTrace(ref @event);
					BubbleInternal(in @event);
				});
				return;
			}
			TryAttachTrace(ref @event);
			BubbleInternal(@event);
		}
		
		/// <summary>
		/// 向下下沉事件。
		/// </summary>
		public void Drop<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkDrop();
			if (!EventMetaData<Value>.IsFrequencyGateOpen)
			{
				EventMetaData<Value>.TimerScheduler.FireOnFrequency(in value, (@event) =>
				{
					@event.MarkDrop();
					TryAttachTrace(ref @event);
					DropInternal(in @event);
				});
				return;
			}
			TryAttachTrace(ref @event);
			DropInternal(@event);
		}
		
		/// <summary>
		/// 广播事件到上下相邻 Layer。
		/// </summary>
		/// <typeparam name="Value"></typeparam>
		/// <param name="event"></param>
		public void BroadCast<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkBroadCast();
			
			if (!EventMetaData<Value>.IsFrequencyGateOpen)
			{
				EventMetaData<Value>.TimerScheduler.FireOnFrequency(in value, (@event) =>
				{
					@event.MarkBroadCast();
					TryAttachTrace(ref @event);
					BroadcastInternal(in @event);
				});
				return;
			}
			
			TryAttachTrace(ref @event);
			BroadcastInternal(@event);
		}

		private void BroadcastInternal<Value>(in Event<Value> @event) where Value : struct
		{
			if (!@event.IsVaild())
			{
				return;
			}

			EventHandledState currentState = Dispatch(@event);
			if (currentState == EventHandledState.Handled)
			{
				m_eventStateTracer?.TryComplete(@event.TraceToken);
				return;
			}

			Layer? higher = Previous as Layer;
			Layer? lower = NextNode as Layer;
		
			if (higher != null && higher != this)
			{
				higher.m_eventStateTracer?.TryIncrementPending(@event.TraceToken);
				higher.BubbleInternal(@event);
			}

			if (lower != null && lower != this)
			{
				lower.m_eventStateTracer?.TryIncrementPending(@event.TraceToken);
				lower.DropInternal(@event);
			}
			m_eventStateTracer?.TryComplete(@event.TraceToken);
		}
		
		// 方向性事件的通用处理入口。
		private void BubbleInternal<Value>(in Event<Value> @event) where Value : struct
		{
		    ProcessEventDirectionally(
		        @event,
		        () => Previous as Layer,
		        e => PostBubble(in e),
		        (layer, e) => layer.BubbleInternal(e)
		    );
		}

		private void DropInternal<Value>(in Event<Value> @event) where Value : struct
		{
		    ProcessEventDirectionally(
		        @event,
		        () => NextNode as Layer,
		        e => PostDrop(in e),
		        (layer, e) => layer.DropInternal(e)
		    );
		}
		
		private void ProcessEventDirectionally<Value>(
			in Event<Value>             @event,
			Func<Layer>                 getTargetLayer,
			Action<Event<Value>>        postMethod,
			Action<Layer, Event<Value>> recursiveMethod) 
			where Value : struct
		{
			if (!@event.IsVaild())
			{
				return;
			}
			
			// 根据 Layer 策略决定是否改为 Post/Ignore/Throw。
			if (UsedLayerStrategy(@event, postMethod))
				return;
	
			var eventHandledState = Dispatch(@event);
			if (eventHandledState == EventHandledState.Handled)
			{
				m_eventStateTracer?.TryComplete(@event.TraceToken);
				return;
			}
			
			Layer targetLayer= getTargetLayer();
			if (targetLayer != null && targetLayer != this)
			{
				targetLayer.m_eventStateTracer?.TryIncrementPending(@event.TraceToken);
				recursiveMethod(targetLayer, @event);
			}
			m_eventStateTracer?.TryComplete(@event.TraceToken);
		}

		private bool UsedLayerStrategy<Value>(Event<Value> @event, Action<Event<Value>> postMethod)
			where Value : struct
		{
			var layerDispatchStrategy = GetLayerStrategy(@event);
			if (layerDispatchStrategy == LayerDispatchStrategy.Post)
			{
				m_pooledEventContainer.Post(@event);
				return true;
			}
			if (layerDispatchStrategy == LayerDispatchStrategy.Ignore)
			{
				postMethod(@event);
				return true;
			}
			if (layerDispatchStrategy == LayerDispatchStrategy.Throw)
			{
				m_eventStateTracer?.TryComplete(@event.TraceToken);
				return true;
			}

			return false;
		}

		private LayerDispatchStrategy GetLayerStrategy<Value>(Event<Value> @event) where Value : struct
		{
			if (m_eventStateTracer == null)
			{
				return LayerDispatchStrategy.None;
			}
			
			m_eventStateTracer.TryGet(@event.TraceToken, out var eventState);
			var layerDispatchStrategy = LayerMetaData.LayerMetaData.GetDispatchStrategy(
				this.GetType(), eventState.CatalogueToken);
			return layerDispatchStrategy;
		}

		internal void NotifyEventProcessed<EventArg>(in Event<EventArg> @event) where EventArg : struct
		{
			m_eventStateTracer?.TryComplete(@event.TraceToken);
		}

		private void TryAttachTrace<Value>(ref Event<Value> @event) where Value : struct
		{
			if (m_eventStateTracer == null)
			{
				return;
			}
			var token = m_eventStateTracer.Register(@event);
			
			if (m_eventLogTracer !=null && m_eventLogTracer.Enabled)
			{
				ref EventState eventState = ref m_eventStateTracer.Resolve(token);
				m_eventLogTracer.Register(ref @eventState);
			}
			
			if (token.IsValid)
			{
				@event.AttachTraceToken(token);
			}
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



