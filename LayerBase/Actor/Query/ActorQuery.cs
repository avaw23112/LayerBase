using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public ActorQueryBuilder Query()
    {
        return new ActorQueryBuilder(this);
    }

    public ActorQueryResult QueryActor<TEvent>()
        where TEvent : struct
    {
        return QueryAll(EventTypeId<TEvent>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2>()
        where TEvent1 : struct
        where TEvent2 : struct
    {
        return QueryAll(EventTypeId<TEvent1>.Id, EventTypeId<TEvent2>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
    {
        return QueryAll(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id,
            EventTypeId<TEvent3>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
    {
        return QueryAll(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id,
            EventTypeId<TEvent3>.Id,
            EventTypeId<TEvent4>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
    {
        return QueryAll(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id,
            EventTypeId<TEvent3>.Id,
            EventTypeId<TEvent4>.Id,
            EventTypeId<TEvent5>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
    {
        return QueryAll(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id,
            EventTypeId<TEvent3>.Id,
            EventTypeId<TEvent4>.Id,
            EventTypeId<TEvent5>.Id,
            EventTypeId<TEvent6>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
    {
        return QueryAll(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id,
            EventTypeId<TEvent3>.Id,
            EventTypeId<TEvent4>.Id,
            EventTypeId<TEvent5>.Id,
            EventTypeId<TEvent6>.Id,
            EventTypeId<TEvent7>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
    {
        return QueryAll(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id,
            EventTypeId<TEvent3>.Id,
            EventTypeId<TEvent4>.Id,
            EventTypeId<TEvent5>.Id,
            EventTypeId<TEvent6>.Id,
            EventTypeId<TEvent7>.Id,
            EventTypeId<TEvent8>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
        where TEvent9 : struct
    {
        return QueryAll(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id,
            EventTypeId<TEvent3>.Id,
            EventTypeId<TEvent4>.Id,
            EventTypeId<TEvent5>.Id,
            EventTypeId<TEvent6>.Id,
            EventTypeId<TEvent7>.Id,
            EventTypeId<TEvent8>.Id,
            EventTypeId<TEvent9>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
        where TEvent9 : struct
        where TEvent10 : struct
    {
        return QueryAll(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id,
            EventTypeId<TEvent3>.Id,
            EventTypeId<TEvent4>.Id,
            EventTypeId<TEvent5>.Id,
            EventTypeId<TEvent6>.Id,
            EventTypeId<TEvent7>.Id,
            EventTypeId<TEvent8>.Id,
            EventTypeId<TEvent9>.Id,
            EventTypeId<TEvent10>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
        where TEvent9 : struct
        where TEvent10 : struct
        where TEvent11 : struct
    {
        return QueryAll(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id,
            EventTypeId<TEvent3>.Id,
            EventTypeId<TEvent4>.Id,
            EventTypeId<TEvent5>.Id,
            EventTypeId<TEvent6>.Id,
            EventTypeId<TEvent7>.Id,
            EventTypeId<TEvent8>.Id,
            EventTypeId<TEvent9>.Id,
            EventTypeId<TEvent10>.Id,
            EventTypeId<TEvent11>.Id);
    }

    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TEvent12>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
        where TEvent9 : struct
        where TEvent10 : struct
        where TEvent11 : struct
        where TEvent12 : struct
    {
        return QueryAll(
            EventTypeId<TEvent1>.Id,
            EventTypeId<TEvent2>.Id,
            EventTypeId<TEvent3>.Id,
            EventTypeId<TEvent4>.Id,
            EventTypeId<TEvent5>.Id,
            EventTypeId<TEvent6>.Id,
            EventTypeId<TEvent7>.Id,
            EventTypeId<TEvent8>.Id,
            EventTypeId<TEvent9>.Id,
            EventTypeId<TEvent10>.Id,
            EventTypeId<TEvent11>.Id,
            EventTypeId<TEvent12>.Id);
    }

    internal ActorQueryResult GetOrBuildQuery(ActorQueryDescriptor descriptor)
    {
        if (_queryCacheByDescriptor.TryGetValue(descriptor, out ActorQueryCache? cache))
        {
            return new ActorQueryResult(this, cache, QueryVersion);
        }

        cache = BuildQueryCache(descriptor);
        _queryCacheByDescriptor.Add(descriptor, cache);
        return new ActorQueryResult(this, cache, QueryVersion);
    }

    internal ActorQueryResult RebuildQuery(ActorQueryDescriptor descriptor)
    {
        ActorQueryCache cache = BuildQueryCache(descriptor);
        _queryCacheByDescriptor[descriptor] = cache;
        return new ActorQueryResult(this, cache, QueryVersion);
    }

    private ActorQueryResult QueryAll(params int[] eventTypeIds)
    {
        return GetOrBuildQuery(new ActorQueryDescriptor(
            CreateBehaviourSignature(eventTypeIds),
            BehaviourSignature.Empty,
            ActorTagSignature.Empty,
            ActorTagSignature.Empty,
            ActorGroupSignature.Empty,
            ActorGroupSignature.Empty));
    }

    private ActorQueryCache BuildQueryCache(ActorQueryDescriptor descriptor)
    {
        var matched = new List<BehaviourArchetype>();
        foreach (BehaviourArchetype archetype in _archetypes)
        {
            if (IsMatch(archetype, descriptor))
            {
                matched.Add(archetype);
            }
        }

        return new ActorQueryCache(descriptor, matched.ToArray());
    }

    private static bool IsMatch(BehaviourArchetype archetype, ActorQueryDescriptor descriptor)
    {
        if (!archetype.Signature.ContainsAll(descriptor.AllBehaviours))
        {
            return false;
        }

        if (archetype.Signature.ContainsAny(descriptor.NoneBehaviours))
        {
            return false;
        }

        if (!archetype.Tags.ContainsAll(descriptor.AllTags))
        {
            return false;
        }

        if (archetype.Tags.ContainsAny(descriptor.NoneTags))
        {
            return false;
        }

        if (!archetype.Groups.ContainsAll(descriptor.AllGroups))
        {
            return false;
        }

        if (archetype.Groups.ContainsAny(descriptor.NoneGroups))
        {
            return false;
        }

        return true;
    }

    private static BehaviourSignature CreateBehaviourSignature(params int[] eventTypeIds)
    {
        return new BehaviourSignature(ActorSignatureUtility.Normalize(eventTypeIds));
    }
}
