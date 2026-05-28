namespace LayerBase.Actor;

/// <summary>
/// IPooledActor 表示可被对象池复用的 Actor。
///
/// 设计边界：
/// 1. Actor 只负责自己的池化生命周期。
/// 2. Projection 的兴趣保活时间由 ProjectedActorRef 维护。
/// 3. Actor 不再暴露 RecycleDeadlineTicks，避免 Actor 合同和 Projection 生命周期耦合。
/// </summary>
public interface IPooledActor : IActor
{
    /// <summary>
    /// OnRent 作用：
    /// Actor 从对象池租出时调用。
    /// 适合执行完整初始化。
    /// </summary>
    void OnRent();

    /// <summary>
    /// OnReturn 作用：
    /// Actor 归还对象池前调用。
    /// 适合执行完整清理。
    /// </summary>
    void OnReturn();

    /// <summary>
    /// OnEnable 作用：
    /// Actor 从 Disabled 恢复到 Active 时调用。
    /// 适合执行轻量恢复。
    /// </summary>
    void OnEnable();

    /// <summary>
    /// OnDisable 作用：
    /// Actor 从 Active 进入 Disabled 时调用。
    /// 适合执行轻量挂起。
    /// </summary>
    void OnDisable();
}
