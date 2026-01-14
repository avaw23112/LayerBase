using System;
using LayerBase.Core.Event;

namespace LayerBase.Core.EventStateTrace;

/// <summary>
/// 记录事件的传播路径与状态，存储于池化槽位中以降低 GC。
/// </summary>
public struct EventState
{
    /// <summary>
    /// 当前事件类型的 id（与 EventTypeId 对应）。
    /// </summary>
    public int EventTypeId;

    /// <summary>
    /// 当前处理状态。
    /// </summary>
    public EventHandledState HandledState;

    /// <summary>
    /// 当前传播方向。
    /// </summary>
    public EventForwardDir ForwardDir;

    /// <summary>
    /// 生成时间戳（Stopwatch.GetTimestamp）。
    /// </summary>
    public long CreatedTimestamp;

    internal EventPath PathHandle;

    /// <summary>
    /// 返回路径的只读视图（segment id 序列）。
    /// </summary>
    public ReadOnlySpan<int> Path => PathHandle.AsSpan();
}
