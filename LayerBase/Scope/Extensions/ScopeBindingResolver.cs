using LayerBase.DI;

namespace LayerBase.Scope.Extensions;

/// <summary>
/// 从 IService / ILayerContext 的绑定中解析 LayerRuntime。
/// 优先使用 ILayerBindingAccessor，避免反射和 Dictionary 查找。
/// </summary>
internal static class ScopeBindingResolver
{
    public static LayerRuntime ResolveRuntime(object obj)
    {
        if (obj is ILayerBindingAccessor accessor &&
            accessor.__LayerBaseBinding is ServiceLayerBinding binding &&
            binding.Layer != null)
        {
            return binding.Layer.OwnerContext
                ?? throw new InvalidOperationException(
                    $"'{obj.GetType().FullName}' is bound to a Layer that is not attached to any runtime.");
        }

        // Fallback: use ServiceLayerBinder (uses ConditionalWeakTable internally)
        var layer = ServiceLayerBinder.RequireBinding(obj).Layer;
        return layer.OwnerContext
            ?? throw new InvalidOperationException(
                $"'{obj.GetType().FullName}' is not attached to any runtime.");
    }
}

// Internal accessor for ServiceLayerBinding
public interface ILayerRuntimeBinding
{
    LayerRuntime Runtime { get; }
}
