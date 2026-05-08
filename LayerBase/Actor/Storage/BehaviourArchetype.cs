using LayerBase.Core.Event;

namespace LayerBase.Actor;

internal sealed class BehaviourArchetype
{
    private TypedStorageRuntime[] _storages = Array.Empty<TypedStorageRuntime>();
    private readonly Dictionary<Type, ushort> _storageIndexByType = new();

    public int ArchetypeId { get; }
    public BehaviourSignature Signature { get; }

    public BehaviourArchetype(int archetypeId, BehaviourSignature signature)
    {
        ArchetypeId = archetypeId;
        Signature = signature;
    }
    internal bool IsLifecycleRunnable(ActorId actorId)
    {
        // actorId 参数表示目标 Actor。
        // TypeStorageIndex 用于定位当前 Archetype 内的具体 Actor 类型存储。
        ushort storageIndex = actorId.TypeStorageIndex;

        if (storageIndex >= (uint)_storages.Length)
        {
            return false;
        }

        return _storages[storageIndex].IsLifecycleRunnable(
            slotIndex: actorId.SlotIndex,
            generation: actorId.Generation);
    }
    public TypedActorStorage<TActor> GetOrCreateStorage<TActor>(ActorTypeMeta<TActor> meta, ActorWorld world)
        where TActor : class, IActor
    {
        Type actorType = typeof(TActor);
        if (_storageIndexByType.TryGetValue(actorType, out ushort existingIndex))
        {
            return (TypedActorStorage<TActor>)_storages[existingIndex];
        }

        ushort storageIndex = checked((ushort)_storages.Length);
        var storage = new TypedActorStorage<TActor>(
            typeStorageIndex: storageIndex,
            maxEventTypeId: Math.Max(EventTypeIdAllocator.MaxId, 1),
            initialCapacity: 4);

        storage.BuildColumns(meta, world);

        Array.Resize(ref _storages, storageIndex + 1);
        _storages[storageIndex] = storage;
        _storageIndexByType.Add(actorType, storageIndex);
        return storage;
    }

    public PostResult Post<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct
    {
        ushort storageIndex = actorId.TypeStorageIndex;
        if ((uint)storageIndex >= (uint)_storages.Length)
        {
            return PostResult.Failure("Invalid ActorId.TypeStorageIndex.");
        }

        TypedStorageRuntime storage = _storages[storageIndex];
        if (!storage.IsAlive(actorId.SlotIndex, actorId.Generation))
        {
            return PostResult.Failure("ActorId is stale or actor slot is not alive.");
        }

        return storage.Post(actorId.SlotIndex, in value, postPolicy, fullPolicy);
    }

    internal bool IsAlive(ActorId actorId)
    {
        ushort storageIndex = actorId.TypeStorageIndex;
        if ((uint)storageIndex >= (uint)_storages.Length)
        {
            return false;
        }

        return _storages[storageIndex].IsAlive(actorId.SlotIndex, actorId.Generation);
    }

    internal bool IsEnable(ActorId actorId)
    {
        ushort storageIndex = actorId.TypeStorageIndex;
        if ((uint)storageIndex >= (uint)_storages.Length)
        {
            return false;
        }

        return _storages[storageIndex].IsEnable(actorId.SlotIndex, actorId.Generation);
    }

    internal bool SetEnable(ActorId actorId, bool enable)
    {
        ushort storageIndex = actorId.TypeStorageIndex;
        if ((uint)storageIndex >= (uint)_storages.Length)
        {
            return false;
        }

        return _storages[storageIndex].SetEnable(actorId.SlotIndex, actorId.Generation, enable);
    }

    internal bool MarkPendingDestroy(ActorId actorId)
    {
        ushort storageIndex = actorId.TypeStorageIndex;
        if ((uint)storageIndex >= (uint)_storages.Length)
        {
            return false;
        }

        return _storages[storageIndex].MarkPendingDestroy(actorId.SlotIndex, actorId.Generation);
    }

    internal void SweepPendingDestroy(ActorWorld world)
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            _storages[i].SweepPendingDestroy(world);
        }
    }
    
    public IEnumerable<IActor> EnumerateActors()
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            foreach (IActor actor in _storages[i].EnumerateActors())
            {
                yield return actor;
            }
        }
    }
    
   public void PostToAliveActors<TEvent>(
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
    where TEvent : struct
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            _storages[i].PostToAliveActors(in value, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2>(
        in TEvent1 value1,
        in TEvent2 value2,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent1 : struct
        where TEvent2 : struct
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2, TEvent3>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value3, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value3, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value4, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value3, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value4, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value5, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value3, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value4, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value5, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value6, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value3, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value4, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value5, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value6, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value7, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
        where TEvent4 : struct
        where TEvent5 : struct
        where TEvent6 : struct
        where TEvent7 : struct
        where TEvent8 : struct
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value3, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value4, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value5, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value6, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value7, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value8, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        in TEvent9 value9,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
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
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value3, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value4, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value5, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value6, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value7, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value8, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value9, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        in TEvent9 value9,
        in TEvent10 value10,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
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
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value3, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value4, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value5, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value6, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value7, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value8, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value9, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value10, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        in TEvent9 value9,
        in TEvent10 value10,
        in TEvent11 value11,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
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
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value3, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value4, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value5, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value6, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value7, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value8, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value9, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value10, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value11, postPolicy, fullPolicy);
        }
    }

    public void PostToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TEvent12>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        in TEvent4 value4,
        in TEvent5 value5,
        in TEvent6 value6,
        in TEvent7 value7,
        in TEvent8 value8,
        in TEvent9 value9,
        in TEvent10 value10,
        in TEvent11 value11,
        in TEvent12 value12,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
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
        for (int i = 0; i < _storages.Length; i++)
        {
            TypedStorageRuntime storage = _storages[i];

            storage.PostToAliveActors(in value1, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value2, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value3, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value4, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value5, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value6, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value7, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value8, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value9, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value10, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value11, postPolicy, fullPolicy);
            storage.PostToAliveActors(in value12, postPolicy, fullPolicy);
        }
    }
}
