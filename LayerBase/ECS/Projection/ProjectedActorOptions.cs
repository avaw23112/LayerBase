using System.Runtime.CompilerServices;

namespace LayerBase.ECS.Projection;

internal readonly struct ProjectedActorOptions
{
    public readonly ProjectedActorRetirePolicy RetirePolicy;
    public readonly ProjectedActorCreatePolicy CreatePolicy;
    public readonly long KeepAliveTicks;
    public readonly long TouchIntervalTicks;

    public static ProjectedActorOptions Default =>
        new(
            ProjectedActorRetirePolicy.ReturnToPool,
            ProjectedActorCreatePolicy.Lazy,
            ProjectedActorTime.SecondsToTicks(0.5f),
            ProjectedActorTime.SecondsToTicks(0.1f));

    public ProjectedActorOptions(
        ProjectedActorRetirePolicy retirePolicy,
        ProjectedActorCreatePolicy createPolicy,
        long keepAliveTicks,
        long touchIntervalTicks)
    {
        RetirePolicy = retirePolicy;
        CreatePolicy = createPolicy;
        KeepAliveTicks = keepAliveTicks;
        TouchIntervalTicks = touchIntervalTicks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ProjectedActorOptions FromAttribute(
        ProjectedActorRetirePolicy retirePolicy,
        ProjectedActorCreatePolicy createPolicy,
        float keepAliveSeconds,
        float touchIntervalSeconds)
    {
        return new ProjectedActorOptions(
            retirePolicy,
            createPolicy,
            ProjectedActorTime.SecondsToTicks(keepAliveSeconds),
            ProjectedActorTime.SecondsToTicks(touchIntervalSeconds));
    }
}
