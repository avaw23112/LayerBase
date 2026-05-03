namespace LayerBase.Core.Event;

/// <summary>
/// 为事件类型声明稳定身份。
///
/// 这个特性只负责稳定诊断身份，不参与热路径事件派发。
/// 热路径仍然使用 EventTypeId<TEvent>.Id。
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class EventIdentityAttribute : Attribute
{
    /// <summary>
    /// 创建事件稳定身份。
    /// </summary>
    /// <param name="stableId">
    /// 稳定数字 ID。
    /// 这个值不应该依赖运行期分配顺序。
    /// 建议由用户显式维护，或由 Source Generator 根据稳定 key 生成并检查冲突。
    /// </param>
    /// <param name="stableKey">
    /// 稳定字符串 Key。
    /// 推荐格式类似 "Combat.DamageApplied"、"UI.InventoryChanged"。
    /// 它用于日志、调试、回放和跨版本迁移。
    /// </param>
    /// <param name="version">
    /// 事件结构版本。
    /// 当事件字段含义发生不兼容变化时，应增加版本号。
    /// </param>
    public EventIdentityAttribute(int stableId, string stableKey, int version = 1)
    {
        if (stableId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stableId),
                "Stable event id must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(stableKey))
        {
            throw new ArgumentException(
                "Stable event key is required.",
                nameof(stableKey));
        }

        StableId = stableId;
        StableKey = stableKey;
        Version = version <= 0 ? 1 : version;
    }

    /// <summary>
    /// 稳定数字 ID。
    /// 用于紧凑日志、回放流、二进制协议等场景。
    /// </summary>
    public int StableId { get; }

    /// <summary>
    /// 稳定字符串 Key。
    /// 用于人类可读诊断和跨版本迁移。
    /// </summary>
    public string StableKey { get; }

    /// <summary>
    /// 事件结构版本。
    /// 用于判断旧数据是否可以安全读取。
    /// </summary>
    public int Version { get; }
}
