using LayerBase.Core.EventStateTrace;

namespace LayerBase.Core.Event
{
    /// <summary>
    /// 事件处理状态
    /// </summary>
    public enum EventHandledState
    {
        /// <summary>已创建，待处理</summary>
        Created,
        /// <summary>已处理并截断</summary>
        Handled,
        /// <summary>未处理，继续传播</summary>
        Continue,
        /// <summary>已处理但继续传播</summary>
        HandledAndContinue,
    }

    /// <summary>
    /// 事件传播方向
    /// </summary>
    public enum EventForwardDir
    {
        BroadCast,
        Bubble,
        Drop,
    }

    /// <summary>
    /// 事件包装类型，携带元数据与追踪标记。
    /// </summary>
    public struct Event<EventArg> where EventArg : struct
    {
        private EventStateToken _traceToken;
        private EventHandledState _handledState;
        private EventForwardDir _forwardDirection;

        public EventArg Value;

        public Event(EventArg value)
        {
            _handledState = EventHandledState.Created;
            _forwardDirection = default;
            _traceToken = default;
            Value = value;
        }

        public int Id => EventTypeId<EventArg>.Id;
        public string Name => typeof(EventArg).Name;
        public EventForwardDir ForwardDir => _forwardDirection;
        internal EventStateToken TraceToken => _traceToken;

        public bool IsVaild() => _handledState != EventHandledState.Handled;
        public void MarkHandled() => _handledState = EventHandledState.Handled;
        public void MarkContinue() => _handledState = EventHandledState.Continue;
        public void MarkHandledAndContinue() => _handledState = EventHandledState.HandledAndContinue;

        public void MarkDrop() => _forwardDirection = EventForwardDir.Drop;
        public void MarkBubble() => _forwardDirection = EventForwardDir.Bubble;
        public void MarkBroadCast() => _forwardDirection = EventForwardDir.BroadCast;

        public override string ToString() => Name;

        internal void AttachTraceToken(EventStateToken token) => _traceToken = token;
    }
}
