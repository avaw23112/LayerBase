namespace LayerBase.ECS;

public readonly record struct EcsRuntimeOptions(EcsQueryBatchOptions QueryBatch)
{
    public static EcsRuntimeOptions Default { get; } = new(EcsQueryBatchOptions.Default);
}
