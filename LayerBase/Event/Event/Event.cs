using LayerBase.Core.EventStateTrace;

namespace LayerBase.Core.Event
{
	public enum EventHandledState
	{
		// 创建
		Created,

		// 已经被处理
		Handled,

		// 不处理继续传播
		Continue,

		// 处理但继续传播
		HandledAndContinue,
	}

	public enum EventForwardDir
	{
		BroadCast,
		Bubble,
		Drop,
	}

	/// <summary>
	/// TODO:记录事件传播过程（在哪一层被哪些handler处理过）
	/// </summary>
	public struct Event<EventArg> where EventArg : struct
	{
		private EventStateToken _traceToken;
		private EventHandledState _mEventHandledState;
		private EventForwardDir _mForwardDir;
		private EventArg m_value;

		public Event(EventArg value)
		{
			_mEventHandledState = EventHandledState.Created;
			m_value = value;
		}

		public int Id => EventTypeId<EventArg>.Id;
		public string Name => typeof(EventArg).Name;
		public EventArg Value => m_value;
		public EventForwardDir ForwardDir => _mForwardDir;
		internal EventStateTrace.EventStateToken TraceToken => _traceToken;
				
		public bool IsVaild() => _mEventHandledState != EventHandledState.Handled;
		public void MarkHandled()=>_mEventHandledState = EventHandledState.Handled;
		public void MarkContinue() =>_mEventHandledState = EventHandledState.Continue;
		public void MarkHandledAndContinue()=>_mEventHandledState = EventHandledState.HandledAndContinue;
		public void MarkDrop() => _mForwardDir = EventForwardDir.Drop;
		public void MarkBubble() => _mForwardDir = EventForwardDir.Bubble;
		public void MarkBroadCast() => _mForwardDir = EventForwardDir.BroadCast;
		public override string ToString() => Name;
		internal void AttachTraceToken(EventStateToken token) => _traceToken = token;
	}
}
