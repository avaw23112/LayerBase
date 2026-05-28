using System;
using LayerBase.ECS.Projection;

namespace LayerBase.Actor;

/// <summary>
/// ActorOptionsAttribute 用于声明 ProjectedActor 的类型级默认策略。
///
/// 约束：
/// 1. 源生成器应在构建期读取该特性。
/// 2. 兼容旧路径时，RegisterGenerated 可以在冷路径读取一次该特性。
/// 3. Touch / Post / Sweep / Ensure 热路径绝不读取该特性。
/// 4. 事件投递、缓冲、背压仍由 EventMetaData 管理。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ActorOptionsAttribute : Attribute
{
    /// <summary>
    /// RetirePolicy 参数作用：
    /// 指定 Actor 失去兴趣并超过 KeepAlive 后的退场方式。
    /// </summary>
    public ProjectedActorRetirePolicy RetirePolicy { get; }

    /// <summary>
    /// CreatePolicy 参数作用：
    /// 指定 ProjectedActor 首次创建时机。
    /// </summary>
    public ProjectedActorCreatePolicy CreatePolicy { get; }

    /// <summary>
    /// KeepAliveSeconds 参数作用：
    /// Actor 最后一次兴趣命中后还能保持 Active 的秒数。
    /// </summary>
    public float KeepAliveSeconds { get; }

    /// <summary>
    /// TouchIntervalSeconds 参数作用：
    /// 两次真实 Touch 之间的最小间隔。
    /// </summary>
    public float TouchIntervalSeconds { get; }

    /// <summary>
    /// 构造 ActorOptionsAttribute。
    ///
    /// retirePolicy 参数作用：
    /// 指定失去兴趣后的处理方式。
    ///
    /// createPolicy 参数作用：
    /// 指定首次创建时机。
    ///
    /// keepAliveSeconds 参数作用：
    /// 指定兴趣保活时间。
    ///
    /// touchIntervalSeconds 参数作用：
    /// 指定 Touch 节流时间。
    /// </summary>
    public ActorOptionsAttribute(
        ProjectedActorRetirePolicy retirePolicy = ProjectedActorRetirePolicy.ReturnToPool,
        ProjectedActorCreatePolicy createPolicy = ProjectedActorCreatePolicy.Lazy,
        float keepAliveSeconds = 0.5f,
        float touchIntervalSeconds = 0.1f)
    {
        RetirePolicy = retirePolicy;
        CreatePolicy = createPolicy;
        KeepAliveSeconds = keepAliveSeconds;
        TouchIntervalSeconds = touchIntervalSeconds;
    }
}
