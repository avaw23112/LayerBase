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
        if (ScopeObjectBinder.TryGet(obj, out ScopeObjectBinding? binding) &&
            binding.Runtime != null)
        {
            return binding.Runtime;
        }

        return ServiceLayerBinder.RequireBinding(obj).Runtime;
    }
}

// Internal accessor for ServiceLayerBinding
public interface ILayerRuntimeBinding
{
    LayerRuntime Runtime { get; }
}
