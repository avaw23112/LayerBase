namespace LayerBase.Actor;

/// <summary>
/// EventStream 队列配置。
///
/// 作用：
/// 控制每个事件类型的全局邮件队列如何分段扩容，以及池最多保留多少备用段。
/// </summary>
public readonly struct EventStreamOptions
{
    /// <summary>
    /// 每个 Segment 能容纳多少封邮件。
    ///
    /// 参数作用：
    /// 值越大，Segment 数量越少，但单个数组更大。
    /// 值越小，单个数组更轻，但 Segment 链接数量更多。
    /// </summary>
    public readonly int SegmentCapacity;

    /// <summary>
    /// Segment 池最多保留多少个空闲 Segment。
    ///
    /// 参数作用：
    /// 避免消息高峰结束后，池里长期保留大量空闲数组。
    /// </summary>
    public readonly int MaxRetainedSegments;

    /// <summary>
    /// 构造 EventStreamOptions。
    /// </summary>
    /// <param name="segmentCapacity">
    /// 每个 Segment 的邮件容量。
    /// 必须大于 0。
    /// 如果传入小于等于 0 的值，将回退到 512。
    /// </param>
    /// <param name="maxRetainedSegments">
    /// Segment 池最多保留多少个空闲 Segment。
    /// 可以为 0。
    /// 0 表示读空 Segment 后不缓存备用 Segment。
    /// </param>
    public EventStreamOptions(
        int segmentCapacity,
        int maxRetainedSegments)
    {
        SegmentCapacity = segmentCapacity > 0
            ? segmentCapacity
            : 512;

        MaxRetainedSegments = maxRetainedSegments >= 0
            ? maxRetainedSegments
            : 4;
    }

    /// <summary>
    /// 默认 EventStream 配置。
    /// </summary>
    public static EventStreamOptions Default =>
        new EventStreamOptions(
            segmentCapacity: 512,
            maxRetainedSegments: 4);
}
