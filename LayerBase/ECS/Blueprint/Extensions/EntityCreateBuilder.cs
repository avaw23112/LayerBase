using System.Buffers;
using System.Runtime.CompilerServices;
using Arch.Core;
using Collections.Pooled;
using LayerBase.Actor;
using LayerBase.Core;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Generated;

namespace LayerBase.ECS;

/// <summary>
/// EntityCreateBuilder，支持 WithBlueprint&lt;TBlueprint&gt;()。
/// </summary>
/// 
public ref struct EntityCreateBuilder
{
    private readonly LayerRuntime _runtime;
    private readonly World _world;
    private readonly Entity _entity;
    private readonly PooledList<ComponentType>  _componentTypes;
    private int _actorTypeId = -1;
    private float _keepAliveSeconds;
    private ProjectedActorReleasePolicy _releasePolicy;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityCreateBuilder(World world)
    {
        _componentTypes = new PooledList<ComponentType>();
        _world = world;
    }
    

    /// <summary>
    /// 应用 Blueprint 结构到当前 Entity。
    /// </summary>
    /// <typeparam name="TBlueprint">Blueprint 类型。</typeparam>
    /// <returns>当前构建器。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateBuilder WithBlueprint<TBlueprint>()
        where TBlueprint : class, IEntityBlueprint, new()
    {
        EntityBlueprint blueprint = EntityBlueprintCache<TBlueprint>.GetOrBuild();
        _componentTypes.AddRange(blueprint.ComponentTypes);
        return this;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateBuilder WithComponent<TComponent>()
        where TComponent : IComponent
    {
        _componentTypes.Add(typeof(TComponent));
        return this;
    }

    public EntityCreateBuilder WithProjectedActor<TActor>(  float keepAliveSeconds = 0.2f,
                                                            ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        _actorTypeId = GeneratedProjectedActorTypes.GetId<TActor>();
        this._keepAliveSeconds = keepAliveSeconds;
        this._releasePolicy = releasePolicy;
        return this;
    }   

    public Entity Build()
    {
        ComponentType[] componentTypes = ArrayPool<ComponentType>.Shared.Rent(_componentTypes.Count);
        _componentTypes.CopyTo(componentTypes);
        Entity entity = _world.Create(componentTypes);
        if(_actorTypeId>=0 && _keepAliveSeconds > 0)
        {
            _world.WithProjectedActor(entity, _actorTypeId, _keepAliveSeconds, _releasePolicy);
        }
        ArrayPool<ComponentType>.Shared.Return(componentTypes);
        _componentTypes.Dispose();
        return entity;
    }
}
