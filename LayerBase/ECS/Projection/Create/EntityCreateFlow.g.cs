#nullable enable
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.ECS.Projection;

namespace LayerBase.ECS.Projection.Create;

public readonly struct EntityCreateFlow0
{
    private readonly World _world;
    private readonly Entity _entity;

    internal EntityCreateFlow0(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow0 WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        _world.WithProjectedActor<TActor>(_entity, keepAliveSeconds, releasePolicy);
        return this;
    }
}

public readonly struct EntityCreateFlow1<T0>
{
    private readonly World _world;
    private readonly Entity _entity;

    internal EntityCreateFlow1(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow1<T0> WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        _world.WithProjectedActor<TActor>(_entity, keepAliveSeconds, releasePolicy);
        return this;
    }
}

public readonly struct EntityCreateFlow2<T0, T1>
{
    private readonly World _world;
    private readonly Entity _entity;

    internal EntityCreateFlow2(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow2<T0, T1> WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        _world.WithProjectedActor<TActor>(_entity, keepAliveSeconds, releasePolicy);
        return this;
    }
}

public readonly struct EntityCreateFlow3<T0, T1, T2>
{
    private readonly World _world;
    private readonly Entity _entity;

    internal EntityCreateFlow3(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow3<T0, T1, T2> WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        _world.WithProjectedActor<TActor>(_entity, keepAliveSeconds, releasePolicy);
        return this;
    }
}

public readonly struct EntityCreateFlow4<T0, T1, T2, T3>
{
    private readonly World _world;
    private readonly Entity _entity;

    internal EntityCreateFlow4(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow4<T0, T1, T2, T3> WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        _world.WithProjectedActor<TActor>(_entity, keepAliveSeconds, releasePolicy);
        return this;
    }
}

public readonly struct EntityCreateFlow5<T0, T1, T2, T3, T4>
{
    private readonly World _world;
    private readonly Entity _entity;

    internal EntityCreateFlow5(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow5<T0, T1, T2, T3, T4> WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        _world.WithProjectedActor<TActor>(_entity, keepAliveSeconds, releasePolicy);
        return this;
    }
}

public readonly struct EntityCreateFlow6<T0, T1, T2, T3, T4, T5>
{
    private readonly World _world;
    private readonly Entity _entity;

    internal EntityCreateFlow6(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow6<T0, T1, T2, T3, T4, T5> WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        _world.WithProjectedActor<TActor>(_entity, keepAliveSeconds, releasePolicy);
        return this;
    }
}

public readonly struct EntityCreateFlow7<T0, T1, T2, T3, T4, T5, T6>
{
    private readonly World _world;
    private readonly Entity _entity;

    internal EntityCreateFlow7(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow7<T0, T1, T2, T3, T4, T5, T6> WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        _world.WithProjectedActor<TActor>(_entity, keepAliveSeconds, releasePolicy);
        return this;
    }
}

public readonly struct EntityCreateFlow8<T0, T1, T2, T3, T4, T5, T6, T7>
{
    private readonly World _world;
    private readonly Entity _entity;

    internal EntityCreateFlow8(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow8<T0, T1, T2, T3, T4, T5, T6, T7> WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        _world.WithProjectedActor<TActor>(_entity, keepAliveSeconds, releasePolicy);
        return this;
    }
}

