using LayerBase.Modules;

namespace LayerBase.Scope;

public static class ModuleCatalogRegistry
{
    private static ILayerBaseModule[]? s_modules;

    public static void Register(ILayerBaseModule[] modules)
    {
        s_modules = modules;
    }

    public static ILayerBaseModule[]? GetAllModules()
    {
        return s_modules;
    }

    public static bool IsAvailable => s_modules != null && s_modules.Length > 0;
}
