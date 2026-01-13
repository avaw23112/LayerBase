using System.Runtime.CompilerServices;
using Events.Core.Event;
using Events.Core.EventHandler;
using Events.Core.PolledEventContainer;
using Events.Core.ResponsibilityChain;

namespace Events.LayerChain
{
	/// <summary>
	/// 责任链式层级结构
	/// </summary>
	public abstract class Layer : Node
	{
		private EventDispatcher m_eventDispatcher;
		private PooledEventContainer m_pooledEventContainer;
		protected Layer() 
		{
			m_eventDispatcher = new EventDispatcher();
			m_pooledEventContainer = new PooledEventContainer(this);
		}
		
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
		
		/// <summary>
		/// 只供EventHub调用的Pump
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Pump()
		{
			m_pooledEventContainer.Pump();
		}
		public void Post<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkBroadCast();
			if (!@event.IsVaild()) return;
			m_pooledEventContainer.Post(@event);
		}

		public void PostDrop<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkDrop();
			if (!@event.IsVaild()) return;
			m_pooledEventContainer.Post(@event);
		}
		
		public void PostBubble<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkBubble();
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
			BubbleInternal(@event);
		}
		
		/// <summary>
		/// 向下一层递送事件
		/// </summary>
		public void Drop<Value>(in Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkDrop();
			DropInternal(@event);
		}
		
		/// <summary>
		/// 广播事件
		/// </summary>
		/// <typeparam name="Value"></typeparam>
		/// <param name="event"></param>
		public void BroadCast<Value>(Value value) where Value : struct
		{
			Event<Value> @event = new Event<Value>(value);
			@event.MarkBroadCast();
			BubbleInternal(@event);
			DropInternal(@event);
		}
		
		private void BubbleInternal<Value>(in Event<Value> @event) where Value : struct
		{
			if (!@event.IsVaild())
			{
				return;
			}
			
			EventState eventState = m_eventDispatcher.Dispatch(@event);
			if (eventState == EventState.Handled)
			{
				return;
			}

			//防空、防错、防循环
			if (Previous != null && Previous is Layer preLayer && preLayer != this)
			{
				preLayer.BubbleInternal(@event);
			}
		}

		private void DropInternal<Value>(in Event<Value> @event) where Value : struct
		{
			if (!@event.IsVaild())
			{
				return;
			}

			EventState eventState = m_eventDispatcher.Dispatch(@event);
			if (eventState == EventState.Handled)
			{
				return;
			}

			//防空、防错、防循环
			if (Next != null && Next is Layer nextLayer && nextLayer != this)
			{
				nextLayer.DropInternal(@event);
			}
		}
	}
}