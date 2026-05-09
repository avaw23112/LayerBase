namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public DispatchResult DispatchNow<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return DispatchResult.Failure(
                DispatchFailureKind.InvalidActorId,
                "Invalid ActorId.ArchetypeId.");
        }

        return _archetypes[actorId.ArchetypeId].DispatchNow(actorId, in value);
    }
}
