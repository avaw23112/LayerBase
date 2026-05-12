using System.Runtime.CompilerServices;
using LayerBase.DI;

namespace LayerBase.ECS;

/// <summary>
/// ILayerContext 的 Blueprint 扩展方法。
/// </summary>
public static class ContextBlueprintExtensions
{
    /// <summary>
    /// 创建实体构建器，用于指定 Blueprint。
    /// </summary>
    /// <param name="context">当前 ILayerContext。</param>
    /// <returns>实体创建构建器。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityCreateBuilder CreateEntity(this ILayerContext context)
    {
         return new EntityCreateBuilder(context.ECSWorld());
    }
}
