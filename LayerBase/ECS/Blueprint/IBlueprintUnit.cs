namespace LayerBase.ECS;

/// <summary>
/// Blueprint / Bundle 的公共接口。
/// </summary>
public interface IBlueprintUnit
{
    /// <summary>
    /// 配置实体结构。
    /// </summary>
    /// <param name="builder">当前实体蓝图构建器。</param>
    void Config(ref EntityBlueprintBuilder builder);
}