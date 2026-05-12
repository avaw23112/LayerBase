using Arch.Core;
using LayerBase.Core;

namespace LayerBase.ECS.Projection.Flow;

public interface IQueryJob<T1>
    where T1 : struct, IComponent
{
    void Execute(
        Entity entity,
        ref T1 c1);
}

public interface IQueryJob<T1, T2>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    void Execute(
        Entity entity,
        ref T1 c1,
        ref T2 c2);
}

public interface IQueryJob<T1, T2, T3>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
{
    void Execute(
        Entity entity,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3);
}

public interface IQueryJob<T1, T2, T3, T4>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
{
    void Execute(
        Entity entity,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4);
}

public interface IQueryJob<T1, T2, T3, T4, T5>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
    where T5 : struct, IComponent
{
    void Execute(
        Entity entity,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5);
}

public interface IQueryJob<T1, T2, T3, T4, T5, T6>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
    where T5 : struct, IComponent
    where T6 : struct, IComponent
{
    void Execute(
        Entity entity,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6);
}

public interface IQueryJob<T1, T2, T3, T4, T5, T6, T7>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
    where T5 : struct, IComponent
    where T6 : struct, IComponent
    where T7 : struct, IComponent
{
    void Execute(
        Entity entity,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7);
}

public interface IQueryJob<T1, T2, T3, T4, T5, T6, T7, T8>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
    where T5 : struct, IComponent
    where T6 : struct, IComponent
    where T7 : struct, IComponent
    where T8 : struct, IComponent
{
    void Execute(
        Entity entity,
        ref T1 c1,
        ref T2 c2,
        ref T3 c3,
        ref T4 c4,
        ref T5 c5,
        ref T6 c6,
        ref T7 c7,
        ref T8 c8);
}
