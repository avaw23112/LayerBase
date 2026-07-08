using Arch.Core;
using System.Runtime.CompilerServices;
using ArchQuery = Arch.Core.Query;

namespace LayerBase.ECS.Runtime;

internal static class EcsSchedulerUtility
{
    public static bool RecordPlainQueryIfNeeded<TJob>(
        World world,
        ArchQuery query,
        object? predicate,
        in TJob job,
        int executorId)
        where TJob : struct
    {
        var scheduler = (IEcsWorkScheduler)world.Runtime.EcsScheduler;

        if (scheduler.Mode != EcsExecutionMode.Async ||
            scheduler.IsSchedulerThread ||
            RuntimeHelpers.IsReferenceOrContainsReferences<TJob>())
        {
            return false;
        }

        ((AsyncEcsScheduler)scheduler).RecordPlainQuery(
            executorId,
            query,
            predicate,
            in job);
        return true;
    }

    public static bool ScheduleIfNeeded(World world, string debugName, Action<World> execute)
    {
        var scheduler = (IEcsWorkScheduler)world.Runtime.EcsScheduler;

        if (scheduler.Mode != EcsExecutionMode.Async || scheduler.IsSchedulerThread)
        {
            return false;
        }

        scheduler.Schedule(PooledEcsWorkItem<Action<World>>.Rent(
            debugName,
            execute,
            static (scheduledWorld, scheduledExecute) => scheduledExecute(scheduledWorld)));
        return true;
    }

    public static bool ScheduleIfNeeded<TState>(
        World world,
        string debugName,
        in TState state,
        Action<World, TState> execute)
    {
        var scheduler = (IEcsWorkScheduler)world.Runtime.EcsScheduler;

        if (scheduler.Mode != EcsExecutionMode.Async || scheduler.IsSchedulerThread)
        {
            return false;
        }

        scheduler.Schedule(PooledEcsWorkItem<TState>.Rent(debugName, in state, execute));
        return true;
    }
}
