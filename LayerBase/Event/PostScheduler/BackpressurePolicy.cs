namespace LayerBase.Core.Event;

/// <summary>
/// 普通 Post 队列满时的背压策略。
///
/// 背压是指：
/// 当生产速度大于消费速度，队列容量不够时，系统如何处理新事件。
/// </summary>
public enum BackpressurePolicy
{
    /// <summary>
    /// 拒绝新事件。
    /// 适合不能丢失旧事件，也不能隐式覆盖事件的场景。
    /// </summary>
    RejectNew,

    /// <summary>
    /// 丢弃新事件。
    /// 适合允许跳过过量事件的通知类场景。
    /// </summary>
    DropNewest,

    /// <summary>
    /// 丢弃最旧事件，然后尝试放入新事件。
    /// 适合“越新的事件越有价值”的普通队列场景。
    /// </summary>
    DropOldest
}
