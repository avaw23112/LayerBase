using System.Runtime.CompilerServices;
using Arch.Core;

namespace LayerBase.ECS;

/// <summary>
/// World 的 Blueprint 扩展方法。
/// </summary>
public static class WorldBlueprintExtensions
{
    /// <summary>
    /// 创建实体构建器，用于指定 Blueprint。
    /// </summary>
    /// <param name="world">当前 ECS World。</param>
    /// <returns>实体创建构建器。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityCreateBuilder CreateEntity(this World world)
    {
        return new EntityCreateBuilder(world);
    }
}

