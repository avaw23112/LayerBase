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
}
