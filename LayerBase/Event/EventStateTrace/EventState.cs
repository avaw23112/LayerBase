using System;
using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;

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
    /// 事件发起时间戳（Stopwatch.GetTimestamp）。
    /// </summary>
    public long StartTimestamp;

    /// <summary>
    /// 当前处理状态。
    /// </summary>
    public EventHandledState HandledState;

    /// <summary>
    /// 当前传播方向。
    /// </summary>
    public EventForwardDir ForwardDir;
    
    /// <summary>
    /// 所属类别
    /// </summary>
    public EventCategoryToken CatalogueToken;

    /// <summary>
    /// 生成时间戳（Stopwatch.GetTimestamp）。
    /// </summary>
    public long CreatedTimestamp;

    /// <summary>
    /// 当前仍在处理/传播的分支计数。
    /// </summary>
    public int PendingCount;

    internal EventPath PathHandle;
    public bool HasPath => PathHandle.HasValue;
}
