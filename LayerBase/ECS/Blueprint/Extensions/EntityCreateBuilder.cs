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
    private readonly PooledList<ComponentType> _componentTypes;
    private int _actorTypeId = -1;

    /// <summary>
    /// _keepAliveOverrideTicks 参数作用：
    /// 保存 WithProjectedActor 显式传入的保活时长。
    /// null 表示不覆盖 ActorOptions.KeepAliveTicks。
    /// </summary>
    private long? _keepAliveOverrideTicks;

    /// <summary>
    /// _releasePolicy 参数作用：
    /// 保留旧释放策略兼容入口。
    /// 新策略优先由 ProjectedActorOptions.RetirePolicy 表达。
    /// </summary>
    private ProjectedActorReleasePolicy _releasePolicy;

    /// <summary>
    /// _isCreatedActor 参数作用：
    /// 防止同一个 EntityCreateBuilder 重复绑定 ProjectedActor。
    /// </summary>
    private bool _isCreatedActor;

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

    /// <summary>
    /// 将当前 Entity 标记为可投影 Actor。
    ///
    /// TActor 类型参数作用：
    /// 指定 Entity 对应的 ProjectedActor 类型。
    ///
    /// 逻辑作用：
    /// 1. 注册 TActor 的 Type / Factory / ActorOptions。
    /// 2. 默认不覆盖 ActorOptions.KeepAliveTicks。
    /// 3. Build 时由 ProjectedActorMarkUtility 读取 ActorOptions。
    /// </summary>
    /// <typeparam name="TActor">ProjectedActor 类型，必须实现 IPooledActor。</typeparam>
    /// <returns>当前 EntityCreateBuilder。</returns>
    public EntityCreateBuilder WithProjectedActor<TActor>()
        where TActor : class, IPooledActor, new()
    {
        if (_isCreatedActor)
        {
            return this;
        }

        _actorTypeId = ActorType<TActor>.Id;

        ProjectedActorTypeRegistry.RegisterGenerated(
            _actorTypeId,
            typeof(TActor),
            static actorWorld => actorWorld.CreateProjectedActor<TActor>());

        _keepAliveOverrideTicks = null;
        _releasePolicy = ProjectedActorReleasePolicy.ReturnToPool;
        _isCreatedActor = true;
        _componentTypes.Add(typeof(ProjectedActorRef));

        return this;
    }

    /// <summary>
    /// 将当前 Entity 标记为可投影 Actor，并显式覆盖 ActorOptions 中的保活时间。
    ///
    /// keepAliveSeconds 参数作用：
    /// 显式指定最后一次 Touch 后 Actor 继续保持 Active 的秒数。
    /// 该值会覆盖 ActorOptions.KeepAliveTicks。
    ///
    /// releasePolicy 参数作用：
    /// 兼容旧释放策略。
    /// 后续建议逐步迁移到 ActorOptions.RetirePolicy。
    /// </summary>
    /// <typeparam name="TActor">ProjectedActor 类型，必须实现 IPooledActor。</typeparam>
    /// <param name="keepAliveSeconds">显式保活秒数。</param>
    /// <param name="releasePolicy">旧释放策略。</param>
    /// <returns>当前 EntityCreateBuilder。</returns>
    public EntityCreateBuilder WithProjectedActor<TActor>(
        float keepAliveSeconds,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        if (_isCreatedActor)
        {
            return this;
        }

        _actorTypeId = ActorType<TActor>.Id;

        ProjectedActorTypeRegistry.RegisterGenerated(
            _actorTypeId,
            typeof(TActor),
            static actorWorld => actorWorld.CreateProjectedActor<TActor>());

        _keepAliveOverrideTicks =
            ProjectedActorTime.SecondsToTicks(keepAliveSeconds);

        _releasePolicy = releasePolicy;
        _isCreatedActor = true;
        _componentTypes.Add(typeof(ProjectedActorRef));

        return this;
    }

    /// <summary>
    /// Build 作用：
    /// 创建 ECS Entity，并在需要时写入 ProjectedActorMeta / ProjectedActorRef。
    /// </summary>
    /// <returns>创建后的 Entity。</returns>
    public Entity Build()
    {
        ComponentType[] componentTypes = ArrayPool<ComponentType>.Shared.Rent(_componentTypes.Count);
        _componentTypes.CopyTo(componentTypes);

        Entity entity = _world.Create(componentTypes);

        if (_actorTypeId >= 0)
        {
            _world.WithProjectedActor(
                entity,
                _actorTypeId,
                _keepAliveOverrideTicks,
                _releasePolicy);
        }

        ArrayPool<ComponentType>.Shared.Return(componentTypes);
        _componentTypes.Dispose();
        return entity;
    }
}
