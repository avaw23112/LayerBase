using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorTime
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long SecondsToTicks(
        float seconds)
    {
        if (seconds <= 0f)
        {
            return 0;
        }

        return (long)(Stopwatch.Frequency * seconds);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long BuildDeadline(
        long nowTicks,
        long keepAliveTicks)
    {
        return nowTicks + keepAliveTicks;
    }
}