using System.Runtime.CompilerServices;
using LayerBase.Layers;

namespace LayerBase.ECS;

/// <summary>
/// Layer 的 Blueprint 扩展方法。
/// </summary>
public static class LayerBlueprintExtensions
{
    /// <summary>
    /// 创建实体构建器，用于指定 Blueprint。
    /// </summary>
    /// <param name="layer">当前 Layer。</param>
    /// <returns>实体创建构建器。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityCreateBuilder CreateEntity(this Layer layer)
    {
        return new EntityCreateBuilder(layer.ECSWorld());
    }
}