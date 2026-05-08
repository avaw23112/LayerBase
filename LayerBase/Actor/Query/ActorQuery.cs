using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public ActorQueryResult QueryActor<TEvent>()
        where TEvent : struct
    {
        return new ActorQueryResult(this, GetOrBuildQueryCache(CreateQuerySignature(EventTypeId<TEvent>.Id)));
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2>()
        where TEvent1 : struct
        where TEvent2 : struct
    {
        return new ActorQueryResult(this, GetOrBuildQueryCache(CreateQuerySignature(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id)));
    }

    private ActorQueryCache GetOrBuildQueryCache(BehaviourSignature querySignature)
    {
        if (_queryCacheBySignature.TryGetValue(querySignature, out ActorQueryCache? cache))
        {
            return cache;
        }

        cache = BuildQueryCache(querySignature);
        _queryCacheBySignature.Add(querySignature, cache);
        return cache;
    }

    private ActorQueryCache BuildQueryCache(BehaviourSignature querySignature)
    {
        var matched = new List<BehaviourArchetype>();
        foreach (BehaviourArchetype archetype in _archetypes)
        {
            if (archetype.Signature.ContainsAll(querySignature))
            {
                matched.Add(archetype);
            }
        }

        return new ActorQueryCache(querySignature, matched.ToArray());
    }

    private static BehaviourSignature CreateQuerySignature(params int[] eventTypeIds)
    {
        Array.Sort(eventTypeIds);

        int uniqueCount = 0;
        for (int i = 0; i < eventTypeIds.Length; i++)
        {
            if (i == 0 || eventTypeIds[i] != eventTypeIds[i - 1])
            {
                eventTypeIds[uniqueCount++] = eventTypeIds[i];
            }
        }

        if (uniqueCount != eventTypeIds.Length)
        {
            Array.Resize(ref eventTypeIds, uniqueCount);
        }

        return new BehaviourSignature(eventTypeIds);
    }
}
