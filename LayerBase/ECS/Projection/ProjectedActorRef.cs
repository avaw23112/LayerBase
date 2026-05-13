using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 的公开 ActorId 缓存组件。
///
/// 作用：
/// 1. 让业务 ECS Query 可以直接拿到 ActorId。
/// 2. 避免每帧通过 Entity 反查 ProjectedActorMeta。
/// 3. 不暴露 internal ProjectedActorMeta。
/// </summary>
public struct ProjectedActorRef
{
    /// <summary>
    /// 当前 Entity 绑定的 ActorId。
    /// ActorId 是 ActorWorld 中定位 Actor 的轻量句柄。
    /// </summary>
    public ActorId ActorId;

    /// <summary>
    /// 当前 ActorId 是否有效。
    /// </summary>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ActorId.IsValid;
    }

    /// <summary>
    /// 构造 ProjectedActorRef。
    ///
    /// 参数说明：
    /// actorId：当前 Entity 对应的 ActorId。
    /// </summary>
    public ProjectedActorRef(ActorId actorId)
    {
        ActorId = actorId;
    }
}
