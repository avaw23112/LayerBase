namespace LayerBase.ECS.Projection;

/// <summary>
/// ProjectedActorRetirePolicy 表示 ProjectedActor 失去兴趣后的退场方式。
/// </summary>
public enum ProjectedActorRetirePolicy : byte
{
    /// <summary>
    /// Disable 参数作用：
    /// Actor 失去兴趣后只进入 Disabled 状态。
    /// 不调用 OnReturn，不清理 ActorId，不改变 Entity 绑定。
    /// </summary>
    Disable = 0,

    /// <summary>
    /// ReturnToPool 参数作用：
    /// Actor 失去兴趣后归还对象池。
    /// 会调用 OnReturn，下次重新命中时会调用 OnRent。
    /// </summary>
    ReturnToPool = 1,

    /// <summary>
    /// DestroyImmediately 参数作用：
    /// Actor 失去兴趣后直接销毁。
    /// </summary>
    DestroyImmediately = 2,

    /// <summary>
    /// DetachAndLetActorFinish 参数作用：
    /// Entity 与 Actor 解绑，但允许 Actor 自行完成剩余事件或收尾逻辑。
    /// </summary>
    DetachAndLetActorFinish = 3
}

/// <summary>
/// ProjectedActorCreatePolicy 表示 ProjectedActor 首次创建时机。
/// </summary>
public enum ProjectedActorCreatePolicy : byte
{
    /// <summary>
    /// Lazy 参数作用：
    /// WithProjectedActor 时只写配置，首次 Touch / Post 时创建 Actor。
    /// </summary>
    Lazy = 0,

    /// <summary>
    /// OnMark 参数作用：
    /// WithProjectedActor 时立即创建 Actor。
    /// 这里的 Mark 是内部投影资格写入，不是业务 API。
    /// </summary>
    OnMark = 1
}
