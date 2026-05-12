using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal delegate ProjectedActorHandle ProjectedActorFactory(
    ActorWorld actorWorld);

internal static class ProjectedActorTypeRegistry
{
    private static Type?[] _typesById = new Type?[64];
    private static ProjectedActorFactory?[] _factoriesById = new ProjectedActorFactory?[64];

    public static void RegisterGenerated(
        int                   actorTypeId,
        Type                  actorType,
        ProjectedActorFactory factory)
    {
        EnsureCapacity(actorTypeId);
        _typesById[actorTypeId] = actorType;
        _factoriesById[actorTypeId] = factory;
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
    }
}