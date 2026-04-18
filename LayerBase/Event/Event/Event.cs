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
    /// 事件包装类型，携带事件值与传播方向。
    /// </summary>
    public struct Event<EventArg> where EventArg : struct
    {
        private EventHandledState _handledState;
        private EventForwardDir _forwardDirection;
        private bool _shouldForwardFromQueue;
        internal ulong TargetMask;

        public EventArg Value;

        public Event(EventArg value)
        {
            _handledState = EventHandledState.Created;
            _forwardDirection = default;
            _shouldForwardFromQueue = true;
            TargetMask = 0;
            Value = value;
        }

        public int Id => EventTypeId<EventArg>.Id;
        public string Name => typeof(EventArg).Name;
        public EventForwardDir ForwardDir => _forwardDirection;
        internal bool ShouldForwardFromQueue => _shouldForwardFromQueue;

        public bool IsVaild() => _handledState != EventHandledState.Handled;
        public EventHandledState HandledState => _handledState;
        public void MarkHandled() => _handledState = EventHandledState.Handled;
        public void MarkContinue() => _handledState = EventHandledState.Continue;
        public void MarkHandledAndContinue() => _handledState = EventHandledState.HandledAndContinue;

        public void MarkDrop() => _forwardDirection = EventForwardDir.Drop;
        public void MarkBubble() => _forwardDirection = EventForwardDir.Bubble;
        public void MarkBroadCast() => _forwardDirection = EventForwardDir.BroadCast;

        public override string ToString() => Name;

        internal void DisableQueuedForwarding() => _shouldForwardFromQueue = false;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubscribeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubscribeAsyncAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubscribeParallelAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class SubscribeDelayAttribute : Attribute { }
}
