#nullable enable
using Arch.Core;
using LayerBase.ECS;

namespace LayerBase.ECS.Projection.Flow;

public readonly struct ProjectionQueryFlow0
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionQueryFlow0(World world, Query query, ProjectionPredicate? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow0 Where(ProjectionPredicate predicate)
    {
        return new ProjectionQueryFlow0(_world, _query, predicate);
    }

    public ProjectionBringFlow0<TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow0<TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_2e<TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow0_2e<TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_3e<TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow0_3e<TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_4e<TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow0_4e<TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_5e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow0_5e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_6e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow0_6e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_7e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow0_7e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_8e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow0_8e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_9e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow0_9e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_10e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow0_10e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_11e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow0_11e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow0_12e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow0_12e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor0.Touch(_world, _query, _predicate);
    }
}

public readonly struct ProjectionBringFlow0<TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0<TEvent0> ForEach(ProjectionForEach<TEvent0> forEach)
    {
        return new ProjectionPostFlow0<TEvent0>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0<TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach<TEvent0> _forEach;

    internal ProjectionPostFlow0(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach<TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0<TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_2e<TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_2e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_2e<TEvent0, TEvent1> ForEach(ProjectionForEach2<TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow0_2e<TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_2e<TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach2<TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow0_2e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach2<TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_2e<TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_2E<TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_3e<TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_3e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_3e<TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow0_3e<TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_3e<TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach3<TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow0_3e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach3<TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_3e<TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_3E<TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_4e<TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_4e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_4e<TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow0_4e<TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_4e<TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach4<TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow0_4e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach4<TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_4e<TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_4E<TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_5e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_5e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_5e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow0_5e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_5e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach5<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow0_5e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach5<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_5e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_5E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_6e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_6e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_6e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow0_6e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_6e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach6<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow0_6e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach6<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_6e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_6E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_7e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_7e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_7e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow0_7e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_7e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach7<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow0_7e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach7<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_7e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_7E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_8e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_8e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_8e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow0_8e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_8e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach8<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow0_8e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach8<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_8e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_8E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_9e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_9e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_9e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow0_9e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_9e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach9<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow0_9e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach9<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_9e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_9E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_10e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_10e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_10e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow0_10e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_10e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach10<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow0_10e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach10<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_10e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_10E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_11e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_11e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_11e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow0_11e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_11e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach11<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow0_11e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach11<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_11e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_11E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionBringFlow0_12e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;

    internal ProjectionBringFlow0_12e(World world, Query query, ProjectionPredicate? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow0_12e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow0_12e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }
}

public readonly struct ProjectionPostFlow0_12e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate? _predicate;
    private readonly ProjectionForEach12<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow0_12e(World world, Query query, ProjectionPredicate? predicate, ProjectionForEach12<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow0_12e<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor0_12E<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

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

    public ProjectionBringFlow1<T0, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow1<T0, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_2e<T0, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow1_2e<T0, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_3e<T0, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow1_3e<T0, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor1<T0>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0>
    {
        ProjectionExecutor1<T0>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1<T0, TEvent0>
where TEvent0 : struct
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

    public ProjectionPostFlow1<T0, TEvent0> ForEach(ProjectionForEach<T0, TEvent0> forEach)
    {
        return new ProjectionPostFlow1<T0, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1<T0, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x1<T0, TEvent0>
    {
        return new ProjectionJobPostFlow1<T0, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1<T0, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach<T0, TEvent0> _forEach;

    internal ProjectionPostFlow1(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach<T0, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1<T0, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1<T0>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1<T0, TEvent0, TJob>
    where TJob : struct, IProjectionJob1x1<T0, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1<T0, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1<T0>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_2e<T0, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_2e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_2e<T0, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow1_2e<T0, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_2e<T0, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x2<T0, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow1_2e<T0, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_2e<T0, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach2<T0, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow1_2e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach2<T0, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_2e<T0, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_2E<T0, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_2e<T0, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob1x2<T0, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_2e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_2e<T0, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_2E<T0, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_3e<T0, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_3e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_3e<T0, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow1_3e<T0, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_3e<T0, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x3<T0, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow1_3e<T0, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_3e<T0, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach3<T0, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow1_3e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach3<T0, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_3e<T0, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_3E<T0, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_3e<T0, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob1x3<T0, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_3e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_3e<T0, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_3E<T0, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_4e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x4<T0, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach4<T0, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow1_4e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach4<T0, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_4E<T0, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob1x4<T0, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_4e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_4e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_4E<T0, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_5e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow1_5e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_5E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob1x5<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_5e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_5e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_5E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_6e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow1_6e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_6E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob1x6<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_6e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_6e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_6E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_7e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow1_7e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_7E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob1x7<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_7e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_7e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_7E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_8e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow1_8e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_8E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob1x8<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_8e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_8e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_8E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_9e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow1_9e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_9E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob1x9<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_9e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_9e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_9E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_10e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow1_10e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_10E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob1x10<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_10e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_10e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_10E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_11e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x11<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach11<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow1_11e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach11<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_11E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob1x11<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_11e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_11e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_11E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;

    internal ProjectionBringFlow1_12e(World world, Query query, ProjectionPredicate<T0>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob1x12<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly ProjectionForEach12<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow1_12e(World world, Query query, ProjectionPredicate<T0>? predicate, ProjectionForEach12<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor1_12E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob1x12<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow1_12e(
        World world,
        Query query,
        ProjectionPredicate<T0>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow1_12e<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor1_12E<T0, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
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

    public ProjectionBringFlow2<T0, T1, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow2<T0, T1, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_2e<T0, T1, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow2_2e<T0, T1, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor2<T0, T1>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1>
    {
        ProjectionExecutor2<T0, T1>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2<T0, T1, TEvent0>
where TEvent0 : struct
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

    public ProjectionPostFlow2<T0, T1, TEvent0> ForEach(ProjectionForEach<T0, T1, TEvent0> forEach)
    {
        return new ProjectionPostFlow2<T0, T1, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2<T0, T1, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x1<T0, T1, TEvent0>
    {
        return new ProjectionJobPostFlow2<T0, T1, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2<T0, T1, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach<T0, T1, TEvent0> _forEach;

    internal ProjectionPostFlow2(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach<T0, T1, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2<T0, T1, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2<T0, T1>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2<T0, T1, TEvent0, TJob>
    where TJob : struct, IProjectionJob2x1<T0, T1, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2<T0, T1, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2<T0, T1>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_2e<T0, T1, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_2e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_2e<T0, T1, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow2_2e<T0, T1, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_2e<T0, T1, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x2<T0, T1, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow2_2e<T0, T1, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_2e<T0, T1, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach2<T0, T1, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow2_2e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach2<T0, T1, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_2e<T0, T1, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_2E<T0, T1, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_2e<T0, T1, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob2x2<T0, T1, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_2e<T0, T1, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_2E<T0, T1, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_3e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x3<T0, T1, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach3<T0, T1, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow2_3e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach3<T0, T1, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_3E<T0, T1, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob2x3<T0, T1, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_3e<T0, T1, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_3E<T0, T1, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_4e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow2_4e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_4E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob2x4<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_4e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_4E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_5e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow2_5e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_5E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob2x5<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_5e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_5E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_6e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow2_6e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_6E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob2x6<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_6e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_6E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_7e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow2_7e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_7E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob2x7<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_7e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_7E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_8e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow2_8e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_8E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob2x8<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_8e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_8E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_9e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow2_9e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_9E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob2x9<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_9e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_9E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_10e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow2_10e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_10E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob2x10<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_10e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_10E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_11e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x11<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach11<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow2_11e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach11<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_11E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob2x11<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_11e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_11E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2_12e(World world, Query query, ProjectionPredicate<T0, T1>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob2x12<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly ProjectionForEach12<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow2_12e(World world, Query query, ProjectionPredicate<T0, T1>? predicate, ProjectionForEach12<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor2_12E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob2x12<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow2_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow2_12e<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor2_12E<T0, T1, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
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

    public ProjectionBringFlow3<T0, T1, T2, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow3<T0, T1, T2, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_2e<T0, T1, T2, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow3_2e<T0, T1, T2, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor3<T0, T1, T2>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2>
    {
        ProjectionExecutor3<T0, T1, T2>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3<T0, T1, T2, TEvent0>
where TEvent0 : struct
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

    public ProjectionPostFlow3<T0, T1, T2, TEvent0> ForEach(ProjectionForEach<T0, T1, T2, TEvent0> forEach)
    {
        return new ProjectionPostFlow3<T0, T1, T2, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3<T0, T1, T2, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x1<T0, T1, T2, TEvent0>
    {
        return new ProjectionJobPostFlow3<T0, T1, T2, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3<T0, T1, T2, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, TEvent0> _forEach;

    internal ProjectionPostFlow3(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach<T0, T1, T2, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3<T0, T1, T2, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3<T0, T1, T2>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3<T0, T1, T2, TEvent0, TJob>
    where TJob : struct, IProjectionJob3x1<T0, T1, T2, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3<T0, T1, T2, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3<T0, T1, T2>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_2e<T0, T1, T2, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_2e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_2e<T0, T1, T2, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, T2, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow3_2e<T0, T1, T2, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_2e<T0, T1, T2, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x2<T0, T1, T2, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow3_2e<T0, T1, T2, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_2e<T0, T1, T2, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach2<T0, T1, T2, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow3_2e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach2<T0, T1, T2, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_2e<T0, T1, T2, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_2E<T0, T1, T2, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_2e<T0, T1, T2, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob3x2<T0, T1, T2, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_2e<T0, T1, T2, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_2E<T0, T1, T2, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_3e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, T2, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x3<T0, T1, T2, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach3<T0, T1, T2, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow3_3e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach3<T0, T1, T2, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_3E<T0, T1, T2, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob3x3<T0, T1, T2, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_3e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_3E<T0, T1, T2, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_4e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow3_4e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_4E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob3x4<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_4e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_4E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_5e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow3_5e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_5E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob3x5<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_5e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_5E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_6e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow3_6e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_6E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob3x6<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_6e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_6E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_7e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow3_7e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_7E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob3x7<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_7e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_7E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_8e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow3_8e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_8E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob3x8<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_8e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_8E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_9e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow3_9e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_9E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob3x9<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_9e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_9E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_10e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow3_10e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_10E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob3x10<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_10e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_10E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_11e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x11<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach11<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow3_11e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach11<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_11E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob3x11<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_11e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_11E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;

    internal ProjectionBringFlow3_12e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob3x12<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly ProjectionForEach12<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow3_12e(World world, Query query, ProjectionPredicate<T0, T1, T2>? predicate, ProjectionForEach12<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor3_12E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob3x12<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow3_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow3_12e<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor3_12E<T0, T1, T2, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
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

    public ProjectionBringFlow4<T0, T1, T2, T3, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow4<T0, T1, T2, T3, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor4<T0, T1, T2, T3>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3>
    {
        ProjectionExecutor4<T0, T1, T2, T3>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4<T0, T1, T2, T3, TEvent0>
where TEvent0 : struct
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

    public ProjectionPostFlow4<T0, T1, T2, T3, TEvent0> ForEach(ProjectionForEach<T0, T1, T2, T3, TEvent0> forEach)
    {
        return new ProjectionPostFlow4<T0, T1, T2, T3, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4<T0, T1, T2, T3, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x1<T0, T1, T2, T3, TEvent0>
    {
        return new ProjectionJobPostFlow4<T0, T1, T2, T3, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4<T0, T1, T2, T3, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, TEvent0> _forEach;

    internal ProjectionPostFlow4(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach<T0, T1, T2, T3, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4<T0, T1, T2, T3, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4<T0, T1, T2, T3>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4<T0, T1, T2, T3, TEvent0, TJob>
    where TJob : struct, IProjectionJob4x1<T0, T1, T2, T3, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4<T0, T1, T2, T3, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4<T0, T1, T2, T3>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, T2, T3, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x2<T0, T1, T2, T3, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach2<T0, T1, T2, T3, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow4_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach2<T0, T1, T2, T3, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_2E<T0, T1, T2, T3, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob4x2<T0, T1, T2, T3, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_2e<T0, T1, T2, T3, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_2E<T0, T1, T2, T3, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow4_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_3E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob4x3<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_3e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_3E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow4_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_4E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob4x4<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_4e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_4E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow4_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_5E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob4x5<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_5e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_5E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow4_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_6E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob4x6<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_6e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_6E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow4_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_7E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob4x7<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_7e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_7E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow4_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_8E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob4x8<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_8e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_8E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow4_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_9E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob4x9<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_9e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_9E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow4_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_10E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob4x10<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_10e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_10E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x11<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach11<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow4_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach11<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_11E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob4x11<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_11e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_11E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;

    internal ProjectionBringFlow4_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob4x12<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly ProjectionForEach12<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow4_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3>? predicate, ProjectionForEach12<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor4_12E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob4x12<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow4_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow4_12e<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor4_12E<T0, T1, T2, T3, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
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

    public ProjectionBringFlow5<T0, T1, T2, T3, T4, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow5<T0, T1, T2, T3, T4, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor5<T0, T1, T2, T3, T4>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4>
    {
        ProjectionExecutor5<T0, T1, T2, T3, T4>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5<T0, T1, T2, T3, T4, TEvent0>
where TEvent0 : struct
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

    public ProjectionPostFlow5<T0, T1, T2, T3, T4, TEvent0> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, TEvent0> forEach)
    {
        return new ProjectionPostFlow5<T0, T1, T2, T3, T4, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5<T0, T1, T2, T3, T4, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x1<T0, T1, T2, T3, T4, TEvent0>
    {
        return new ProjectionJobPostFlow5<T0, T1, T2, T3, T4, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5<T0, T1, T2, T3, T4, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, TEvent0> _forEach;

    internal ProjectionPostFlow5(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5<T0, T1, T2, T3, T4, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5<T0, T1, T2, T3, T4>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5<T0, T1, T2, T3, T4, TEvent0, TJob>
    where TJob : struct, IProjectionJob5x1<T0, T1, T2, T3, T4, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5<T0, T1, T2, T3, T4, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5<T0, T1, T2, T3, T4>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, T2, T3, T4, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x2<T0, T1, T2, T3, T4, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach2<T0, T1, T2, T3, T4, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow5_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_2E<T0, T1, T2, T3, T4, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob5x2<T0, T1, T2, T3, T4, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_2e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_2E<T0, T1, T2, T3, T4, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow5_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_3E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob5x3<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_3e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_3E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow5_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_4E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob5x4<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_4e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_4E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow5_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_5E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob5x5<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_5e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_5E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow5_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_6E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob5x6<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_6e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_6E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow5_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_7E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob5x7<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_7e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_7E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow5_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_8E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob5x8<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_8e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_8E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow5_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_9E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob5x9<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_9e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_9E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow5_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_10E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob5x10<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_10e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_10E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x11<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach11<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow5_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach11<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_11E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob5x11<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_11e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_11E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;

    internal ProjectionBringFlow5_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob5x12<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly ProjectionForEach12<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow5_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4>? predicate, ProjectionForEach12<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor5_12E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob5x12<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow5_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow5_12e<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor5_12E<T0, T1, T2, T3, T4, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
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

    public ProjectionBringFlow6<T0, T1, T2, T3, T4, T5, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow6<T0, T1, T2, T3, T4, T5, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5>
    {
        ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6<T0, T1, T2, T3, T4, T5, TEvent0>
where TEvent0 : struct
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

    public ProjectionPostFlow6<T0, T1, T2, T3, T4, T5, TEvent0> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, T5, TEvent0> forEach)
    {
        return new ProjectionPostFlow6<T0, T1, T2, T3, T4, T5, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6<T0, T1, T2, T3, T4, T5, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x1<T0, T1, T2, T3, T4, T5, TEvent0>
    {
        return new ProjectionJobPostFlow6<T0, T1, T2, T3, T4, T5, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6<T0, T1, T2, T3, T4, T5, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, T5, TEvent0> _forEach;

    internal ProjectionPostFlow6(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, T5, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6<T0, T1, T2, T3, T4, T5, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6<T0, T1, T2, T3, T4, T5, TEvent0, TJob>
    where TJob : struct, IProjectionJob6x1<T0, T1, T2, T3, T4, T5, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6<T0, T1, T2, T3, T4, T5, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow6_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_2E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob6x2<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_2e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_2E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow6_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_3E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob6x3<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_3e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_3E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow6_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_4E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob6x4<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_4e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_4E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow6_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_5E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob6x5<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_5e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_5E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow6_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_6E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob6x6<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_6e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_6E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow6_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_7E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob6x7<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_7e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_7E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow6_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_8E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob6x8<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_8e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_8E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow6_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_9E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob6x9<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_9e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_9E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow6_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_10E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob6x10<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_10e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_10E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x11<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach11<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow6_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach11<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_11E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob6x11<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_11e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_11E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;

    internal ProjectionBringFlow6_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob6x12<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly ProjectionForEach12<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow6_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate, ProjectionForEach12<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor6_12E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob6x12<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow6_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow6_12e<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor6_12E<T0, T1, T2, T3, T4, T5, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
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

    public ProjectionBringFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6>
    {
        ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0>
where TEvent0 : struct
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

    public ProjectionPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, TEvent0> forEach)
    {
        return new ProjectionPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x1<T0, T1, T2, T3, T4, T5, T6, TEvent0>
    {
        return new ProjectionJobPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, TEvent0> _forEach;

    internal ProjectionPostFlow7(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TJob>
    where TJob : struct, IProjectionJob7x1<T0, T1, T2, T3, T4, T5, T6, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow7_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_2E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob7x2<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_2e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_2E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow7_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_3E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob7x3<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_3e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_3E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow7_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_4E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob7x4<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_4e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_4E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow7_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_5E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob7x5<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_5e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_5E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow7_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_6E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob7x6<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_6e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_6E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow7_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_7E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob7x7<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_7e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_7E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow7_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_8E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob7x8<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_8e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_8E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow7_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_9E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob7x9<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_9e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_9E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow7_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_10E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob7x10<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_10e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_10E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x11<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow7_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_11E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob7x11<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_11e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_11E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;

    internal ProjectionBringFlow7_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob7x12<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow7_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate, ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor7_12E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob7x12<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow7_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow7_12e<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor7_12E<T0, T1, T2, T3, T4, T5, T6, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
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

    public ProjectionBringFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7>
    {
        ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>
where TEvent0 : struct
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

    public ProjectionPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0> forEach)
    {
        return new ProjectionPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x1<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>
    {
        return new ProjectionJobPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0> _forEach;

    internal ProjectionPostFlow8(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TJob>
    where TJob : struct, IProjectionJob8x1<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow8_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_2E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob8x2<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_2e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_2E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow8_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_3E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob8x3<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_3e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_3E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow8_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_4E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob8x4<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_4e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_4E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow8_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_5E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob8x5<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_5e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_5E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow8_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_6E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob8x6<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_6e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_6E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow8_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_7E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob8x7<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_7e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_7E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow8_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_8E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob8x8<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_8e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_8E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow8_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_9E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob8x9<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_9e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_9E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow8_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_10E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob8x10<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_10e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_10E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x11<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow8_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_11E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob8x11<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_11e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_11E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;

    internal ProjectionBringFlow8_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob8x12<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow8_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate, ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor8_12E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob8x12<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow8_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow8_12e<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor8_12E<T0, T1, T2, T3, T4, T5, T6, T7, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionQueryFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionQueryFlow9(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8> Where(ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8> predicate)
    {
        return new ProjectionQueryFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(_world, _query, predicate);
    }

    public ProjectionBringFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor9<T0, T1, T2, T3, T4, T5, T6, T7, T8>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8>
    {
        ProjectionExecutor9<T0, T1, T2, T3, T4, T5, T6, T7, T8>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0> forEach)
    {
        return new ProjectionPostFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0>
    {
        return new ProjectionJobPostFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0> _forEach;

    internal ProjectionPostFlow9(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9<T0, T1, T2, T3, T4, T5, T6, T7, T8>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TJob>
    where TJob : struct, IProjectionJob9x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9<T0, T1, T2, T3, T4, T5, T6, T7, T8>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow9_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_2E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob9x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_2E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow9_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_3E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob9x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_3E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow9_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_4E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob9x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_4E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow9_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_5E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob9x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_5E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow9_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_6E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob9x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_6E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow9_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_7E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob9x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_7E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow9_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_8E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob9x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_8E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow9_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_9E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob9x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_9E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow9_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_10E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob9x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_10E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow9_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_11E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob9x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_11E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;

    internal ProjectionBringFlow9_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob9x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow9_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate, ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor9_12E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob9x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow9_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow9_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor9_12E<T0, T1, T2, T3, T4, T5, T6, T7, T8, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionQueryFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionQueryFlow10(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> Where(ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> predicate)
    {
        return new ProjectionQueryFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(_world, _query, predicate);
    }

    public ProjectionBringFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        ProjectionExecutor10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0> forEach)
    {
        return new ProjectionPostFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0>
    {
        return new ProjectionJobPostFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0> _forEach;

    internal ProjectionPostFlow10(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TJob>
    where TJob : struct, IProjectionJob10x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow10_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_2E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob10x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_2E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow10_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_3E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob10x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_3E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow10_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_4E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob10x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_4E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow10_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_5E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob10x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_5E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow10_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_6E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob10x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_6E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow10_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_7E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob10x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_7E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow10_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_8E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob10x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_8E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow10_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_9E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob10x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_9E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow10_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_10E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob10x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_10E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow10_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_11E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob10x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_11E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;

    internal ProjectionBringFlow10_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob10x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow10_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate, ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor10_12E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob10x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow10_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow10_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor10_12E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionQueryFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionQueryFlow11(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Where(ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> predicate)
    {
        return new ProjectionQueryFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(_world, _query, predicate);
    }

    public ProjectionBringFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        ProjectionExecutor11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0> forEach)
    {
        return new ProjectionPostFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0>
    {
        return new ProjectionJobPostFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0> _forEach;

    internal ProjectionPostFlow11(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TJob>
    where TJob : struct, IProjectionJob11x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow11_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_2E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob11x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_2E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow11_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_3E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob11x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_3E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow11_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_4E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob11x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_4E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow11_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_5E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob11x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_5E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow11_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_6E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob11x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_6E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow11_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_7E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob11x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_7E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow11_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_8E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob11x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_8E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow11_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_9E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob11x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_9E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow11_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_10E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob11x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_10E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow11_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_11E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob11x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_11E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;

    internal ProjectionBringFlow11_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob11x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow11_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate, ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor11_12E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob11x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow11_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow11_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor11_12E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionQueryFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionQueryFlow12(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate = null)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Where(ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> predicate)
    {
        return new ProjectionQueryFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(_world, _query, predicate);
    }

    public ProjectionBringFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0> Bring<TEvent0>()
where TEvent0 : struct
    {
        return new ProjectionBringFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
where TEvent0 : struct
    where TEvent1 : struct
    {
        return new ProjectionBringFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2> Bring<TEvent0, TEvent1, TEvent2>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    {
        return new ProjectionBringFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3> Bring<TEvent0, TEvent1, TEvent2, TEvent3>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    {
        return new ProjectionBringFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    {
        return new ProjectionBringFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    {
        return new ProjectionBringFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    {
        return new ProjectionBringFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    {
        return new ProjectionBringFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    {
        return new ProjectionBringFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    {
        return new ProjectionBringFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    {
        return new ProjectionBringFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate);
    }

    public ProjectionBringFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Bring<TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
    {
        return new ProjectionBringFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate);
    }

    public void TouchProjectedActor()
    {
        ProjectionExecutor12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>.Touch(_world, _query, _predicate);
    }

    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
    {
        ProjectionExecutor12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0> ForEach(ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0> forEach)
    {
        return new ProjectionPostFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0>
    {
        return new ProjectionJobPostFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0> _forEach;

    internal ProjectionPostFlow12(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>.Post<TEvent0>(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TJob>
    where TJob : struct, IProjectionJob12x1<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0>
where TEvent0 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>.Post<TEvent0, TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1> ForEach(ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1> forEach)
    {
        return new ProjectionPostFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1>
    {
        return new ProjectionJobPostFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow12_2e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_2E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TJob>
    where TJob : struct, IProjectionJob12x2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1>
where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_2e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_2e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_2E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2> ForEach(ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2> forEach)
    {
        return new ProjectionPostFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2>
    {
        return new ProjectionJobPostFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2> _forEach;

    internal ProjectionPostFlow12_3e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_3E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TJob>
    where TJob : struct, IProjectionJob12x3<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_3e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_3e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_3E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3> ForEach(ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        return new ProjectionPostFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3>
    {
        return new ProjectionJobPostFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3> _forEach;

    internal ProjectionPostFlow12_4e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_4E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TJob>
    where TJob : struct, IProjectionJob12x4<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_4e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_4e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_4E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> ForEach(ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        return new ProjectionPostFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
    {
        return new ProjectionJobPostFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> _forEach;

    internal ProjectionPostFlow12_5e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_5E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob>
    where TJob : struct, IProjectionJob12x5<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_5e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_5e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_5E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> ForEach(ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        return new ProjectionPostFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
    {
        return new ProjectionJobPostFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> _forEach;

    internal ProjectionPostFlow12_6e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_6E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob>
    where TJob : struct, IProjectionJob12x6<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_6e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_6e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_6E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> ForEach(ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        return new ProjectionPostFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
    {
        return new ProjectionJobPostFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> _forEach;

    internal ProjectionPostFlow12_7e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_7E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob>
    where TJob : struct, IProjectionJob12x7<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_7e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_7e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_7E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> ForEach(ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        return new ProjectionPostFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
    {
        return new ProjectionJobPostFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> _forEach;

    internal ProjectionPostFlow12_8e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_8E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob>
    where TJob : struct, IProjectionJob12x8<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_8e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_8e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_8E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> ForEach(ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        return new ProjectionPostFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
    {
        return new ProjectionJobPostFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> _forEach;

    internal ProjectionPostFlow12_9e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_9E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob>
    where TJob : struct, IProjectionJob12x9<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_9e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_9e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_9E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> ForEach(ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        return new ProjectionPostFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
    {
        return new ProjectionJobPostFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> _forEach;

    internal ProjectionPostFlow12_10e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_10E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob>
    where TJob : struct, IProjectionJob12x10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_10e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_10e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_10E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> ForEach(ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        return new ProjectionPostFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
    {
        return new ProjectionJobPostFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> _forEach;

    internal ProjectionPostFlow12_11e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_11E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob>
    where TJob : struct, IProjectionJob12x11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_11e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_11e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_11E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

public readonly struct ProjectionBringFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;

    internal ProjectionBringFlow12_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> ForEach(ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        return new ProjectionPostFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(_world, _query, _predicate, forEach);
    }

    public ProjectionJobPostFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob12x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
    {
        return new ProjectionJobPostFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
}

public readonly struct ProjectionPostFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> _forEach;

    internal ProjectionPostFlow12_12e(World world, Query query, ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate, ProjectionForEach12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> forEach)
    {
        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11> Batch() => this;

    public void Post()
    {
        ProjectionExecutor12_12E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post(_world, _query, _predicate, _forEach);
    }
}

public readonly struct ProjectionJobPostFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob>
    where TJob : struct, IProjectionJob12x12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>
where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct
    where TEvent3 : struct
    where TEvent4 : struct
    where TEvent5 : struct
    where TEvent6 : struct
    where TEvent7 : struct
    where TEvent8 : struct
    where TEvent9 : struct
    where TEvent10 : struct
    where TEvent11 : struct
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow12_12e(
        World world,
        Query query,
        ProjectionPredicate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? predicate,
        TJob job)
    {
        // world �������ã�
        // ��ǰ ECS World��

        // query �������ã�
        // ��ǰ Projection Query��

        // predicate �������ã�
        // ��ѡ����������

        // job �������ã�
        // Դ���������ɵ� Projection Job��
        // ��������û�д�� [Query] + [Bring] ������

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow12_12e<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

        ProjectionExecutor12_12E<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TEvent0, TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>.Post<TJob>(
            _world,
            _query,
            _predicate,
            ref job);
    }
}

