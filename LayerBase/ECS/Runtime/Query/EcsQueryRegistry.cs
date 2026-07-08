using Arch.Core;
using ArchQuery = Arch.Core.Query;

namespace LayerBase.ECS.Runtime.Query;

public sealed class EcsQueryRegistry
{
    private readonly World _world;
    private readonly Dictionary<EcsQueryKey, int> _queryIds = new();
    private readonly List<ArchQuery> _queries = new();

    public EcsQueryRegistry(World world)
    {
        _world = world;
    }

    public int GetOrCreate()
    {
        QueryDescription description = EcsQueryDescriptionCache.Description;
        return GetOrCreate(EcsQueryKey.Create(), in description);
    }

    public int GetOrCreate<T0>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0>(), in description);
    }

    public int GetOrCreate<T0, T1>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1>(), in description);
    }

    public int GetOrCreate<T0, T1, T2>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1, T2>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1, T2>(), in description);
    }

    public int GetOrCreate<T0, T1, T2, T3>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1, T2, T3>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1, T2, T3>(), in description);
    }

    public int GetOrCreate<T0, T1, T2, T3, T4>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1, T2, T3, T4>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1, T2, T3, T4>(), in description);
    }

    public int GetOrCreate<T0, T1, T2, T3, T4, T5>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1, T2, T3, T4, T5>(), in description);
    }

    public int GetOrCreate<T0, T1, T2, T3, T4, T5, T6>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1, T2, T3, T4, T5, T6>(), in description);
    }

    public int GetOrCreate<T0, T1, T2, T3, T4, T5, T6, T7>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6, T7>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1, T2, T3, T4, T5, T6, T7>(), in description);
    }

    public int GetOrCreate<T0, T1, T2, T3, T4, T5, T6, T7, T8>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6, T7, T8>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1, T2, T3, T4, T5, T6, T7, T8>(), in description);
    }

    public int GetOrCreate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(), in description);
    }

    public int GetOrCreate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(), in description);
    }

    public int GetOrCreate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()
    {
        QueryDescription description = EcsQueryDescriptionCache<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>.Description;
        return GetOrCreate(EcsQueryKey.Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(), in description);
    }

    public ArchQuery Get(int queryId)
    {
        return _queries[queryId];
    }

    private int GetOrCreate(EcsQueryKey key, in QueryDescription description)
    {
        if (_queryIds.TryGetValue(key, out int id))
        {
            return id;
        }

        ArchQuery query = _world.Query(in description);
        id = _queries.Count;
        _queries.Add(query);
        _queryIds.Add(key, id);
        return id;
    }
}
