namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public bool IsEnable(ActorId actorId)
    {
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId].IsEnable(actorId);
    }

    public bool SetEnable(ActorId actorId, bool enable)
    {
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId].SetEnable(actorId, enable);
    }
}
