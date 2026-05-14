namespace LayerBase.Actor;

/// <summary>
/// 邮箱 Pump 统计模式。
///
/// 作用：
/// 控制 Pump 过程中记录哪些统计信息。
/// 在高吞吐场景下，关闭统计可以减少热路径开销。
/// </summary>
public enum ActorMailPumpStatsMode
{
    /// <summary>
    /// 不记录任何统计信息。
    ///
    /// 适用场景：
    /// Benchmark、高频 ECS-Actor 桥接、批量事件分发。
    /// 调度状态数据仍然保留，只关闭观测统计。
    /// </summary>
    None,

    /// <summary>
    /// 记录基本统计信息。
    ///
    /// 适用场景：
    /// 生产环境监控。
    /// 不记录 Bucket 级细节，只记录总数和限流命中次数。
    /// </summary>
    Basic,

    /// <summary>
    /// 记录完整统计信息。
    ///
    /// 适用场景：
    /// 调试、性能分析。
    /// 记录 Bucket 级和 Actor 级细节。
    /// </summary>
    Full
}
