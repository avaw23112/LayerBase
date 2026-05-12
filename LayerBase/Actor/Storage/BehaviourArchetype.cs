using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Async;
using System.Text;
using LayerBase.ECS.Projection;

namespace LayerBase.Actor;

internal sealed class BehaviourArchetype
{
    private TypedStorageRuntime[] _storages = Array.Empty<TypedStorageRuntime>();

    public int ArchetypeId { get; }
    public BehaviourSignature Signature { get; }
    public ActorTagSignature Tags { get; }
    public ActorGroupSignature Groups { get; }

    public BehaviourArchetype(
        int                 archetypeId,
        BehaviourSignature  signature,
        ActorTagSignature   tags,
        ActorGroupSignature groups)
    {
        ArchetypeId = archetypeId;
        Signature = signature;
        Tags = tags;
        Groups = groups;
    }

    internal bool IsLifecycleRunnable(ActorId actorId)
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return false;
        }

        return storage.IsLifecycleRunnable(
            actorId.SlotIndex,
            actorId.Generation);
    }

    internal bool TryGetActor(
        ActorId     actorId,
        out IActor? actor)
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            actor = null;
            return false;
        }

        return storage.TryGetActor(
            actorId,
            out actor);
    }

    internal bool ReleaseProjectedActor(
        ActorId                     actorId,
        ActorWorld                  world,
        ProjectedActorReleasePolicy releasePolicy)
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return false;
        }

        return storage.ReleaseProjectedActor(
            actorId,
            world,
            releasePolicy);
    }

    public TypedActorStorage<TActor> GetOrCreateStorage<TActor>(ActorTypeMeta<TActor> meta, ActorWorld world)
        where TActor : class, IActor
    {
        if (_storages.Length > 0)
        {
            return (TypedActorStorage<TActor>)_storages[0];
        }

        var storage = new TypedActorStorage<TActor>(
            ArchetypeId,
            Math.Max(EventTypeIdAllocator.MaxId, 1),
            4);

        storage.BuildColumns(meta, world);

        Array.Resize(ref _storages, 1);
        _storages[0] = storage;
        return storage;
    }

    public PostResult PostCall<TRequest, TResponse>(
        ActorId                               actorId,
        in ActorCallMail<TRequest, TResponse> mail)
        where TRequest : struct
        where TResponse : struct
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return PostResult.Failure(
                ActorPostStatus.ActorNotFound,
                PostFailureKind.InvalidActorId);
        }

        if (!storage.IsAlive(actorId.SlotIndex, actorId.Generation))
        {
            ActorSlotState state = storage.GetSlotState(actorId.SlotIndex);
            if (state == ActorSlotState.PendingDestroy)
            {
                return PostResult.Failure(
                    ActorPostStatus.ActorPendingDestroy,
                    PostFailureKind.PendingDestroy);
            }

            return PostResult.Failure(
                ActorPostStatus.ActorNotAlive,
                PostFailureKind.InvalidActorId);
        }

        return storage.PostCall(actorId.SlotIndex, in mail);
    }

    public DispatchResult DispatchNow<TEvent>(
        ActorId   actorId,
        in TEvent value)
        where TEvent : struct
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return DispatchResult.Failure(
                DispatchFailureKind.InvalidActorId,
                "Actor archetype storage is missing.");
        }

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
        ActorId           actorId,
        in TRequest       request,
        CancellationToken cancellationToken)
        where TRequest : struct
        where TResponse : struct
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                actorId,
                ActorCallFailureKind.InvalidActorId);
        }

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
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return false;
        }

        return storage.IsAlive(actorId.SlotIndex, actorId.Generation);
    }

    internal bool IsEnable(ActorId actorId)
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return false;
        }

        return storage.IsEnable(actorId.SlotIndex, actorId.Generation);
    }

    internal bool TryGetStorage<TActor>(out TypedActorStorage<TActor>? storage)
        where TActor : class, IActor
    {
        if (!TryGetStorage(out TypedStorageRuntime? rawStorage)
            || rawStorage is not TypedActorStorage<TActor> typedStorage)
        {
            storage = null;
            return false;
        }

        storage = typedStorage;
        return true;
    }

    internal bool SetEnable(ActorId actorId, bool enable)
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return false;
        }

        return storage.SetEnable(actorId.SlotIndex, actorId.Generation, enable);
    }

    internal bool MarkPendingDestroy(ActorId actorId)
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return false;
        }

        return storage.MarkPendingDestroy(actorId.SlotIndex, actorId.Generation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsCurrentGeneration(ActorId actorId)
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return false;
        }

        return storage.IsCurrentGeneration(actorId);
    }

    internal void PostAll<TEvent>(
        ActorWorld             world,
        EventPostState<TEvent> state,
        ActorPostRouteCode     routeCode,
        in TEvent              value)
        where TEvent : struct
    {
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return;
        }

        storage.PostAll(world, state, routeCode, in value);
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
        if (!TryGetStorage(out TypedStorageRuntime? storage)
            || storage == null)
        {
            return ActorDebugInfo.Invalid(actorId, "Actor archetype storage is missing.");
        }

        return storage.GetDebugInfo(actorId, Describe());
    }

    private bool TryGetStorage(out TypedStorageRuntime? storage)
    {
        if (_storages.Length == 0)
        {
            storage = null;
            return false;
        }

        storage = _storages[0];
        return storage != null;
    }

    internal string Describe()
    {
        return
            $"Behaviours=[{string.Join(", ", Signature.EventTypeIds.ToArray())}], Tags=[{string.Join(", ", Tags.Ids.ToArray())}], Groups=[{string.Join(", ", Groups.Ids.ToArray())}]";
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

    // PostToAliveActors: removed. Default post path no longer exists.
    // Use ActorWorld.PostTo or PostAll through compiled route.
    // PostManyToAliveActors: removed. Same reason as above.

    // PostToAliveActors<TEvent3..TEvent12>: removed.
    // PostManyToAliveActors: removed. Use PostAll through compiled route instead.
}