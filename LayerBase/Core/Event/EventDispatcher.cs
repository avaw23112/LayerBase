using System;
using System.Collections.Generic;
using LayerBase.Async;
using LayerBase.Core.EventHandler;

namespace LayerBase.Core.Event
{

	/// <summary>
	/// 每一层都有一个Dispatcher,负责管理所有绑定的事件handler
	/// </summary>
	internal class EventDispatcher
	{
		private readonly Dictionary<int, List<Delegate>> m_map = new ();
		private readonly Dictionary<int, List<IEventHandler>> m_handlerMap = new ();
		private readonly object m_lock = new ();
		
		/// <summary>
		/// 所有处理器都是顺序无关的,它不跟踪事件的状态
		/// </summary>
		/// <param name="handler"></param>
		/// <typeparam name="EventArg"></typeparam>
		/// <exception cref="ArgumentNullException"></exception>
		public void Subscribe<EventArg>(IEventHandler<EventArg> handler) where EventArg : struct
		{
			if (handler == null) throw new ArgumentNullException(nameof(handler));
			int typeId = EventTypeId<EventArg>.Id;
			lock (m_lock)
			{
				if (!m_handlerMap.TryGetValue(typeId, out var list))
				{
					list = new List<IEventHandler>(capacity: 4);
					m_handlerMap[typeId] = list;
				}

				list.Add(handler);
			}
		}
		
		/// <summary>
		/// 所有处理方法都是顺序相关的,它跟踪事件状态
		/// </summary>
		/// <param name="handlerDelegate"></param>
		/// <typeparam name="EventArg"></typeparam>
		/// <exception cref="ArgumentNullException"></exception>
		public void Subscribe<EventArg>(EventHandlerDelegate<EventArg> handlerDelegate) where EventArg : struct
		{
			if (handlerDelegate == null) throw new ArgumentNullException(nameof(handlerDelegate));
			int typeId = EventTypeId<EventArg>.Id;

			lock (m_lock)
			{
				if (!m_map.TryGetValue(typeId, out var list))
				{
					list = new List<Delegate>(capacity: 4);
					m_map[typeId] = list;
				}

				list.Add(handlerDelegate);
			}
		}
		public void Subscribe<EventArg>(EventHandlerDelegateAsync<EventArg> handlerDelegate) where EventArg : struct
		{
			if (handlerDelegate == null) throw new ArgumentNullException(nameof(handlerDelegate));
			int typeId = EventTypeId<EventArg>.Id;

			lock (m_lock)
			{
				if (!m_map.TryGetValue(typeId, out var list))
				{
					list = new List<Delegate>(capacity: 4);
					m_map[typeId] = list;
				}

				list.Add(handlerDelegate);
			}
		}
		public bool Unsubscribe<T>(IEventHandler<T> handler) where T : struct
		{
			if (handler == null) return false;
			int typeId = EventTypeId<T>.Id;

			lock (m_lock)
			{
				if (!m_handlerMap.TryGetValue(typeId, out var list) || list.Count == 0)
					return false;
				bool removed = list.Remove(handler);

				if (removed && list.Count == 0)
				{
					m_handlerMap.Remove(typeId);
				}
				return removed;
			}
		}
		
		public bool Unsubscribe<T>(EventHandlerDelegate<T> handlerDelegate) where T : struct
		{
			if (handlerDelegate == null) return false;
			int typeId = EventTypeId<T>.Id;

			lock (m_lock)
			{
				if (!m_map.TryGetValue(typeId, out var list) || list.Count == 0)
					return false;
				bool removed = list.Remove(handlerDelegate);

				if (removed && list.Count == 0)
				{
					m_map.Remove(typeId);
				}
				return removed;
			}
		}
		public bool Unsubscribe<T>(EventHandlerDelegateAsync<T> handlerDelegateAsync) where T : struct
		{
			if (handlerDelegateAsync == null) return false;

			int typeId = EventTypeId<T>.Id;

			lock (m_lock)
			{
				if (!m_map.TryGetValue(typeId, out var list) || list.Count == 0)
					return false;
				bool removed = list.Remove(handlerDelegateAsync);

				if (removed && list.Count == 0)
				{
					m_map.Remove(typeId);
				}
				return removed;
			}
		}
		public EventState Dispatch<T>(in Event<T> @event) where T : struct
		{
			int typeId = EventTypeId<T>.Id;
			
			//先处理无顺序的Handler
			DealHandlers(@event, typeId);
			
			//再处理有顺序的Delegate
			return DealDelegates(@event, typeId);
		}

		private EventState DealDelegates<T>(in Event<T> @event, int typeId) where T : struct
		{
			//有效性校验
			if (!m_map.TryGetValue(typeId, out var list) || list.Count == 0)
			{
				return EventState.Continue;
			}
			if (!@event.IsVaild())
			{
				return EventState.Handled;
			}
			
			//留个tag,最终决定是否继续传播事件
			bool handledAndContinueSeen = false;
			for (int i = 0; i < list.Count; i++)
			{
				var d = list[i];
				var value = @event.Value;
				
				//有序处理事件委托
				if (d is EventHandlerDelegate<T> eventHandlerDelegate)
				{
					EventState r = eventHandlerDelegate(value);
					if (r == EventState.Handled)
					{
						@event.MarkHandled();
						return EventState.Handled;
					}
					if (r == EventState.HandledAndContinue)
					{
						@event.MarkHandledAndContinue();
						handledAndContinueSeen = true;
					}
					else @event.MarkContinue();
				}
				//如果是异步事件,即发即忘不等待.
				else if(d is EventHandlerDelegateAsync<T> eventHandlerDelegateAsync)
				{
					eventHandlerDelegateAsync(value).Forget();
				}
			}
			return handledAndContinueSeen ? EventState.HandledAndContinue : EventState.Continue;
		}

		private void DealHandlers<T>(in Event<T> @event, int typeId) where T : struct
		{
			if (m_handlerMap.TryGetValue(typeId, out var handlers) && handlers.Count != 0)
			{
				for (int i = 0; i < handlers.Count; i++)
				{
					var handler = handlers[i];
					if (handler is IEventHandler<T> eventHandler == false)
					{
						throw new Exception($"致命错误.{@event}事件被分发到不可能的事件处理器{handler.GetType().Name}中");
					}
					
					var value = @event.Value;
					eventHandler.Deal(value);
				}
			}
		}
	}
}