using System.Runtime.CompilerServices;
using LayerBase.DI;

namespace LayerBase.Scope.Extensions;

public static class ScopeExtensions
{
    // ── IService Extensions ──

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScopeAccessor Scope(this IService service)
    {
        var runtime = ScopeBindingResolver.ResolveRuntime(service);
        return new ScopeAccessor(runtime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScopeAccessor<TScope> Scope<TScope>(this IService service)
    {
        var runtime = ScopeBindingResolver.ResolveRuntime(service);
        var scopeId = ScopeTypeRouteCache<TScope>.Resolve(runtime);
        return new ScopeAccessor<TScope>(runtime, scopeId);
    }

    // ── ILayerContext Extensions ──

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScopeAccessor Scope(this ILayerContext context)
    {
        var runtime = ScopeBindingResolver.ResolveRuntime(context);
        return new ScopeAccessor(runtime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScopeAccessor<TScope> Scope<TScope>(this ILayerContext context)
    {
        var runtime = ScopeBindingResolver.ResolveRuntime(context);
        var scopeId = ScopeTypeRouteCache<TScope>.Resolve(runtime);
        return new ScopeAccessor<TScope>(runtime, scopeId);
    }
}

/// <summary>
/// ScopeId 路由缓存，避免每帧通过 Type 查找。
/// </summary>
internal static class ScopeTypeRouteCache<TScope>
{
    public static int Resolve(LayerRuntime runtime)
    {
        if (runtime.ScopeHost == null)
            return -1;

        if (runtime.ScopeHost.Routes.TryGetScopeId<TScope>(out int scopeId))
        {
            return scopeId;
        }

        return -1;
    }
}
