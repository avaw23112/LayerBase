namespace LayerBase.ECS;

/// <summary>
/// 标记一个 class 是 Blueprint。
/// 供 Roslyn 分析器、源生成器、Agent 索引器识别。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LayerBlueprintAttribute : Attribute
{
}
