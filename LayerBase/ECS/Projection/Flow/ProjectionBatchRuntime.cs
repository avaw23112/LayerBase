using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

internal static class ProjectionBatchRuntime
{
    public static ProjectionBatchBuffer<TEvent> RentBuffer<TEvent>(World world)
        where TEvent : struct
    {
        int autoFlushLimit = GetAutoFlushLimit(world, accessBytesPerEntity: 1);
        return ProjectionBatchBuffer<TEvent>.Rent(
            GetInitialCapacity(autoFlushLimit),
            world.ProjectedActorCommands,
            autoFlushLimit);
    }

    public static int GetAutoFlushLimit(World world, int accessBytesPerEntity)
    {
        EcsQueryBatchOptions options = world.EcsScheduler.BatchOptions;
        return options.EnableImplicitBatching
            ? options.ResolveBatchEntityCount(accessBytesPerEntity)
            : 0;
    }

    public static int GetInitialCapacity(int autoFlushLimit)
    {
        return autoFlushLimit > 0
            ? Math.Min(autoFlushLimit, 64)
            : 64;
    }
}
