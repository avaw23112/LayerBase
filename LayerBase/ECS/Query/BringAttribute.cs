namespace LayerBase.ECS;

/// <summary>
/// 标记一个 [Query] 方法要输出的 Actor 事件类型。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class BringAttribute : Attribute
{
    /// <summary>
    /// 当前 Query 方法要输出的 Actor 事件类型集合。
    /// </summary>
    public readonly Type[] EventTypes;

    /// <summary>
    /// 使用 typeof 语法指定事件类型。
    /// </summary>
    /// <param name="eventTypes">要输出的 Actor 事件类型。</param>
    public BringAttribute(params Type[] eventTypes)
    {
        EventTypes = eventTypes;
    }
}

/// <summary>
/// 泛型语法糖，指定 1 个事件类型。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BringAttribute<TEvent0> : BringAttribute
{
    public BringAttribute()
        : base(typeof(TEvent0))
    {
    }
}

/// <summary>
/// 泛型语法糖，指定 2 个事件类型。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BringAttribute<TEvent0, TEvent1> : BringAttribute
{
    public BringAttribute()
        : base(typeof(TEvent0), typeof(TEvent1))
    {
    }
}

/// <summary>
/// 泛型语法糖，指定 3 个事件类型。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BringAttribute<TEvent0, TEvent1, TEvent2> : BringAttribute
{
    public BringAttribute()
        : base(typeof(TEvent0), typeof(TEvent1), typeof(TEvent2))
    {
    }
}

/// <summary>
/// 泛型语法糖，指定 4 个事件类型。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BringAttribute<TEvent0, TEvent1, TEvent2, TEvent3> : BringAttribute
{
    public BringAttribute()
        : base(typeof(TEvent0), typeof(TEvent1), typeof(TEvent2), typeof(TEvent3))
    {
    }
}
