#nullable enable
using Arch.Core;

namespace LayerBase.ECS.Projection.Create;

public static class EntityCreateWorldExtensions
{
    public static EntityCreateFlow0 CreateEntity(
        this World world)
    {
        Entity entity = world.Create();
        return new EntityCreateFlow0(world, entity);
    }

    public static EntityCreateFlow1<T0> CreateEntity<T0>(
        this World world,
        in T0 c0)
    {
        Entity entity = world.Create(c0);
        return new EntityCreateFlow1<T0>(world, entity);
    }

    public static EntityCreateFlow2<T0, T1> CreateEntity<T0, T1>(
        this World world,
        in T0 c0, in T1 c1)
    {
        Entity entity = world.Create(c0, c1);
        return new EntityCreateFlow2<T0, T1>(world, entity);
    }

    public static EntityCreateFlow3<T0, T1, T2> CreateEntity<T0, T1, T2>(
        this World world,
        in T0 c0, in T1 c1, in T2 c2)
    {
        Entity entity = world.Create(c0, c1, c2);
        return new EntityCreateFlow3<T0, T1, T2>(world, entity);
    }

    public static EntityCreateFlow4<T0, T1, T2, T3> CreateEntity<T0, T1, T2, T3>(
        this World world,
        in T0 c0, in T1 c1, in T2 c2, in T3 c3)
    {
        Entity entity = world.Create(c0, c1, c2, c3);
        return new EntityCreateFlow4<T0, T1, T2, T3>(world, entity);
    }

    public static EntityCreateFlow5<T0, T1, T2, T3, T4> CreateEntity<T0, T1, T2, T3, T4>(
        this World world,
        in T0 c0, in T1 c1, in T2 c2, in T3 c3, in T4 c4)
    {
        Entity entity = world.Create(c0, c1, c2, c3, c4);
        return new EntityCreateFlow5<T0, T1, T2, T3, T4>(world, entity);
    }

    public static EntityCreateFlow6<T0, T1, T2, T3, T4, T5> CreateEntity<T0, T1, T2, T3, T4, T5>(
        this World world,
        in T0 c0, in T1 c1, in T2 c2, in T3 c3, in T4 c4, in T5 c5)
    {
        Entity entity = world.Create(c0, c1, c2, c3, c4, c5);
        return new EntityCreateFlow6<T0, T1, T2, T3, T4, T5>(world, entity);
    }

    public static EntityCreateFlow7<T0, T1, T2, T3, T4, T5, T6> CreateEntity<T0, T1, T2, T3, T4, T5, T6>(
        this World world,
        in T0 c0, in T1 c1, in T2 c2, in T3 c3, in T4 c4, in T5 c5, in T6 c6)
    {
        Entity entity = world.Create(c0, c1, c2, c3, c4, c5, c6);
        return new EntityCreateFlow7<T0, T1, T2, T3, T4, T5, T6>(world, entity);
    }

    public static EntityCreateFlow8<T0, T1, T2, T3, T4, T5, T6, T7> CreateEntity<T0, T1, T2, T3, T4, T5, T6, T7>(
        this World world,
        in T0 c0, in T1 c1, in T2 c2, in T3 c3, in T4 c4, in T5 c5, in T6 c6, in T7 c7)
    {
        Entity entity = world.Create(c0, c1, c2, c3, c4, c5, c6, c7);
        return new EntityCreateFlow8<T0, T1, T2, T3, T4, T5, T6, T7>(world, entity);
    }

}
