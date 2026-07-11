using System;
using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal delegate ProjectedActorHandle ProjectedActorFactory(
    ActorWorld actorWorld);

internal static class ProjectedActorTypeRegistry
{
    private static Type?[] _typesById = new Type?[64];
    private static ProjectedActorFactory?[] _factoriesById = new ProjectedActorFactory?[64];
    private static ProjectedActorOptions[] _optionsById = new ProjectedActorOptions[64];
    private static bool[] _optionsInitializedById = new bool[64];

    static ProjectedActorTypeRegistry()
    {
        LayerHub.RegisterCacheResetter(Reset);
    }

    public static void RegisterGenerated(
        int                   actorTypeId,
        Type                  actorType,
        ProjectedActorFactory factory)
    {
        EnsureCapacity(actorTypeId);

        _typesById[actorTypeId] = actorType;
        _factoriesById[actorTypeId] = factory;

        if (!_optionsInitializedById[actorTypeId])
        {
            _optionsById[actorTypeId] = CreateOptionsFromAttribute(actorType);
            _optionsInitializedById[actorTypeId] = true;
        }
    }

    public static void RegisterGenerated(
        int                      actorTypeId,
        Type                     actorType,
        ProjectedActorFactory    factory,
        in ProjectedActorOptions options)
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

    private static ProjectedActorOptions CreateOptionsFromAttribute(Type actorType)
    {
        object[] attrs = actorType.GetCustomAttributes(inherit: false);
        foreach (object attr in attrs)
        {
            if (attr is ProjectedActorOptionsAttribute projectedAttr)
            {
                return ProjectedActorOptions.FromAttribute(
                    projectedAttr.RetirePolicy,
                    projectedAttr.CreatePolicy,
                    projectedAttr.KeepAliveSeconds,
                    projectedAttr.TouchIntervalSeconds);
            }

            if (attr is ActorOptionsAttribute compatibilityAttr)
            {
                return ProjectedActorOptions.FromAttribute(
                    compatibilityAttr.RetirePolicy,
                    compatibilityAttr.CreatePolicy,
                    compatibilityAttr.KeepAliveSeconds,
                    compatibilityAttr.TouchIntervalSeconds);
            }
        }

        return ProjectedActorOptions.Default;
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
