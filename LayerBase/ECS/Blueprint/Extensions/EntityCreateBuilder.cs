using System.Buffers;
using System.Runtime.CompilerServices;
using Arch.Core;
using Collections.Pooled;
using LayerBase.Core;

namespace LayerBase.ECS;

/// <summary>
/// EntityCreateBuilder，支持 WithBlueprint&lt;TBlueprint&gt;()。
/// </summary>
/// 
public readonly ref struct EntityCreateBuilder 
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly PooledList<ComponentType>  _componentTypes;

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
        where TComponent : class, IComponent, new()
    {
        _componentTypes.Add(typeof(TComponent));
        return this;
    }

    public Entity Build()
    {
        ComponentType[] componentTypes = ArrayPool<ComponentType>.Shared.Rent(_componentTypes.Count);
        _componentTypes.CopyTo(componentTypes);
        Entity entity = _world.Create(componentTypes);
        ArrayPool<ComponentType>.Shared.Return(componentTypes);
        _componentTypes.Dispose();
        return entity;
    }
}
