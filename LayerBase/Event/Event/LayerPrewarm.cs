using System;

namespace LayerBase.Core.Event;

/// <summary>
/// 事件系统预热目标。
/// </summary>
[Flags]
public enum LayerPrewarmTargets
{
    /// <summary>
    /// 不执行任何预热。
    /// </summary>
    None = 0,

    /// <summary>
    /// 预热事件类型 ID。
    /// 提前访问 EventTypeId&lt;TEvent&gt;.Id。
    /// </summary>
    EventTypeId = 1 << 0,

    /// <summary>
    /// 预热事件 Bucket。
    /// 提前创建 EventBucket&lt;TEvent&gt; 并写入静态缓存。
    /// </summary>
    Bucket = 1 << 1,

    /// <summary>
    /// 预热派发表。
    /// 如果 Bucket 当前是 dirty 状态，则提前执行 Rebuild。
    /// </summary>
    DispatchTable = 1 << 2,

    /// <summary>
    /// 预热 Post 队列。
    /// 提前为指定事件类型创建 Post 存储。
    /// </summary>
    PostQueue = 1 << 3,

    /// <summary>
    /// 推荐默认预热目标。
    /// </summary>
    Default = EventTypeId | Bucket | DispatchTable,

    /// <summary>
    /// 完整预热目标。
    /// </summary>
    All = EventTypeId | Bucket | DispatchTable | PostQueue
}

/// <summary>
/// 事件系统预热参数。
/// </summary>
public readonly struct LayerPrewarmOptions
{
    /// <summary>
    /// 要执行的预热目标。
    /// </summary>
    public readonly LayerPrewarmTargets Targets;

    /// <summary>
    /// PostQueue 预热的 Layer 数量上限。
    /// 在当前 PostScheduler 模型下，该参数主要作为预留，
    /// 或者用于限制预热存储的某些分片。
    /// </summary>
    public readonly int LayerCount;

    /// <summary>
    /// 创建预热参数。
    /// </summary>
    public LayerPrewarmOptions(
        LayerPrewarmTargets targets = LayerPrewarmTargets.Default,
        int layerCount = 0)
    {
        Targets = targets;
        LayerCount = layerCount;
    }

    /// <summary>
    /// 默认预热参数。
    /// </summary>
    public static LayerPrewarmOptions Default => new();
}

/// <summary>
/// 标记某个事件类型需要加入源生成器预热清单。
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class PrewarmEventAttribute : Attribute
{
}
