using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 高频投递扩展。
/// 作用：通过 ProjectedActorRef 快速投递事件，避免 TryGetProjectionMeta。
/// </summary>
public static class ProjectedActorPostExtensions
{
    /// <summary>
    /// 对带有 T1 / T2 / ProjectedActorRef 的实体批量投递事件。
    ///
    /// 类型参数说明：
    /// T1：第一个 ECS 组件类型，例如 Position。
    /// T2：第二个 ECS 组件类型，例如 Velocity。
    /// TEvent：投递给 Actor 的事件类型。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// actorWorld：ActorWorld。
    /// value：事件值。
    ///
    /// 作用：
    /// 直接通过 ProjectedActorRef.ActorId 投递，减少 Projection Lookup 成本。
    /// </summary>
    public static void PostProjected<T1, T2, TEvent>(
        this World world,
        ActorWorld actorWorld,
        in TEvent value)
        where T1 : struct
        where T2 : struct
        where TEvent : struct
    {
        var query = new QueryDescription()
            .WithAll<T1, T2, ProjectedActorRef>();

        // 捕获 value 到本地变量，避免 lambda 中使用 in 参数。
        TEvent capturedValue = value;

        world.Query(
            in query,
            (ref T1 c1, ref T2 c2, ref ProjectedActorRef actorRef) =>
            {
                if (!actorRef.IsValid)
                {
                    return;
                }

                actorWorld.PostTo(actorRef.ActorId, in capturedValue);
            });
    }

    /// <summary>
    /// 对带有 T1 / ProjectedActorRef 的实体批量投递事件。
    ///
    /// 类型参数说明：
    /// T1：第一个 ECS 组件类型，例如 Position。
    /// TEvent：投递给 Actor 的事件类型。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// actorWorld：ActorWorld。
    /// value：事件值。
    ///
    /// 作用：
    /// 直接通过 ProjectedActorRef.ActorId 投递，减少 Projection Lookup 成本。
    /// </summary>
    public static void PostProjected<T1, TEvent>(
        this World world,
        ActorWorld actorWorld,
        in TEvent value)
        where T1 : struct
        where TEvent : struct
    {
        var query = new QueryDescription()
            .WithAll<T1, ProjectedActorRef>();

        // 捕获 value 到本地变量，避免 lambda 中使用 in 参数。
        TEvent capturedValue = value;

        world.Query(
            in query,
            (ref T1 c1, ref ProjectedActorRef actorRef) =>
            {
                if (!actorRef.IsValid)
                {
                    return;
                }

                actorWorld.PostTo(actorRef.ActorId, in capturedValue);
            });
    }

    /// <summary>
    /// 对带有 ProjectedActorRef 的实体批量投递事件。
    ///
    /// 类型参数说明：
    /// TEvent：投递给 Actor 的事件类型。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// actorWorld：ActorWorld。
    /// value：事件值。
    ///
    /// 作用：
    /// 直接通过 ProjectedActorRef.ActorId 投递，减少 Projection Lookup 成本。
    /// </summary>
    public static void PostProjected<TEvent>(
        this World world,
        ActorWorld actorWorld,
        in TEvent value)
        where TEvent : struct
    {
        var query = new QueryDescription()
            .WithAll<ProjectedActorRef>();

        // 捕获 value 到本地变量，避免 lambda 中使用 in 参数。
        TEvent capturedValue = value;

        world.Query(
            in query,
            (ref ProjectedActorRef actorRef) =>
            {
                if (!actorRef.IsValid)
                {
                    return;
                }

                actorWorld.PostTo(actorRef.ActorId, in capturedValue);
            });
    }
}
