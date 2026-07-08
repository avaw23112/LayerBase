using Arch.Core;

namespace LayerBase.ECS.Runtime;

internal static class EcsSchedulerUtility
{
    public static bool ScheduleIfNeeded(World world, string debugName, Action<World> execute)
    {
        var scheduler = (IEcsWorkScheduler)world.Runtime.EcsScheduler;

        if (scheduler.Mode != EcsExecutionMode.Async || scheduler.IsSchedulerThread)
        {
            return false;
        }

        scheduler.Schedule(new DelegateEcsWorkItem(debugName, execute));
        return true;
    }
}
