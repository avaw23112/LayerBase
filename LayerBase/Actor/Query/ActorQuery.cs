using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public ActorQueryResult QueryActor<TEvent>()
        where TEvent : struct
    {
        return new ActorQueryResult(this, GetOrBuildQueryCache(CreateQuerySignature(EventTypeId<TEvent>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1、TEvent2 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    public ActorQueryResult QueryActor<TEvent1, TEvent2>()
        where TEvent1 : struct
        where TEvent2 : struct
    {
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
                EventTypeId<TEvent1>.Id,
                EventTypeId<TEvent2>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1、TEvent2、TEvent3 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent3">第 3 个行为标记类型。</typeparam>
    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
    {
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
                EventTypeId<TEvent1>.Id,
                EventTypeId<TEvent2>.Id,
                EventTypeId<TEvent3>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1 到 TEvent4 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent3">第 3 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent4">第 4 个行为标记类型。</typeparam>
    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
    {
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
                EventTypeId<TEvent1>.Id,
                EventTypeId<TEvent2>.Id,
                EventTypeId<TEvent3>.Id,
                EventTypeId<TEvent4>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1 到 TEvent5 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent3">第 3 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent4">第 4 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent5">第 5 个行为标记类型。</typeparam>
    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
    {
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
                EventTypeId<TEvent1>.Id,
                EventTypeId<TEvent2>.Id,
                EventTypeId<TEvent3>.Id,
                EventTypeId<TEvent4>.Id,
                EventTypeId<TEvent5>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1 到 TEvent6 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent3">第 3 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent4">第 4 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent5">第 5 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent6">第 6 个行为标记类型。</typeparam>
    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
    {
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
                EventTypeId<TEvent1>.Id,
                EventTypeId<TEvent2>.Id,
                EventTypeId<TEvent3>.Id,
                EventTypeId<TEvent4>.Id,
                EventTypeId<TEvent5>.Id,
                EventTypeId<TEvent6>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1 到 TEvent7 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent3">第 3 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent4">第 4 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent5">第 5 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent6">第 6 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent7">第 7 个行为标记类型。</typeparam>
    public ActorQueryResult QueryActor<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>()
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
    {
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
                EventTypeId<TEvent1>.Id,
                EventTypeId<TEvent2>.Id,
                EventTypeId<TEvent3>.Id,
                EventTypeId<TEvent4>.Id,
                EventTypeId<TEvent5>.Id,
                EventTypeId<TEvent6>.Id,
                EventTypeId<TEvent7>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1 到 TEvent8 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent3">第 3 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent4">第 4 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent5">第 5 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent6">第 6 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent7">第 7 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent8">第 8 个行为标记类型。</typeparam>
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
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
                EventTypeId<TEvent1>.Id,
                EventTypeId<TEvent2>.Id,
                EventTypeId<TEvent3>.Id,
                EventTypeId<TEvent4>.Id,
                EventTypeId<TEvent5>.Id,
                EventTypeId<TEvent6>.Id,
                EventTypeId<TEvent7>.Id,
                EventTypeId<TEvent8>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1 到 TEvent9 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent3">第 3 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent4">第 4 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent5">第 5 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent6">第 6 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent7">第 7 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent8">第 8 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent9">第 9 个行为标记类型。</typeparam>
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
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
                EventTypeId<TEvent1>.Id,
                EventTypeId<TEvent2>.Id,
                EventTypeId<TEvent3>.Id,
                EventTypeId<TEvent4>.Id,
                EventTypeId<TEvent5>.Id,
                EventTypeId<TEvent6>.Id,
                EventTypeId<TEvent7>.Id,
                EventTypeId<TEvent8>.Id,
                EventTypeId<TEvent9>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1 到 TEvent10 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent3">第 3 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent4">第 4 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent5">第 5 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent6">第 6 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent7">第 7 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent8">第 8 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent9">第 9 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent10">第 10 个行为标记类型。</typeparam>
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
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
                EventTypeId<TEvent1>.Id,
                EventTypeId<TEvent2>.Id,
                EventTypeId<TEvent3>.Id,
                EventTypeId<TEvent4>.Id,
                EventTypeId<TEvent5>.Id,
                EventTypeId<TEvent6>.Id,
                EventTypeId<TEvent7>.Id,
                EventTypeId<TEvent8>.Id,
                EventTypeId<TEvent9>.Id,
                EventTypeId<TEvent10>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1 到 TEvent11 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent3">第 3 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent4">第 4 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent5">第 5 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent6">第 6 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent7">第 7 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent8">第 8 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent9">第 9 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent10">第 10 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent11">第 11 个行为标记类型。</typeparam>
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
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
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
                EventTypeId<TEvent11>.Id)));
    }

    /// <summary>
    /// 查询同时拥有 TEvent1 到 TEvent12 行为标记的 Actor。
    /// </summary>
    /// <typeparam name="TEvent1">第 1 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent2">第 2 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent3">第 3 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent4">第 4 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent5">第 5 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent6">第 6 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent7">第 7 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent8">第 8 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent9">第 9 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent10">第 10 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent11">第 11 个行为标记类型。</typeparam>
    /// <typeparam name="TEvent12">第 12 个行为标记类型。</typeparam>
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
        return new ActorQueryResult(
            this,
            GetOrBuildQueryCache(CreateQuerySignature(
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
                EventTypeId<TEvent12>.Id)));
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
