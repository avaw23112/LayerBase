using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.EventStateTrace;
using LayerBase.Core.PolledEventContainer;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;

namespace LayerBase.Layers
{
	/// <summary>
	/// 责任链式层级结构
	/// TODO:将EventStateTracer改为必非空
	/// </summary>
	public abstract class Layer : Node
	{
		private EventDispatcher m_eventDispatcher;
		private PooledEventContainer m_pooledEventContainer;
		private EventStateTracer? m_eventStateTracer;
		private EventLogTracer? m_eventLogTracer;
		
		//临时服务容器,存储由源生成器填充的Service
		private ServiceCollection? m_serviceCollection;
		private ServiceProvider? m_serviceProvider;
		
		protected Layer() 
		{
			m_eventDispatcher = new EventDispatcher();
			m_pooledEventContainer = new PooledEventContainer(this);
			m_serviceCollection = new ServiceCollection();
		}
		
		/// <summary>
		/// 主要入口
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Pump()
		{
			m_pooledEventContainer.Pump();
			m_eventStateTracer?.Pump();
		}
		
		// -----------------DI-------------------
		public T GetService<T>()
		{
			if (m_serviceProvider == null)
				throw new NullReferenceException("层级未构建容器");
			return m_serviceProvider.Get<T>();
		}
		public void Dispose() => m_serviceProvider?.Dispose();
		
		/// <summary>
		/// 由源生成器代码调用,将服务注册服务到容器中
		/// </summary>
		/// <param name="service"></param>
		public void RegisterService(IService service)
		{
			service.ConfigureServices(m_serviceCollection);
		}
		
		/// <summary>
		/// 构建服务实例
		/// </summary>
		public void Build()
		{
			m_serviceProvider?.Dispose();
			m_serviceProvider = new ServiceProvider(m_serviceCollection.ToDescriptors());
		}
		
		// -----------------追踪-------------------
		
		internal void SetEventTracer(EventStateTracer stateTracer)
		{
			if (stateTracer == null)
			{
				throw new Exception("无效事件追踪器");
			}
			m_eventStateTracer = stateTracer;
			m_eventDispatcher.StateTracer = stateTracer;
		}

		internal void SetEventLogTracer(EventLogTracer logTracer)
		{
			if (logTracer == null)
			{
				throw new Exception("无效日志追踪器");
			}
			if (m_eventStateTracer == null)
			{
				throw new Exception("无效事件追踪器");
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
		
		// -----------------追踪-------------------
		
		/// <summary>
		/// 绑定当前层的事件处理器
		/// </summary>
		/// <typeparam name="Value"></typeparam>
		/// <param name="eventHandlerDelegate"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Bind<Value>(EventHandlerDelegate<Value> eventHandlerDelegate) where Value : struct
		{
			m_eventDispatcher.Subscribe(eventHandlerDelegate);
		}

		/// <summary>
		/// 绑定当前层的异步事件处理器
		/// </summary>
		/// <typeparam name="Value"></typeparam>
		/// <param name="eventHandlerDelegateAsync"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Bind<Value>(EventHandlerDelegateAsync<Value> eventHandlerDelegateAsync) where Value : struct
		{
			m_eventDispatcher.Subscribe(eventHandlerDelegateAsync);
		}
		
		/// <summary>
		/// 绑定当前层的顺序无关事件处理器
		/// </summary>
		/// <param name="eventHandler"></param>
		/// <typeparam name="Value"></typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Bind<Value>(IEventHandler<Value> eventHandler) where Value : struct
		{
			m_eventDispatcher.Subscribe(eventHandler);
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
			ref EventState eventState = ref m_eventStateTracer.Resolve(@event.TraceToken);
			m_eventLogTracer?.TryBeginLayer(ref eventState, GetType().Name);
			return m_eventDispatcher.Dispatch(@event);
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
		//------------------------------------------------------------
		
		/// <summary>
		/// 向上一层递送事件
		/// </summary>
		public void Bubble<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkBubble();
			TryAttachTrace(ref @event);
			BubbleInternal(@event);
		}
		
		/// <summary>
		/// 向下一层递送事件
		/// </summary>
		public void Drop<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkDrop();
			TryAttachTrace(ref @event);
			DropInternal(@event);
		}
		
		/// <summary>
		/// 广播事件
		/// </summary>
		/// <typeparam name="Value"></typeparam>
		/// <param name="event"></param>
		public void BroadCast<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkBroadCast();
			TryAttachTrace(ref @event);		
			BubbleInternal(@event);
			DropInternal(@event);
		}
		
		private void BubbleInternal<Value>(in Event<Value> @event) where Value : struct
		{
			if (!@event.IsVaild())
			{
				return;
			}
			
			EventHandledState eventHandledState = Dispatch(@event);
			if (eventHandledState == EventHandledState.Handled)
			{
				m_eventStateTracer?.TryComplete(@event.TraceToken);
				return;
			}

			//防空、防错、防循环
			if (Previous != null && Previous is Layer preLayer && preLayer != this)
			{
				preLayer.m_eventStateTracer?.TryIncrementPending(@event.TraceToken);
				preLayer.BubbleInternal(@event);
			}
			m_eventStateTracer?.TryComplete(@event.TraceToken);
		}

		private void DropInternal<Value>(in Event<Value> @event) where Value : struct
		{
			if (!@event.IsVaild())
			{
				return;
			}

			EventHandledState eventHandledState = Dispatch(@event);
			if (eventHandledState == EventHandledState.Handled)
			{
				m_eventStateTracer?.TryComplete(@event.TraceToken);
				return;
			}

			//防空、防错、防循环
			if (Next != null && Next is Layer nextLayer && nextLayer != this)
			{
				nextLayer.m_eventStateTracer?.TryIncrementPending(@event.TraceToken);
				nextLayer.DropInternal(@event);
			}
			m_eventStateTracer?.TryComplete(@event.TraceToken);
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
	}
}
