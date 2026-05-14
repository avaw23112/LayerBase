namespace LayerBase.Actor;

public readonly struct ActorMailPumpOptions
{
    public readonly int MaxTotalMailsPerPump;
    public readonly int MaxMailsPerBucketPerPump;
    public readonly int MaxMailsPerActorPerPump;
    public readonly int MaxEmptyBucketChecksPerPump;
    public readonly int TimeCheckInterval;
    public readonly int MaxEventCountPerPump;

    public ActorMailPumpOptions(
        int maxTotalMailsPerPump,
        int maxMailsPerBucketPerPump,
        int maxMailsPerActorPerPump,
        int maxEmptyBucketChecksPerPump,
        int timeCheckInterval,
        int maxEventCountPerPump)
    {
        MaxTotalMailsPerPump = maxTotalMailsPerPump;
        MaxMailsPerBucketPerPump = maxMailsPerBucketPerPump;
        MaxMailsPerActorPerPump = maxMailsPerActorPerPump;
        MaxEmptyBucketChecksPerPump = maxEmptyBucketChecksPerPump;
        TimeCheckInterval = Math.Max(timeCheckInterval, 1);
        MaxEventCountPerPump = maxEventCountPerPump;
    }

    /// <summary>
    /// 吞吐优先模式。
    ///
    /// 适用场景：
    /// Benchmark、高频 ECS-Actor 桥接、批量事件分发。
    ///
    /// 特点：
    /// 不对单个 Bucket 或 Actor 做额外限流。
    /// 只受 RuntimeFrameBudget 控制。
    /// </summary>
    public static ActorMailPumpOptions Throughput => new(
        maxTotalMailsPerPump: 0,
        maxMailsPerBucketPerPump: 0,
        maxMailsPerActorPerPump: 0,
        maxEmptyBucketChecksPerPump: 64,
        timeCheckInterval: 128,
        maxEventCountPerPump: 128);

    /// <summary>
    /// 公平优先模式。
    ///
    /// 适用场景：
    /// 很多 Actor 同时有消息。
    /// 不希望某个 Actor 或某个 Bucket 独占一帧。
    /// 消息处理耗时差异较大。
    /// 更重视响应均衡，而不是极限吞吐。
    /// </summary>
    public static ActorMailPumpOptions Fair => new(
        maxTotalMailsPerPump: 1024,
        maxMailsPerBucketPerPump: 128,
        maxMailsPerActorPerPump: 8,
        maxEmptyBucketChecksPerPump: 64,
        timeCheckInterval: 16,
        maxEventCountPerPump: 1);

    /// <summary>
    /// 默认模式。
    ///
    /// 短期保持与 Throughput 相同的行为。
    /// </summary>
    public static ActorMailPumpOptions Default => Throughput;
}
