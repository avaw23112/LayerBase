namespace LayerBase.Scope;

internal static class ScopeTypeRouteCache<TScope>
{
    // High 32 bits: RouteTable Generation
    // Low 32 bits: ScopeId
    private static long s_cachedEntry;

    public static bool TryGet(int generation, out int scopeId)
    {
        long entry = Volatile.Read(ref s_cachedEntry);
        if ((int)(entry >> 32) == generation)
        {
            scopeId = (int)(entry & 0xFFFFFFFF);
            return true;
        }

        scopeId = -1;
        return false;
    }

    public static void Set(int generation, int scopeId)
    {
        long entry = ((long)generation << 32) | (uint)scopeId;
        Interlocked.Exchange(ref s_cachedEntry, entry);
    }
}
