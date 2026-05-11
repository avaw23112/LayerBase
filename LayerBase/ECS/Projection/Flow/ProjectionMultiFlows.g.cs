#nullable enable
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

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

