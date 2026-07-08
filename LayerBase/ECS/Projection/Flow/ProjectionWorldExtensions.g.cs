#nullable enable
using Arch.Core;
using LayerBase.ECS.Projection;

namespace LayerBase.ECS.Projection.Flow;

/// <summary>
/// 确保 ProjectedActorRef 组件类型在首次使用前注册。
/// </summary>
internal static class ProjectedActorRefComponentRegistration
{
    internal static readonly int ComponentId = ProjectedActorRefRegistration.ComponentType.Id;
}

public static class ProjectionWorldExtensions
{
    public static ProjectionQueryFlow0 Query(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // Projection 热路径必须组件。
        // 作用：保证进入 ProjectionExecutor 的 chunk 必然拥有 ActorId 缓存。
        description.WithAll<ProjectedActorRef>();

        return new ProjectionQueryFlow0(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow1<T0> Query<T0>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0>();

        return new ProjectionQueryFlow1<T0>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow2<T0, T1> Query<T0, T1>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1>();

        return new ProjectionQueryFlow2<T0, T1>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow3<T0, T1, T2> Query<T0, T1, T2>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1, T2>();

        return new ProjectionQueryFlow3<T0, T1, T2>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow4<T0, T1, T2, T3> Query<T0, T1, T2, T3>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1, T2, T3>();

        return new ProjectionQueryFlow4<T0, T1, T2, T3>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow5<T0, T1, T2, T3, T4> Query<T0, T1, T2, T3, T4>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1, T2, T3, T4>();

        return new ProjectionQueryFlow5<T0, T1, T2, T3, T4>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow6<T0, T1, T2, T3, T4, T5> Query<T0, T1, T2, T3, T4, T5>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1, T2, T3, T4, T5>();

        return new ProjectionQueryFlow6<T0, T1, T2, T3, T4, T5>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow7<T0, T1, T2, T3, T4, T5, T6> Query<T0, T1, T2, T3, T4, T5, T6>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1, T2, T3, T4, T5, T6>();

        return new ProjectionQueryFlow7<T0, T1, T2, T3, T4, T5, T6>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow8<T0, T1, T2, T3, T4, T5, T6, T7> Query<T0, T1, T2, T3, T4, T5, T6, T7>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1, T2, T3, T4, T5, T6, T7>();

        return new ProjectionQueryFlow8<T0, T1, T2, T3, T4, T5, T6, T7>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1, T2, T3, T4, T5, T6, T7, T8>();

        return new ProjectionQueryFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>();

        return new ProjectionQueryFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();

        return new ProjectionQueryFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            world,
            world.Query(in description));
    }

    public static ProjectionQueryFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>();

        return new ProjectionQueryFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            world,
            world.Query(in description));
    }

}
