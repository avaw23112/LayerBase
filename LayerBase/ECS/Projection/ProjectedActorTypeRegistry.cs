using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal delegate ProjectedActorHandle ProjectedActorFactory(
    ActorWorld actorWorld);

internal static class ProjectedActorTypeRegistry
{
    private static Type?[] _typesById = new Type?[64];
    private static ProjectedActorFactory?[] _factoriesById = new ProjectedActorFactory?[64];

    /// <summary>
    /// _optionsById 字段作用：
    /// ActorTypeId -> ProjectedActorOptions。
    ///
    /// 旧 RegisterGenerated 会冷路径反射读取 ActorOptionsAttribute。
    /// 新 RegisterGenerated overload 由源生成器直接传入 options，不反射。
    /// </summary>
    private static ProjectedActorOptions[] _optionsById = new ProjectedActorOptions[64];

    /// <summary>
    /// _optionsInitializedById 字段作用：
    /// 标记某个 ActorTypeId 是否已经初始化 options。
    ///
    /// 作用：
    /// 1. 避免旧 RegisterGenerated 被重复调用时重复反射。
    /// 2. 保证同一个 actorTypeId 只解析一次 ActorOptions。
    /// </summary>
    private static bool[] _optionsInitializedById = new bool[64];

    static ProjectedActorTypeRegistry()
    {
        LayerHub.RegisterCacheResetter(Reset);
    }

    /// <summary>
    /// 旧 RegisterGenerated - 兼容冷路径，首次调用时反射读取 ActorOptionsAttribute 并缓存。
    /// </summary>
    public static void RegisterGenerated(
        int                   actorTypeId,
        Type                  actorType,
        ProjectedActorFactory factory)
    {
        EnsureCapacity(actorTypeId);
        if (!_optionsInitializedById[actorTypeId])
        {
            _typesById[actorTypeId] = actorType;
            _factoriesById[actorTypeId] = factory;
            _optionsById[actorTypeId] = CreateOptionsFromAttribute(actorType);
            _optionsInitializedById[actorTypeId] = true;
        }
    }

    /// <summary>
    /// 新 RegisterGenerated overload - 由源生成器直接传入 options，完全无反射。
    /// </summary>
    public static void RegisterGenerated(
        int                          actorTypeId,
        Type                         actorType,
        ProjectedActorFactory        factory,
        in ProjectedActorOptions     options)
    {
        EnsureCapacity(actorTypeId);
        _typesById[actorTypeId] = actorType;
        _factoriesById[actorTypeId] = factory;
        _optionsById[actorTypeId] = options;
        _optionsInitializedById[actorTypeId] = true;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ProjectedActorHandle CreateActorByTypeId(
        ActorWorld actorWorld,
        int        actorTypeId)
    {
        if ((uint)actorTypeId >= (uint)_factoriesById.Length)
        {
            return default;
        }

        ProjectedActorFactory? factory = _factoriesById[actorTypeId];
        if (factory == null)
        {
            return default;
        }

        return factory(actorWorld);
    }

    public static Type? GetActorType(
        int actorTypeId)
    {
        if ((uint)actorTypeId >= (uint)_typesById.Length)
        {
            return null;
        }

        return _typesById[actorTypeId];
    }

    /// <summary>
    /// GetOptions 作用：
    /// 获取指定 ActorTypeId 的 ProjectedActorOptions。
    ///
    /// 约束：
    /// 只做数组读取，不做反射。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ProjectedActorOptions GetOptions(int actorTypeId)
    {
        if ((uint)actorTypeId >= (uint)_optionsById.Length)
        {
            return ProjectedActorOptions.Default;
        }

        if (!_optionsInitializedById[actorTypeId])
        {
            return ProjectedActorOptions.Default;
        }

        return _optionsById[actorTypeId];
    }

    /// <summary>
    /// 冷路径反射方法 - 只允许被旧 RegisterGenerated 调用。
    /// Touch / Post / Sweep / Ensure 绝不能调用该方法。
    /// </summary>
    private static ProjectedActorOptions CreateOptionsFromAttribute(Type actorType)
    {
        object[] attrs = actorType.GetCustomAttributes(
            typeof(ActorOptionsAttribute),
            inherit: false);

        if (attrs.Length == 0 ||
            attrs[0] is not ActorOptionsAttribute attr)
        {
            return ProjectedActorOptions.Default;
        }

        return ProjectedActorOptions.FromAttribute(
            attr.RetirePolicy,
            attr.CreatePolicy,
            attr.KeepAliveSeconds,
            attr.TouchIntervalSeconds);
    }

    private static void EnsureCapacity(
        int actorTypeId)
    {
        if ((uint)actorTypeId < (uint)_factoriesById.Length)
        {
            return;
        }

        int newLength = _factoriesById.Length;
        while ((uint)actorTypeId >= (uint)newLength)
        {
            newLength <<= 1;
        }

        Array.Resize(ref _typesById, newLength);
        Array.Resize(ref _factoriesById, newLength);
        Array.Resize(ref _optionsById, newLength);
        Array.Resize(ref _optionsInitializedById, newLength);
    }

    private static void Reset()
    {
        _typesById = new Type?[64];
        _factoriesById = new ProjectedActorFactory?[64];
        _optionsById = new ProjectedActorOptions[64];
        _optionsInitializedById = new bool[64];
    }
}
