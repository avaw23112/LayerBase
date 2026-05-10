using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

internal static class ActorPostRouteUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsRouteInMask(byte routeCode, uint mask)
    {
        return routeCode < 32
               && ((mask >> routeCode) & 1u) != 0;
    }
}
