namespace LayerBase.ECS;

public readonly record struct EcsQueryBatchOptions(
    bool EnableImplicitBatching,
    int DefaultBatchLimitBytes,
    int MinBatchEntityCount,
    int MaxBatchEntityCount)
{
    public static EcsQueryBatchOptions Default { get; } = new(
        EnableImplicitBatching: false,
        DefaultBatchLimitBytes: 512 * 1024,
        MinBatchEntityCount: 256,
        MaxBatchEntityCount: 32768);

    public int ResolveBatchEntityCount(int accessBytesPerEntity)
    {
        int safeAccessBytes = Math.Max(1, accessBytesPerEntity);
        int batchEntityCount = DefaultBatchLimitBytes / safeAccessBytes;
        return Math.Clamp(batchEntityCount, MinBatchEntityCount, MaxBatchEntityCount);
    }
}
