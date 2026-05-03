namespace LayerBase.Async;

/// <summary>
/// CompletionQueue Drain 的统计结果。
/// </summary>
public readonly struct CompletionDrainStats
{
    /// <summary>
    /// 创建 CompletionQueue Drain 统计结果。
    /// </summary>
    /// <param name="processed">
    /// 本次成功执行或已处理的 completion 数量。
    /// </param>
    /// <param name="errors">
    /// 本次 Drain 捕获到的异常数量。
    /// </param>
    /// <param name="remaining">
    /// 本次 Drain 后仍留在队列中的 completion 数量。
    /// </param>
    public CompletionDrainStats(int processed, int errors, int remaining)
    {
        Processed = processed;
        Errors = errors;
        Remaining = remaining;
    }

    public int Processed { get; }
    public int Errors { get; }
    public int Remaining { get; }
}
