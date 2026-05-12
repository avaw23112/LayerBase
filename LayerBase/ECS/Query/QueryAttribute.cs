namespace LayerBase.ECS;

/// <summary>
/// 标记一个方法为 ECS Query 入口。
/// 源生成器会根据此属性生成 Query + ForEach 或 Query + Bring + Post 代码。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryAttribute : Attribute
{
}
