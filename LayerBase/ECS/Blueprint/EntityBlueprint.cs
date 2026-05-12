using System.Runtime.CompilerServices;
using Arch.Core;

namespace LayerBase.ECS;

/// <summary>
/// Blueprint 构建后的实体结构缓存。
/// </summary>
public sealed class EntityBlueprint
{
    private readonly List<Type> _componentTypes = new();
    private ComponentType[] ctx;
    private Type? _projectedActorType;
    private Type? _actorType;

    internal EntityBlueprint()
    {
    }

    public Type ActorProjection => _projectedActorType;
    public Type Actor => _actorType;

    /// <summary>
    /// 实体包含的组件类型列表。
    /// </summary>
    public ComponentType[] ComponentTypes
    {
        get
        {
            if (ctx == null)
            {
                ctx = new ComponentType[_componentTypes.Count];
                for (int i = 0; i < _componentTypes.Count; i++)
                {
                    ctx[i] = _componentTypes[i];
                }
            }

            return ctx;
        }
    }

    /// <summary>
    /// 延迟投影的 Actor 类型，可能为 null。
    /// </summary>
    public Type? ProjectedActorType => _projectedActorType;

    /// <summary>
    /// 立即绑定的 Actor 类型，可能为 null。
    /// </summary>
    public Type? ActorType => _actorType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddComponent<TComponent>()
    {
        _componentTypes.Add(typeof(TComponent));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetProjectedActor<TActor>()
    {
        _projectedActorType = typeof(TActor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetActor<TActor>()
    {
        _actorType = typeof(TActor);
    }
}