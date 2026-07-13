using Arch.Core;

namespace LayerBase.ECS.Runtime;

internal static class EcsSchedulerUtility
{
    public static bool ScheduleIfNeeded(World world, string debugName, Action<World> execute)
    {
        if (!world.TryGetEcsWorkScheduler(out IEcsWorkScheduler? scheduler) ||
            scheduler == null)
        {
            return false;
        }

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
        if (!world.TryGetEcsWorkScheduler(out IEcsWorkScheduler? scheduler) ||
            scheduler == null)
        {
            return false;
        }

        if (scheduler.Mode != EcsExecutionMode.Async || scheduler.IsSchedulerThread)
        {
            return false;
        }

        scheduler.Schedule(PooledEcsWorkItem<TState>.Rent(debugName, in state, execute));
        return true;
    }
}
