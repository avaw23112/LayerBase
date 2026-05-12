namespace LayerBase.Actor;

public static class ActorQueryCountExtensions
{
    public static int CountAlive(this ActorQueryResult query)
    {
        query = query.RefreshIfNeeded();

        int count = 0;
        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            count += archetype.CountAlive();
        }

        return count;
    }

    public static int CountEnabled(this ActorQueryResult query)
    {
        query = query.RefreshIfNeeded();

        int count = 0;
        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            count += archetype.CountEnabled();
        }

        return count;
    }

    public static bool IsEmpty(this ActorQueryResult query)
    {
        query = query.RefreshIfNeeded();

        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            if (archetype.HasAnyAlive())
            {
                return false;
            }
        }

        return true;
    }
}