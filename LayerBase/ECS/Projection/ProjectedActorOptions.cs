using System.Runtime.CompilerServices;

namespace LayerBase.ECS.Projection;

/// <summary>
/// ProjectedActorOptions 是 ActorOptionsAttribute 转换后的运行时缓存数据。
///
/// 约束：
/// 1. Touch / Post / Sweep / Ensure 只读该结构。
/// 2. 不在热路径读取 Attribute。
/// 3. 不在热路径使用 Dictionary。
/// </summary>
internal readonly struct ProjectedActorOptions
{
    /// <summary>
    /// RetirePolicy 字段作用：
    /// Actor 失去兴趣后的退场方式。
    /// </summary>
    public readonly ProjectedActorRetirePolicy RetirePolicy;

    /// <summary>
    /// CreatePolicy 字段作用：
    /// Actor 首次创建时机。
    /// </summary>
    public readonly ProjectedActorCreatePolicy CreatePolicy;

    /// <summary>
    /// KeepAliveTicks 字段作用：
    /// 最后一次兴趣命中后的保活时长。
    /// </summary>
    public readonly long KeepAliveTicks;

    /// <summary>
    /// TouchIntervalTicks 字段作用：
    /// 两次真实 Touch 之间的最小间隔。
    /// </summary>
    public readonly long TouchIntervalTicks;

    /// <summary>
    /// Default 属性作用：
    /// 未配置 ActorOptionsAttribute 时的默认策略。
    /// </summary>
    public static ProjectedActorOptions Default =>
        new ProjectedActorOptions(
            ProjectedActorRetirePolicy.ReturnToPool,
            ProjectedActorCreatePolicy.Lazy,
            ProjectedActorTime.SecondsToTicks(0.5f),
            ProjectedActorTime.SecondsToTicks(0.1f));

    /// <summary>
    /// 构造 ProjectedActorOptions。
    ///
    /// retirePolicy 参数作用：
    /// Actor 失去兴趣后的退场方式。
    ///
    /// createPolicy 参数作用：
    /// Actor 首次创建时机。
    ///
    /// keepAliveTicks 参数作用：
    /// 兴趣保活时长。
    ///
    /// touchIntervalTicks 参数作用：
    /// Touch 节流时长。
    /// </summary>
    public ProjectedActorOptions(
        ProjectedActorRetirePolicy retirePolicy,
        ProjectedActorCreatePolicy createPolicy,
        long keepAliveTicks,
        long touchIntervalTicks)
    {
        RetirePolicy = retirePolicy;
        CreatePolicy = createPolicy;
        KeepAliveTicks = keepAliveTicks;
        TouchIntervalTicks = touchIntervalTicks;
    }

    /// <summary>
    /// 从 ActorOptionsAttribute 构建 ProjectedActorOptions。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ProjectedActorOptions FromAttribute(
        ProjectedActorRetirePolicy retirePolicy,
        ProjectedActorCreatePolicy createPolicy,
        float keepAliveSeconds,
        float touchIntervalSeconds)
    {
        return new ProjectedActorOptions(
            retirePolicy,
            createPolicy,
            ProjectedActorTime.SecondsToTicks(keepAliveSeconds),
            ProjectedActorTime.SecondsToTicks(touchIntervalSeconds));
    }
}
