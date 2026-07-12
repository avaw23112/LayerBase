namespace LayerBase.Scope;

internal static class ScopeTypeRouteCache<TScope>
{
    private static int s_generation;
    private static int s_scopeId;

    public static bool TryGet(int generation, out int scopeId)
    {
        if (Volatile.Read(ref s_generation) == generation)
        {
            scopeId = Volatile.Read(ref s_scopeId);
            return true;
        }

        scopeId = -1;
        return false;
    }

    public static void Set(int generation, int scopeId)
    {
        Volatile.Write(ref s_scopeId, scopeId);
        Volatile.Write(ref s_generation, generation);
    }
}
