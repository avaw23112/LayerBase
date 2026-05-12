namespace LayerBase.Actor;

internal interface IActorEventBucket
{
    PumpOneResult PumpOne(
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats,
        int                       bucketIndex);

    /// <summary>
    /// 批量 Pump 当前 Bucket。
    ///
    /// 参数说明：
    /// budget：当前帧预算。
    /// options：邮箱 Pump 配置。
    /// stats：Pump 统计构建器。
    /// bucketIndex：当前 bucket 的索引。
    /// maxEvents：本次最多处理多少事件。
    ///
    /// 作用：
    /// 减少每处理一个事件就返回 ActorWorld 外层调度器的成本。
    /// </summary>
    ActorPumpManyResult PumpMany(
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats,
        int                       bucketIndex,
        int                       maxEvents);

    bool HasPendingWork();
}