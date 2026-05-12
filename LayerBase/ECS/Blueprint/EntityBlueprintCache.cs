using System.Runtime.CompilerServices;

namespace LayerBase.ECS;

/// <summary>
/// Blueprint 构建结果缓存。
/// 每个 TBlueprint 只构建一次。
/// </summary>
/// <typeparam name="TBlueprint">当前实体使用的 Blueprint 类型。</typeparam>
public static class EntityBlueprintCache<TBlueprint>
    where TBlueprint : class, IEntityBlueprint, new()
{
    private static readonly EntityBlueprint s_blueprint = Build();

    /// <summary>
    /// 获取或构建 Blueprint。
    /// </summary>
    /// <returns>缓存的 EntityBlueprint。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityBlueprint GetOrBuild()
    {
        return s_blueprint;
    }

    private static EntityBlueprint Build()
    {
        var builder = new EntityBlueprintBuilder();
        BlueprintUnitCache<TBlueprint>.Config(ref builder);
        return builder.Build();
    }
}