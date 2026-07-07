namespace LayerBase.Core.Event;

/// <summary>
/// 事件处理结果状态。
/// </summary>
public enum EventHandledState
{
    /// <summary>继续后续处理器。</summary>
    Continue,
    /// <summary>事件已被处理，停止后续同步处理器。</summary>
    Handled,
    /// <summary>事件已被处理，但继续传递给后续处理器。</summary>
    HandledAndContinue
}

/// <summary>
/// 事件数据的泛型包装。提供与底层值类型之间的隐式转换。
/// </summary>
public readonly struct Event<T> where T : struct
{
    public readonly T Value;

    public Event(T value)
    {
        Value = value;
    }

    public static implicit operator T(Event<T> e) => e.Value;
    public static implicit operator Event<T>(T value) => new(value);
}