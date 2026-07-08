#nullable enable
using Arch.Core;
using LayerBase.ECS.Projection;

namespace LayerBase.ECS.Projection.Flow;

internal static class ProjectedActorRefComponentRegistration
{
    internal static readonly int ComponentId = ProjectedActorRefRegistration.ComponentType.Id;
}

public static class ProjectionWorldExtensions
{
    public static ProjectionQueryFlow0 Query(
        this World world)
    {
        return new ProjectionQueryFlow0(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate());
    }

    public static ProjectionQueryFlow1<T0> Query<T0>(
        this World world)
    {
        return new ProjectionQueryFlow1<T0>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0>());
    }

    public static ProjectionQueryFlow2<T0, T1> Query<T0, T1>(
        this World world)
    {
        return new ProjectionQueryFlow2<T0, T1>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1>());
    }

    public static ProjectionQueryFlow3<T0, T1, T2> Query<T0, T1, T2>(
        this World world)
    {
        return new ProjectionQueryFlow3<T0, T1, T2>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1, T2>());
    }

    public static ProjectionQueryFlow4<T0, T1, T2, T3> Query<T0, T1, T2, T3>(
        this World world)
    {
        return new ProjectionQueryFlow4<T0, T1, T2, T3>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1, T2, T3>());
    }

    public static ProjectionQueryFlow5<T0, T1, T2, T3, T4> Query<T0, T1, T2, T3, T4>(
        this World world)
    {
        return new ProjectionQueryFlow5<T0, T1, T2, T3, T4>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1, T2, T3, T4>());
    }

    public static ProjectionQueryFlow6<T0, T1, T2, T3, T4, T5> Query<T0, T1, T2, T3, T4, T5>(
        this World world)
    {
        return new ProjectionQueryFlow6<T0, T1, T2, T3, T4, T5>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1, T2, T3, T4, T5>());
    }

    public static ProjectionQueryFlow7<T0, T1, T2, T3, T4, T5, T6> Query<T0, T1, T2, T3, T4, T5, T6>(
        this World world)
    {
        return new ProjectionQueryFlow7<T0, T1, T2, T3, T4, T5, T6>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1, T2, T3, T4, T5, T6>());
    }

    public static ProjectionQueryFlow8<T0, T1, T2, T3, T4, T5, T6, T7> Query<T0, T1, T2, T3, T4, T5, T6, T7>(
        this World world)
    {
        return new ProjectionQueryFlow8<T0, T1, T2, T3, T4, T5, T6, T7>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1, T2, T3, T4, T5, T6, T7>());
    }

    public static ProjectionQueryFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
        this World world)
    {
        return new ProjectionQueryFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1, T2, T3, T4, T5, T6, T7, T8>());
    }

    public static ProjectionQueryFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        this World world)
    {
        return new ProjectionQueryFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>());
    }

    public static ProjectionQueryFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        this World world)
    {
        return new ProjectionQueryFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>());
    }

    public static ProjectionQueryFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
        this World world)
    {
        return new ProjectionQueryFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            world.Runtime,
            world.Runtime.EcsQueryRegistry.GetOrCreate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>());
    }

}
