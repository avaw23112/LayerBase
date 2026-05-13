namespace LayerBase.Actor;

/// <summary>
/// 批量 Pump 的结果。
/// 用于表达一次批量 Pump 实际处理了多少事件，以及为什么结束。
/// </summary>
internal readonly struct ActorPumpManyResult
{
    /// <summary>
    /// 本次实际处理的事件数量。
    /// </summary>
    public readonly int Processed;

    /// <summary>
    /// 批量 Pump 的结束原因。
    /// 复用现有 PumpOneResult，避免新增复杂状态枚举。
    /// </summary>
    public readonly PumpOneResult Result;

    /// <summary>
    /// 是否至少处理了一个事件。
    /// </summary>
    public bool HasProcessed => Processed > 0;

    /// <summary>
    /// 构造批量 Pump 结果。
    ///
    /// 参数说明：
    /// processed：本批次处理的事件数量。
    /// result：结束原因。
    /// </summary>
    public ActorPumpManyResult(
        int processed,
        PumpOneResult result)
    {
        Processed = processed;
        Result = result;
    }

    /// <summary>
    /// 表示没有可处理工作。
    /// </summary>
    public static ActorPumpManyResult NoWork()
    {
        return new ActorPumpManyResult(
            processed: 0,
            result: PumpOneResult.NoWork);
    }

    /// <summary>
    /// 表示成功处理了一批事件。
    ///
    /// 参数说明：
    /// processed：本批次处理的事件数量。
    /// </summary>
    public static ActorPumpManyResult ProcessedBatch(int processed)
    {
        return new ActorPumpManyResult(
            processed: processed,
            result: PumpOneResult.Processed);
    }
}
