using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

/// <summary>
/// EventStream 中的单封邮件。
///
/// 作用：
/// 存储事件数据、目标 slotIndex 和 Actor 创建时的 generation。
/// Pump 时通过 generation 校验邮件是否仍然有效。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
[SkipLocalsInit]
internal struct EventStreamMail<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 目标 Actor 的 slotIndex。
    /// </summary>
    public int SlotIndex;

    /// <summary>
    /// 目标 Actor 创建时的 generation。
    /// 如果 Actor 已销毁并复用 slot，generation 不匹配，邮件会被跳过。
    /// </summary>
    public int Generation;

    /// <summary>
    /// 事件数据。
    /// </summary>
    public TEvent Value;
}
