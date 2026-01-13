namespace Events.Core.Event
{
	public enum EventState
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

	public enum EventDir
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
		private EventState m_eventState;
		private EventDir m_dir;
		private EventArg m_value;

		public Event(EventArg value)
		{
			m_eventState = EventState.Created;
			m_value = value;
		}

		public int Id => EventTypeId<EventArg>.Id;
		public string Name => typeof(EventArg).Name;
		public EventArg Value => m_value;
		public EventDir Dir => m_dir;
				
		public bool IsVaild() => m_eventState != EventState.Handled;
		public void MarkHandled()=>m_eventState = EventState.Handled;
		public void MarkContinue() =>m_eventState = EventState.Continue;
		public void MarkHandledAndContinue()=>m_eventState = EventState.HandledAndContinue;
		public void MarkDrop() => m_dir = EventDir.Drop;
		public void MarkBubble() => m_dir = EventDir.Bubble;
		public void MarkBroadCast() => m_dir = EventDir.BroadCast;

		public override string ToString() => Name;
	}
}