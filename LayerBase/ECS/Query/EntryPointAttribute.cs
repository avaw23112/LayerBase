namespace LayerBase.ECS;

/// <summary>
/// 指定 [Query] 方法生成的入口方法名。
/// 如果未指定，则使用去掉 "On" 前缀的方法名。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EntryPointAttribute : Attribute
{
    /// <summary>
    /// 生成的入口方法名。
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// 指定源生成器生成的 Query 入口方法名。
    /// </summary>
    /// <param name="name">入口方法名，不能为 null 或空白。</param>
    /// <exception cref="ArgumentException">name 为 null 或空白时抛出。</exception>
    public EntryPointAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "EntryPoint name cannot be null or whitespace.",
                nameof(name));
        }

        Name = name;
    }
}