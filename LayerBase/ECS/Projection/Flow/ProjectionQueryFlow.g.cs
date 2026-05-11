#nullable enable
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

public readonly struct ProjectionQueryFlow1<T0>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionQueryFlow1(World world, Query query, ProjectionPredicate<T0>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow1<T0> Where(ProjectionPredicate<T0> predicate)
    {
        return new ProjectionQueryFlow1<T0>(_world, _query, predicate);
    }

    public ProjectionBringFlow1<T0, TEvent> Bring<TEvent>()
        where TEvent : struct
    {
        return new ProjectionBringFlow1<T0, TEvent>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor1<T0>.Touch(_world, _query, _predicate);
    }
}

public readonly struct ProjectionBringFlow1<T0, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1<T0, TEvent> ForEach(ProjectionForEach<T0, TEvent> forEach)
    {
        return new ProjectionPostFlow1<T0, TEvent>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow1<T0, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach<T0, TEvent> _forEach;

    internal ProjectionPostFlow1(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach<T0, TEvent> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1<T0, TEvent> Batch()
    {
        return this;
    }

    public void Post()
    {
        ProjectionExecutor1<T0>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionQueryFlow2<T0, T1>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionQueryFlow2(World world, Query query, ProjectionPredicate<T0, T1>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow2<T0, T1> Where(ProjectionPredicate<T0, T1> predicate)
    {
        return new ProjectionQueryFlow2<T0, T1>(_world, _query, predicate);
    }

    public ProjectionBringFlow2<T0, T1, TEvent> Bring<TEvent>()
        where TEvent : struct
    {
        return new ProjectionBringFlow2<T0, T1, TEvent>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor2<T0, T1>.Touch(_world, _query, _predicate);
    }
}

public readonly struct ProjectionBringFlow2<T0, T1, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2<T0, T1, TEvent> ForEach(ProjectionForEach<T0, T1, TEvent> forEach)
    {
        return new ProjectionPostFlow2<T0, T1, TEvent>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow2<T0, T1, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach<T0, T1, TEvent> _forEach;

    internal ProjectionPostFlow2(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach<T0, T1, TEvent> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2<T0, T1, TEvent> Batch()
    {
        return this;
    }

    public void Post()
    {
        ProjectionExecutor2<T0, T1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionQueryFlow3<T0, T1, T2>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionQueryFlow3(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow3<T0, T1, T2> Where(ProjectionPredicate<T0, T1, T2> predicate)
    {
        return new ProjectionQueryFlow3<T0, T1, T2>(_world, _query, predicate);
    }

    public ProjectionBringFlow3<T0, T1, T2, TEvent> Bring<TEvent>()
        where TEvent : struct
    {
        return new ProjectionBringFlow3<T0, T1, T2, TEvent>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor3<T0, T1, T2>.Touch(_world, _query, _predicate);
    }
}

public readonly struct ProjectionBringFlow3<T0, T1, T2, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3<T0, T1, T2, TEvent> ForEach(ProjectionForEach<T0, T1, T2, TEvent> forEach)
    {
        return new ProjectionPostFlow3<T0, T1, T2, TEvent>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow3<T0, T1, T2, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, TEvent> _forEach;

    internal ProjectionPostFlow3(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach<T0, T1, T2, TEvent> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3<T0, T1, T2, TEvent> Batch()
    {
        return this;
    }

    public void Post()
    {
        ProjectionExecutor3<T0, T1, T2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionQueryFlow4<T0, T1, T2, T3>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionQueryFlow4(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow4<T0, T1, T2, T3> Where(ProjectionPredicate<T0, T1, T2, T3> predicate)
    {
        return new ProjectionQueryFlow4<T0, T1, T2, T3>(_world, _query, predicate);
    }

    public ProjectionBringFlow4<T0, T1, T2, T3, TEvent> Bring<TEvent>()
        where TEvent : struct
    {
        return new ProjectionBringFlow4<T0, T1, T2, T3, TEvent>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor4<T0, T1, T2, T3>.Touch(_world, _query, _predicate);
    }
}

public readonly struct ProjectionBringFlow4<T0, T1, T2, T3, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4<T0, T1, T2, T3, TEvent> ForEach(ProjectionForEach<T0, T1, T2, T3, TEvent> forEach)
    {
        return new ProjectionPostFlow4<T0, T1, T2, T3, TEvent>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow4<T0, T1, T2, T3, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, TEvent> _forEach;

    internal ProjectionPostFlow4(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach<T0, T1, T2, T3, TEvent> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4<T0, T1, T2, T3, TEvent> Batch()
    {
        return this;
    }

    public void Post()
    {
        ProjectionExecutor4<T0, T1, T2, T3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionQueryFlow5<T0, T1, T2, T3, T4>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionQueryFlow5(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow5<T0, T1, T2, T3, T4> Where(ProjectionPredicate<T0, T1, T2, T3, T4> predicate)
    {
        return new ProjectionQueryFlow5<T0, T1, T2, T3, T4>(_world, _query, predicate);
    }

    public ProjectionBringFlow5<T0, T1, T2, T3, T4, TEvent> Bring<TEvent>()
        where TEvent : struct
    {
        return new ProjectionBringFlow5<T0, T1, T2, T3, T4, TEvent>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor5<T0, T1, T2, T3, T4>.Touch(_world, _query, _predicate);
    }
}

public readonly struct ProjectionBringFlow5<T0, T1, T2, T3, T4, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5<T0, T1, T2, T3, T4, TEvent> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, TEvent> forEach)
    {
        return new ProjectionPostFlow5<T0, T1, T2, T3, T4, TEvent>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow5<T0, T1, T2, T3, T4, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, TEvent> _forEach;

    internal ProjectionPostFlow5(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, TEvent> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5<T0, T1, T2, T3, T4, TEvent> Batch()
    {
        return this;
    }

    public void Post()
    {
        ProjectionExecutor5<T0, T1, T2, T3, T4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionQueryFlow6<T0, T1, T2, T3, T4, T5>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionQueryFlow6(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow6<T0, T1, T2, T3, T4, T5> Where(ProjectionPredicate<T0, T1, T2, T3, T4, T5> predicate)
    {
        return new ProjectionQueryFlow6<T0, T1, T2, T3, T4, T5>(_world, _query, predicate);
    }

    public ProjectionBringFlow6<T0, T1, T2, T3, T4, T5, TEvent> Bring<TEvent>()
        where TEvent : struct
    {
        return new ProjectionBringFlow6<T0, T1, T2, T3, T4, T5, TEvent>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.Touch(_world, _query, _predicate);
    }
}

public readonly struct ProjectionBringFlow6<T0, T1, T2, T3, T4, T5, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6<T0, T1, T2, T3, T4, T5, TEvent> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, T5, TEvent> forEach)
    {
        return new ProjectionPostFlow6<T0, T1, T2, T3, T4, T5, TEvent>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow6<T0, T1, T2, T3, T4, T5, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, T5, TEvent> _forEach;

    internal ProjectionPostFlow6(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, T5, TEvent> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6<T0, T1, T2, T3, T4, T5, TEvent> Batch()
    {
        return this;
    }

    public void Post()
    {
        ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionQueryFlow7<T0, T1, T2, T3, T4, T5, T6>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionQueryFlow7(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow7<T0, T1, T2, T3, T4, T5, T6> Where(ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6> predicate)
    {
        return new ProjectionQueryFlow7<T0, T1, T2, T3, T4, T5, T6>(_world, _query, predicate);
    }

    public ProjectionBringFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent> Bring<TEvent>()
        where TEvent : struct
    {
        return new ProjectionBringFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.Touch(_world, _query, _predicate);
    }
}

public readonly struct ProjectionBringFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, TEvent> forEach)
    {
        return new ProjectionPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, TEvent> _forEach;

    internal ProjectionPostFlow7(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, TEvent> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent> Batch()
    {
        return this;
    }

    public void Post()
    {
        ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionQueryFlow8<T0, T1, T2, T3, T4, T5, T6, T7>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionQueryFlow8(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow8<T0, T1, T2, T3, T4, T5, T6, T7> Where(ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7> predicate)
    {
        return new ProjectionQueryFlow8<T0, T1, T2, T3, T4, T5, T6, T7>(_world, _query, predicate);
    }

    public ProjectionBringFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent> Bring<TEvent>()
        where TEvent : struct
    {
        return new ProjectionBringFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.Touch(_world, _query, _predicate);
    }
}

public readonly struct ProjectionBringFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, TEvent> forEach)
    {
        return new ProjectionPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent>
    where TEvent : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, TEvent> _forEach;

    internal ProjectionPostFlow8(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, TEvent> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent> Batch()
    {
        return this;
    }

    public void Post()
    {
        ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.Post(_world, _query, _predicate, _forEach);
    }
}
