namespace LayerBase.Core.Event;

/// <summary>
/// 事件身份快照。
///
/// 它同时包含运行期 ID 和稳定 ID。
/// 运行期 ID 用于当前 Runtime 的热路径。
/// 稳定 ID 用于诊断、日志、回放、存档和跨版本识别。
/// </summary>
public readonly struct EventIdentity
{
    /// <summary>
    /// 创建事件身份快照。
    /// </summary>
    /// <param name="runtimeId">
    /// 运行期事件 ID。
    /// 它来自 EventTypeId<TEvent>.Id，只保证当前进程内可用。
    /// </param>
    /// <param name="stableId">
    /// 稳定数字 ID。
    /// 这个值不应该因为事件类型首次访问顺序不同而变化。
    /// </param>
    /// <param name="stableKey">
    /// 稳定字符串 Key。
    /// 它用于日志和跨版本迁移。
    /// </param>
    /// <param name="version">
    /// 事件结构版本。
    /// 用于判断事件数据结构是否兼容。
    /// </param>
    /// <param name="eventType">
    /// 事件 CLR 类型。
    /// CLR 是 .NET 运行时的类型系统。
    /// 这里保存 Type 只用于诊断，不用于热路径派发。
    /// </param>
    public EventIdentity(
        int runtimeId,
        int stableId,
        string stableKey,
        int version,
        Type eventType)
    {
        RuntimeId = runtimeId;
        StableId = stableId;
        StableKey = stableKey;
        Version = version;
        EventType = eventType;
    }

    /// <summary>
    /// 当前运行期事件 ID。
    /// </summary>
    public int RuntimeId { get; }

    /// <summary>
    /// 稳定数字 ID。
    /// </summary>
    public int StableId { get; }

    /// <summary>
    /// 稳定字符串 Key。
    /// </summary>
    public string StableKey { get; }

    /// <summary>
    /// 事件结构版本。
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// 事件 CLR 类型。
    /// </summary>
    public Type EventType { get; }
}
