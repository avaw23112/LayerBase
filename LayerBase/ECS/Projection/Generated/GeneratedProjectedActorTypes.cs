using LayerBase.Actor;

namespace LayerBase.ECS.Projection.Generated;

internal static partial class GeneratedProjectedActorTypes
{
    public static void RegisterTo(
        ProjectedActorTypeRegistry registry)
    {
        RegisterGeneratedTypes(registry);
    }

    public static int GetId<TActor>()
        where TActor : class, IPooledActor, new()
    {
        int actorTypeId = -1;
        TryWriteGeneratedId<TActor>(ref actorTypeId);
        return actorTypeId;
    }

    private static partial void RegisterGeneratedTypes(
        ProjectedActorTypeRegistry registry);

    private static partial void TryWriteGeneratedId<TActor>(
        ref int actorTypeId)
        where TActor : class, IPooledActor, new();
}
