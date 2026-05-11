#nullable enable
using Arch.Core;

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

    public void TouchProjectedActor()
    {
        ProjectionExecutor1<T0>.Touch(_world, _query, _predicate);
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
        ProjectionExecutor1<T0>.Post(_world, _query, _predicate, _forEach);
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

    public void TouchProjectedActor()
    {
        ProjectionExecutor2<T0, T1>.Touch(_world, _query, _predicate);
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
        ProjectionExecutor2<T0, T1>.Post(_world, _query, _predicate, _forEach);
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

    public void TouchProjectedActor()
    {
        ProjectionExecutor3<T0, T1, T2>.Touch(_world, _query, _predicate);
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
        ProjectionExecutor3<T0, T1, T2>.Post(_world, _query, _predicate, _forEach);
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

    public void TouchProjectedActor()
    {
        ProjectionExecutor4<T0, T1, T2, T3>.Touch(_world, _query, _predicate);
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
        ProjectionExecutor4<T0, T1, T2, T3>.Post(_world, _query, _predicate, _forEach);
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

    public void TouchProjectedActor()
    {
        ProjectionExecutor5<T0, T1, T2, T3, T4>.Touch(_world, _query, _predicate);
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
        ProjectionExecutor5<T0, T1, T2, T3, T4>.Post(_world, _query, _predicate, _forEach);
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

    public void TouchProjectedActor()
    {
        ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.Touch(_world, _query, _predicate);
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
        ProjectionExecutor6<T0, T1, T2, T3, T4, T5>.Post(_world, _query, _predicate, _forEach);
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

    public void TouchProjectedActor()
    {
        ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.Touch(_world, _query, _predicate);
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
        ProjectionExecutor7<T0, T1, T2, T3, T4, T5, T6>.Post(_world, _query, _predicate, _forEach);
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

    public void TouchProjectedActor()
    {
        ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.Touch(_world, _query, _predicate);
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
        ProjectionExecutor8<T0, T1, T2, T3, T4, T5, T6, T7>.Post(_world, _query, _predicate, _forEach);
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

