using System.Runtime.CompilerServices;
using LayerBase.DI;

namespace LayerBase.ECS;

/// <summary>
/// Service 的 Blueprint 扩展方法。
/// </summary>
public static class ServiceBlueprintExtensions
{
    /// <summary>
    /// 创建实体构建器，用于指定 Blueprint。
    /// </summary>
    /// <param name="service">当前 Service。</param>
    /// <returns>实体创建构建器。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityCreateBuilder CreateEntity(this IService service)
    {
        return new EntityCreateBuilder(service.ECSWorld());
    }
}