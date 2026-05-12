namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public bool DestroyActor(ActorId actorId)
    {
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        bool marked = _archetypes[actorId.ArchetypeId].MarkPendingDestroy(actorId);
        if (marked)
        {
            _pendingDestroyCount++;
        }

        return marked;
    }

    public bool IsAlive(ActorId actorId)
    {
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId].IsAlive(actorId);
    }

    private void SweepPendingDestroy()
    {
        if (_pendingDestroyCount <= 0)
        {
            return;
        }

        foreach (BehaviourArchetype archetype in _archetypes)
        {
            archetype.SweepPendingDestroy(this);
        }

        _pendingDestroyCount = 0;
    }
}