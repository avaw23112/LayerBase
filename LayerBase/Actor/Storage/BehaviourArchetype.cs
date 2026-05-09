using LayerBase.Core.Event;
using LayerBase.Async;
using System.Text;

namespace LayerBase.Actor;

internal sealed class BehaviourArchetype
{
    private TypedStorageRuntime[] _storages = Array.Empty<TypedStorageRuntime>();
    private readonly Dictionary<Type, ushort> _storageIndexByType = new();

    public int ArchetypeId { get; }
    public BehaviourSignature Signature { get; }
    public ActorTagSignature Tags { get; }
    public ActorGroupSignature Groups { get; }

    public BehaviourArchetype(
        int archetypeId,
        BehaviourSignature signature,
        ActorTagSignature tags,
        ActorGroupSignature groups)
    {
        ArchetypeId = archetypeId;
        Signature = signature;
        Tags = tags;
        Groups = groups;
    }

    internal bool IsLifecycleRunnable(ActorId actorId)
    {
        ushort storageIndex = actorId.TypeStorageIndex;
        if (storageIndex >= (uint)_storages.Length)
        {
            return false;
        }

        return _storages[storageIndex].IsLifecycleRunnable(
            actorId.SlotIndex,
            actorId.Generation);
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
            storageIndex,
            ArchetypeId,
            Math.Max(EventTypeIdAllocator.MaxId, 1),
            4);

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
            return PostResult.Failure(
                ActorPostStatus.ActorNotFound,
                "Invalid ActorId.TypeStorageIndex.",
                PostFailureKind.InvalidActorId);
        }

        TypedStorageRuntime storage = _storages[storageIndex];
        if (!storage.IsAlive(actorId.SlotIndex, actorId.Generation))
        {
            ActorSlotState state = storage.GetSlotState(actorId.SlotIndex);
            if (state == ActorSlotState.PendingDestroy)
            {
                return PostResult.Failure(
                    ActorPostStatus.ActorPendingDestroy,
                    "Actor is pending destroy.",
                    PostFailureKind.PendingDestroy);
            }

            if (state == ActorSlotState.Destroying)
            {
                return PostResult.Failure(
                    ActorPostStatus.ActorNotAlive,
                    "Actor is destroying.",
                    PostFailureKind.Destroying);
            }

            if (storage.GetGeneration(actorId.SlotIndex) != actorId.Generation)
            {
                return PostResult.Failure(
                    ActorPostStatus.ActorNotFound,
                    "ActorId generation mismatch.",
                    PostFailureKind.InvalidActorId);
            }

            return PostResult.Failure(
                ActorPostStatus.ActorNotAlive,
                "ActorId is stale or actor slot is not alive.",
                PostFailureKind.InvalidActorId);
        }

        return storage.Post(actorId.SlotIndex, in value, postPolicy, fullPolicy);
    }

    public PostResult PostCall<TRequest, TResponse>(
        ActorId actorId,
        in ActorCallMail<TRequest, TResponse> mail)
        where TRequest : struct
        where TResponse : struct
    {
        ushort storageIndex = actorId.TypeStorageIndex;
        if ((uint)storageIndex >= (uint)_storages.Length)
        {
            return PostResult.Failure(
                ActorPostStatus.ActorNotFound,
                "Invalid ActorId.TypeStorageIndex.",
                PostFailureKind.InvalidActorId);
        }

        TypedStorageRuntime storage = _storages[storageIndex];
        if (!storage.IsAlive(actorId.SlotIndex, actorId.Generation))
        {
            ActorSlotState state = storage.GetSlotState(actorId.SlotIndex);
            if (state == ActorSlotState.PendingDestroy)
            {
                return PostResult.Failure(
                    ActorPostStatus.ActorPendingDestroy,
                    "Actor is pending destroy.",
                    PostFailureKind.PendingDestroy);
            }

            return PostResult.Failure(
                ActorPostStatus.ActorNotAlive,
                "ActorId is stale or actor slot is not alive.",
                PostFailureKind.InvalidActorId);
        }

        return storage.PostCall(actorId.SlotIndex, in mail);
    }

    public DispatchResult DispatchNow<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        ushort storageIndex = actorId.TypeStorageIndex;
        if ((uint)storageIndex >= (uint)_storages.Length)
        {
            return DispatchResult.Failure(
                DispatchFailureKind.InvalidActorId,
                "Invalid ActorId.TypeStorageIndex.");
        }

        TypedStorageRuntime storage = _storages[storageIndex];
        ActorSlotState state = storage.GetSlotState(actorId.SlotIndex);
        if (state == ActorSlotState.PendingDestroy)
        {
            return DispatchResult.Failure(
                DispatchFailureKind.PendingDestroy,
                "Actor is pending destroy.");
        }

        if (!storage.IsAlive(actorId.SlotIndex, actorId.Generation))
        {
            return DispatchResult.Failure(
                DispatchFailureKind.ActorNotFound,
                "ActorId is stale or actor slot is not alive.");
        }

        return storage.DispatchNow(actorId.SlotIndex, actorId.Generation, in value);
    }

    public LBTask<TResponse> ImmediatelyAsk<TRequest, TResponse>(
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken)
        where TRequest : struct
        where TResponse : struct
    {
        ushort storageIndex = actorId.TypeStorageIndex;
        if ((uint)storageIndex >= (uint)_storages.Length)
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                actorId,
                ActorCallFailureKind.InvalidActorId);
        }

        TypedStorageRuntime storage = _storages[storageIndex];
        ActorSlotState state = storage.GetSlotState(actorId.SlotIndex);
        if (state == ActorSlotState.PendingDestroy)
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                actorId,
                ActorCallFailureKind.PendingDestroy);
        }

        return storage.ImmediatelyAsk<TRequest, TResponse>(
            actorId.SlotIndex,
            actorId.Generation,
            in request,
            cancellationToken);
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

    internal ActorDebugInfo GetDebugInfo(ActorId actorId)
    {
        ushort storageIndex = actorId.TypeStorageIndex;
        if ((uint)storageIndex >= (uint)_storages.Length)
        {
            return ActorDebugInfo.Invalid(actorId, "Invalid TypeStorageIndex.");
        }

        return _storages[storageIndex].GetDebugInfo(actorId, Describe());
    }

    internal string Describe()
    {
        return $"Behaviours=[{string.Join(", ", Signature.EventTypeIds.ToArray())}], Tags=[{string.Join(", ", Tags.Ids.ToArray())}], Groups=[{string.Join(", ", Groups.Ids.ToArray())}]";
    }

    internal int CountAlive()
    {
        int count = 0;
        for (int i = 0; i < _storages.Length; i++)
        {
            count += _storages[i].CountAlive();
        }

        return count;
    }

    internal int CountEnabled()
    {
        int count = 0;
        for (int i = 0; i < _storages.Length; i++)
        {
            count += _storages[i].CountEnabled();
        }

        return count;
    }

    internal bool HasAnyAlive()
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            if (_storages[i].HasAnyAlive())
            {
                return true;
            }
        }

        return false;
    }

    internal int GetTotalPendingMailCount()
    {
        int count = 0;
        for (int i = 0; i < _storages.Length; i++)
        {
            count += _storages[i].GetTotalPendingMailCount();
        }

        return count;
    }

    internal void AppendDebugRows(StringBuilder builder)
    {
        string archetypeInfo = Describe();
        for (int i = 0; i < _storages.Length; i++)
        {
            _storages[i].AppendDebugRow(builder, ArchetypeId, archetypeInfo);
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

    internal void ForEachActor<TActor>(Action<TActor> action)
        where TActor : class, IActor
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            if (_storages[i] is TypedActorStorage<TActor> storage)
            {
                storage.ForEachActor(action);
            }
        }
    }

    internal void ForEachActor<TActor, TState>(ref TState state, ActorForEachAction<TActor, TState> action)
        where TActor : class, IActor
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            if (_storages[i] is TypedActorStorage<TActor> storage)
            {
                storage.ForEachActor(ref state, action);
            }
        }
    }

    internal void ForEachStorage<TActor, TState>(ref TState state, ActorStorageForEachAction<TActor, TState> action)
        where TActor : class, IActor
    {
        for (int i = 0; i < _storages.Length; i++)
        {
            if (_storages[i] is TypedActorStorage<TActor> storage)
            {
                storage.ForEachStorage(ref state, action);
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
            _storages[i].PostManyToAliveActors(in value1, in value2, postPolicy, fullPolicy);
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
            _storages[i].PostManyToAliveActors(in value1, in value2, in value3, postPolicy, fullPolicy);
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
            _storages[i].PostManyToAliveActors(in value1, in value2, in value3, in value4, postPolicy, fullPolicy);
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
            _storages[i].PostManyToAliveActors(in value1, in value2, in value3, in value4, in value5, postPolicy, fullPolicy);
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
            _storages[i].PostManyToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, postPolicy, fullPolicy);
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
            _storages[i].PostManyToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, in value7, postPolicy, fullPolicy);
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
            _storages[i].PostManyToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, in value7, in value8, postPolicy, fullPolicy);
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
            _storages[i].PostManyToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, in value7, in value8, in value9, postPolicy, fullPolicy);
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
            _storages[i].PostManyToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, in value7, in value8, in value9, in value10, postPolicy, fullPolicy);
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
            _storages[i].PostManyToAliveActors(in value1, in value2, in value3, in value4, in value5, in value6, in value7, in value8, in value9, in value10, in value11, postPolicy, fullPolicy);
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
            _storages[i].PostManyToAliveActors(
                in value1,
                in value2,
                in value3,
                in value4,
                in value5,
                in value6,
                in value7,
                in value8,
                in value9,
                in value10,
                in value11,
                in value12,
                postPolicy,
                fullPolicy);
        }
    }
}
