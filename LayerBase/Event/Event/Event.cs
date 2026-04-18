using System;

namespace LayerBase.Core.Event
{
    /// <summary>
    /// 事件处理状态：用于控制事件流在层级间的传播行为。
    /// </summary>
    public enum EventHandledState
    {
        /// <summary>
        /// 继续传播：允许后续 Handler 和 后续层级继续处理此事件。
        /// </summary>
        Continue,
        
        /// <summary>
        /// 已处理（完全截断）：立即停止此事件在当前及所有后续层级的传播。
        /// </summary>
        Handled,
        
        /// <summary>
        /// 已处理（层级截断）：标记事件在当前层级已处理，不再传播给后续层级，但允许当前层级内的后续 Handler 继续处理。
        /// </summary>
        HandledAndContinue
    }

    public struct Event<T> where T : struct
    {
        public T Value;
        public ulong TargetMask;
        
        /// <summary>
        /// 事件传播模式。
        /// </summary>
        public int Propagation;

        public Event(T value)
        {
            Value = value;
            TargetMask = 0;
            Propagation = 0; // 默认 Global
        }
    }
}
