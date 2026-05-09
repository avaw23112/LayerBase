using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using LayerBase.Core.Event;
using LayerBase.Async;

namespace LayerBase.Actor;

internal sealed class TypedActorStorage<TActor> : TypedStorageRuntime
    where TActor : class, IActor
{
    private delegate void PrewarmHotBinder(ActorWorld world, int fastIndex, int version, int slotIndex, int generation);

    private ActorEventColumnRuntime[] _columnsByEventId;
    private TActor?[] _actors;
    private int[] _generations;
    private int[] _fastIndices;
    private ActorSlotState[] _states;
    private ActorSlotFlags[] _slotFlags;
    private bool[] _enabled;
    private bool[] _createdFromPool;
    private ActorLifecycleHandles[] _lifecycleHandles;
    private ActorSlotFreeList _freeList;
    private PrewarmHotBinder[] _prewarmHotBinders = Array.Empty<PrewarmHotBinder>();
    private int _prewarmHotBinderCount;
    private int _nextSlotIndex;
    private readonly int _archetypeId;
    private readonly int _storageRouteId;
    private ActorTypeMeta<TActor>? _meta;
    private object?[] _callInvokersByRouteId = Array.Empty<object?>();
    private ActorCallColumnRuntime?[] _callColumnsByRouteId = Array.Empty<ActorCallColumnRuntime?>();
    private Type?[] _callRequestTypesByRouteId = Array.Empty<Type?>();
    private Type?[] _callResponseTypesByRouteId = Array.Empty<Type?>();

    public ushort TypeStorageIndex { get; }
    internal int StorageRouteId => _storageRouteId;
    public override string ActorTypeName => typeof(TActor).Name;
    public TActor?[] Actors => _actors;
    public ActorSlotState[] States => _states;
    public bool[] Enabled => _enabled;
    public int MaxSlot => Math.Min(_nextSlotIndex, _actors.Length);

    public TypedActorStorage(ushort typeStorageIndex, int archetypeId, int storageRouteId, int maxEventTypeId, int initialCapacity)
    {
        TypeStorageIndex = typeStorageIndex;
        _archetypeId = archetypeId;
        _storageRouteId = storageRouteId;
        _columnsByEventId = new ActorEventColumnRuntime[Math.Max(maxEventTypeId + 1, 1)];
        int capacity = Math.Max(initialCapacity, 1);
        _actors = new TActor?[capacity];
        _generations = new int[_actors.Length];
        _fastIndices = new int[_actors.Length];
        _states = new ActorSlotState[_actors.Length];
        _slotFlags = new ActorSlotFlags[_actors.Length];
        _enabled = new bool[_actors.Length];
        _createdFromPool = new bool[_actors.Length];
        _lifecycleHandles = new ActorLifecycleHandles[_actors.Length];
        for (int i = 0; i < _lifecycleHandles.Length; i++)
        {
            _lifecycleHandles[i] = ActorLifecycleHandles.Empty;
        }
        Array.Fill(_fastIndices, -1);

        _freeList = new ActorSlotFreeList(_actors.Length);
        _nextSlotIndex = 0;
    }

    public override bool IsLifecycleRunnable(int slotIndex, int generation)
    {
        return (uint)slotIndex < (uint)_actors.Length
               && _generations[slotIndex] == generation
               && _states[slotIndex] == ActorSlotState.Alive
               && _enabled[slotIndex]
               && _actors[slotIndex] != null;
    }

    public int AllocateSlot(TActor actor, bool createdFromPool)
    {
        int slotIndex = _freeList.TryPop(out int freeSlot)
            ? freeSlot
            : AllocateNewSlot();

        _actors[slotIndex] = actor;
        _states[slotIndex] = ActorSlotState.Alive;
        _enabled[slotIndex] = true;
        _slotFlags[slotIndex] = ActorSlotFlags.Alive | ActorSlotFlags.Enabled;
        _fastIndices[slotIndex] = -1;
        _createdFromPool[slotIndex] = createdFromPool;
        _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;
        EnsureColumnCapacity(slotIndex);
        return slotIndex;
    }

    internal void BindFastIndex(int slotIndex, int fastIndex)
    {
        _fastIndices[slotIndex] = fastIndex;
    }

    public override int GetGeneration(int slotIndex)
    {
        return _generations[slotIndex];
    }

    public override bool IsAlive(int slotIndex, int generation)
    {
        return (uint)slotIndex < (uint)_actors.Length
               && _states[slotIndex] == ActorSlotState.Alive
               && _actors[slotIndex] != null
               && _generations[slotIndex] == generation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsAliveSlot(int slotIndex)
    {
        return (uint)slotIndex < (uint)_actors.Length
               && _states[slotIndex] == ActorSlotState.Alive
               && _actors[slotIndex] != null;
    }

    public override ActorSlotState GetSlotState(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_states.Length)
        {
            return ActorSlotState.Empty;
        }

        return _states[slotIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsSlotEnabled(int slotIndex)
    {
        return (uint)slotIndex < (uint)_enabled.Length
               && _enabled[slotIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CanPostFast(
        int slotIndex,
        ActorSlotFlags rejectMask,
        bool rejectDisabled)
    {
        if ((uint)slotIndex >= (uint)_slotFlags.Length)
        {
            return false;
        }

        ActorSlotFlags flags = _slotFlags[slotIndex];
        if ((flags & ActorSlotFlags.Alive) == 0)
        {
            return false;
        }

        if ((flags & rejectMask) != 0)
        {
            return false;
        }

        if (rejectDisabled && (flags & ActorSlotFlags.Enabled) == 0)
        {
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal long GetActorPumpKey(int slotIndex)
    {
        return ((long)_archetypeId << 32) | (uint)slotIndex;
    }

    public override bool IsEnable(int slotIndex, int generation)
    {
        return IsAlive(slotIndex, generation)
               && _enabled[slotIndex];
    }

    public override bool SetEnable(int slotIndex, int generation, bool enable)
    {
        if (!IsAlive(slotIndex, generation))
        {
            return false;
        }

        _enabled[slotIndex] = enable;
        if (enable)
        {
            _slotFlags[slotIndex] |= ActorSlotFlags.Enabled;
        }
        else
        {
            _slotFlags[slotIndex] &= ~ActorSlotFlags.Enabled;
        }
        
        var onEnable = _actors[slotIndex] as IEnable;
        var onDisable = _actors[slotIndex] as IDisable;
        if (onEnable != null && enable)
        {
            onEnable.OnEnable();
        }
        else if(onDisable != null && !enable)
        {
            onDisable.OnDisable();
        }
        return true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override PostResult Post<TEvent>(
        int slotIndex,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct
    {
        if (!TryGetColumn(out EventColumn<TActor, TEvent>? column))
        {
            return PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                $"Actor type {typeof(TActor).Name} does not support event {typeof(TEvent).Name}.",
                PostFailureKind.UnsupportedEvent);
        }

        return column.Post(slotIndex, in value, postPolicy, fullPolicy);
    }

    public override DispatchResult DispatchNow<TEvent>(
        int slotIndex,
        int generation,
        in TEvent value)
    {
        if (!IsAlive(slotIndex, generation))
        {
            return DispatchResult.Failure(
                DispatchFailureKind.ActorNotFound,
                "Actor slot is not alive.");
        }

        if (!TryGetColumn(out EventColumn<TActor, TEvent>? column))
        {
            return DispatchResult.Failure(
                DispatchFailureKind.UnsupportedEvent,
                $"Actor type {typeof(TActor).Name} does not support event {typeof(TEvent).Name}.");
        }

        TActor? actor = _actors[slotIndex];
        if (actor == null)
        {
            return DispatchResult.Failure(
                DispatchFailureKind.ActorNotFound,
                "Actor slot is empty.");
        }

        return column.DispatchNow(actor, in value);
    }

    public override LBTask<TResponse> ImmediatelyAsk<TRequest, TResponse>(
        int slotIndex,
        int generation,
        in TRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAlive(slotIndex, generation))
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                ActorCallFailureKind.ActorNotFound);
        }

        int routeId = ActorCallRouteId<TRequest, TResponse>.Id;
        object?[] invokers = _callInvokersByRouteId;

        if ((uint)routeId >= (uint)invokers.Length)
        {
            return ActorCallFailure.Unsupported<TResponse, TRequest, TResponse>();
        }

        var invoker = invokers[routeId] as ActorCallInvoker<TActor, TRequest, TResponse>;
        if (invoker == null)
        {
            return ActorCallFailure.Unsupported<TResponse, TRequest, TResponse>();
        }

        TActor? actor = _actors[slotIndex];
        if (actor == null)
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                ActorCallFailureKind.ActorNotFound);
        }

        try
        {
            return invoker(actor, in request, cancellationToken);
        }
        catch (Exception exception)
        {
            return LBTask<TResponse>.FromException(exception);
        }
    }

    public override PostResult PostCall<TRequest, TResponse>(
        int slotIndex,
        in ActorCallMail<TRequest, TResponse> mail)
    {
        int routeId = ActorCallRouteId<TRequest, TResponse>.Id;
        if ((uint)routeId >= (uint)_callColumnsByRouteId.Length)
        {
            return PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                $"Actor type {typeof(TActor).Name} does not support request {typeof(TRequest).Name} / response {typeof(TResponse).Name}.",
                PostFailureKind.UnsupportedEvent);
        }

        if (_callColumnsByRouteId[routeId] is not ActorCallColumn<TActor, TRequest, TResponse> column)
        {
            return PostResult.Failure(
                ActorPostStatus.EventNotSupported,
                $"Actor type {typeof(TActor).Name} does not support request {typeof(TRequest).Name} / response {typeof(TResponse).Name}.",
                PostFailureKind.UnsupportedEvent);
        }

        return column.Post(slotIndex, in mail);
    }

    public override void PostToAliveActors<TEvent>(
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
    {
        if (!TryGetColumn(out EventColumn<TActor, TEvent>? column))
        {
            return;
        }

        if (CanUsePostAllFastPath(column, postPolicy, fullPolicy))
        {
            column.PostToAliveSlotsFast(_actors, _states, _enabled, MaxSlot, in value);
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column.Post(slotIndex, in value, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2>(
        in TEvent1 value1,
        in TEvent2 value2,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent1 : struct
        where TEvent2 : struct
    {
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2, TEvent3>(
        in TEvent1 value1,
        in TEvent2 value2,
        in TEvent3 value3,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent1 : struct
        where TEvent2 : struct
        where TEvent3 : struct
    {
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent3>? column3))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column3, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
                _ = column3.PostQueuedFast(slotIndex, in value3);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
            _ = column3.Post(slotIndex, in value3, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4>(
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
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent3>? column3))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent4>? column4))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column3, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column4, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
                _ = column3.PostQueuedFast(slotIndex, in value3);
                _ = column4.PostQueuedFast(slotIndex, in value4);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
            _ = column3.Post(slotIndex, in value3, postPolicy, fullPolicy);
            _ = column4.Post(slotIndex, in value4, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>(
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
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent3>? column3))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent4>? column4))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent5>? column5))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column3, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column4, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column5, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
                _ = column3.PostQueuedFast(slotIndex, in value3);
                _ = column4.PostQueuedFast(slotIndex, in value4);
                _ = column5.PostQueuedFast(slotIndex, in value5);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
            _ = column3.Post(slotIndex, in value3, postPolicy, fullPolicy);
            _ = column4.Post(slotIndex, in value4, postPolicy, fullPolicy);
            _ = column5.Post(slotIndex, in value5, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6>(
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
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent3>? column3))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent4>? column4))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent5>? column5))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent6>? column6))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column3, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column4, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column5, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column6, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
                _ = column3.PostQueuedFast(slotIndex, in value3);
                _ = column4.PostQueuedFast(slotIndex, in value4);
                _ = column5.PostQueuedFast(slotIndex, in value5);
                _ = column6.PostQueuedFast(slotIndex, in value6);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
            _ = column3.Post(slotIndex, in value3, postPolicy, fullPolicy);
            _ = column4.Post(slotIndex, in value4, postPolicy, fullPolicy);
            _ = column5.Post(slotIndex, in value5, postPolicy, fullPolicy);
            _ = column6.Post(slotIndex, in value6, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7>(
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
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent3>? column3))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent4>? column4))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent5>? column5))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent6>? column6))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent7>? column7))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column3, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column4, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column5, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column6, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column7, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
                _ = column3.PostQueuedFast(slotIndex, in value3);
                _ = column4.PostQueuedFast(slotIndex, in value4);
                _ = column5.PostQueuedFast(slotIndex, in value5);
                _ = column6.PostQueuedFast(slotIndex, in value6);
                _ = column7.PostQueuedFast(slotIndex, in value7);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
            _ = column3.Post(slotIndex, in value3, postPolicy, fullPolicy);
            _ = column4.Post(slotIndex, in value4, postPolicy, fullPolicy);
            _ = column5.Post(slotIndex, in value5, postPolicy, fullPolicy);
            _ = column6.Post(slotIndex, in value6, postPolicy, fullPolicy);
            _ = column7.Post(slotIndex, in value7, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8>(
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
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent3>? column3))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent4>? column4))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent5>? column5))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent6>? column6))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent7>? column7))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent8>? column8))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column3, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column4, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column5, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column6, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column7, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column8, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
                _ = column3.PostQueuedFast(slotIndex, in value3);
                _ = column4.PostQueuedFast(slotIndex, in value4);
                _ = column5.PostQueuedFast(slotIndex, in value5);
                _ = column6.PostQueuedFast(slotIndex, in value6);
                _ = column7.PostQueuedFast(slotIndex, in value7);
                _ = column8.PostQueuedFast(slotIndex, in value8);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
            _ = column3.Post(slotIndex, in value3, postPolicy, fullPolicy);
            _ = column4.Post(slotIndex, in value4, postPolicy, fullPolicy);
            _ = column5.Post(slotIndex, in value5, postPolicy, fullPolicy);
            _ = column6.Post(slotIndex, in value6, postPolicy, fullPolicy);
            _ = column7.Post(slotIndex, in value7, postPolicy, fullPolicy);
            _ = column8.Post(slotIndex, in value8, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9>(
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
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent3>? column3))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent4>? column4))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent5>? column5))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent6>? column6))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent7>? column7))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent8>? column8))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent9>? column9))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column3, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column4, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column5, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column6, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column7, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column8, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column9, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
                _ = column3.PostQueuedFast(slotIndex, in value3);
                _ = column4.PostQueuedFast(slotIndex, in value4);
                _ = column5.PostQueuedFast(slotIndex, in value5);
                _ = column6.PostQueuedFast(slotIndex, in value6);
                _ = column7.PostQueuedFast(slotIndex, in value7);
                _ = column8.PostQueuedFast(slotIndex, in value8);
                _ = column9.PostQueuedFast(slotIndex, in value9);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
            _ = column3.Post(slotIndex, in value3, postPolicy, fullPolicy);
            _ = column4.Post(slotIndex, in value4, postPolicy, fullPolicy);
            _ = column5.Post(slotIndex, in value5, postPolicy, fullPolicy);
            _ = column6.Post(slotIndex, in value6, postPolicy, fullPolicy);
            _ = column7.Post(slotIndex, in value7, postPolicy, fullPolicy);
            _ = column8.Post(slotIndex, in value8, postPolicy, fullPolicy);
            _ = column9.Post(slotIndex, in value9, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10>(
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
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent3>? column3))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent4>? column4))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent5>? column5))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent6>? column6))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent7>? column7))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent8>? column8))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent9>? column9))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent10>? column10))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column3, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column4, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column5, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column6, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column7, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column8, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column9, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column10, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
                _ = column3.PostQueuedFast(slotIndex, in value3);
                _ = column4.PostQueuedFast(slotIndex, in value4);
                _ = column5.PostQueuedFast(slotIndex, in value5);
                _ = column6.PostQueuedFast(slotIndex, in value6);
                _ = column7.PostQueuedFast(slotIndex, in value7);
                _ = column8.PostQueuedFast(slotIndex, in value8);
                _ = column9.PostQueuedFast(slotIndex, in value9);
                _ = column10.PostQueuedFast(slotIndex, in value10);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
            _ = column3.Post(slotIndex, in value3, postPolicy, fullPolicy);
            _ = column4.Post(slotIndex, in value4, postPolicy, fullPolicy);
            _ = column5.Post(slotIndex, in value5, postPolicy, fullPolicy);
            _ = column6.Post(slotIndex, in value6, postPolicy, fullPolicy);
            _ = column7.Post(slotIndex, in value7, postPolicy, fullPolicy);
            _ = column8.Post(slotIndex, in value8, postPolicy, fullPolicy);
            _ = column9.Post(slotIndex, in value9, postPolicy, fullPolicy);
            _ = column10.Post(slotIndex, in value10, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11>(
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
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent3>? column3))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent4>? column4))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent5>? column5))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent6>? column6))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent7>? column7))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent8>? column8))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent9>? column9))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent10>? column10))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent11>? column11))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column3, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column4, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column5, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column6, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column7, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column8, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column9, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column10, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column11, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
                _ = column3.PostQueuedFast(slotIndex, in value3);
                _ = column4.PostQueuedFast(slotIndex, in value4);
                _ = column5.PostQueuedFast(slotIndex, in value5);
                _ = column6.PostQueuedFast(slotIndex, in value6);
                _ = column7.PostQueuedFast(slotIndex, in value7);
                _ = column8.PostQueuedFast(slotIndex, in value8);
                _ = column9.PostQueuedFast(slotIndex, in value9);
                _ = column10.PostQueuedFast(slotIndex, in value10);
                _ = column11.PostQueuedFast(slotIndex, in value11);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
            _ = column3.Post(slotIndex, in value3, postPolicy, fullPolicy);
            _ = column4.Post(slotIndex, in value4, postPolicy, fullPolicy);
            _ = column5.Post(slotIndex, in value5, postPolicy, fullPolicy);
            _ = column6.Post(slotIndex, in value6, postPolicy, fullPolicy);
            _ = column7.Post(slotIndex, in value7, postPolicy, fullPolicy);
            _ = column8.Post(slotIndex, in value8, postPolicy, fullPolicy);
            _ = column9.Post(slotIndex, in value9, postPolicy, fullPolicy);
            _ = column10.Post(slotIndex, in value10, postPolicy, fullPolicy);
            _ = column11.Post(slotIndex, in value11, postPolicy, fullPolicy);
        }
    }

    public override void PostManyToAliveActors<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5, TEvent6, TEvent7, TEvent8, TEvent9, TEvent10, TEvent11, TEvent12>(
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
        if (!TryGetColumn(out EventColumn<TActor, TEvent1>? column1))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent2>? column2))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent3>? column3))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent4>? column4))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent5>? column5))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent6>? column6))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent7>? column7))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent8>? column8))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent9>? column9))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent10>? column10))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent11>? column11))
        {
            return;
        }
        if (!TryGetColumn(out EventColumn<TActor, TEvent12>? column12))
        {
            return;
        }

        if (CanUsePostAllFastPath(column1, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column2, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column3, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column4, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column5, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column6, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column7, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column8, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column9, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column10, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column11, postPolicy, fullPolicy)
            && CanUsePostAllFastPath(column12, postPolicy, fullPolicy))
        {
            for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
            {
                if (!IsSlotPostable(slotIndex))
                {
                    continue;
                }

                _ = column1.PostQueuedFast(slotIndex, in value1);
                _ = column2.PostQueuedFast(slotIndex, in value2);
                _ = column3.PostQueuedFast(slotIndex, in value3);
                _ = column4.PostQueuedFast(slotIndex, in value4);
                _ = column5.PostQueuedFast(slotIndex, in value5);
                _ = column6.PostQueuedFast(slotIndex, in value6);
                _ = column7.PostQueuedFast(slotIndex, in value7);
                _ = column8.PostQueuedFast(slotIndex, in value8);
                _ = column9.PostQueuedFast(slotIndex, in value9);
                _ = column10.PostQueuedFast(slotIndex, in value10);
                _ = column11.PostQueuedFast(slotIndex, in value11);
                _ = column12.PostQueuedFast(slotIndex, in value12);
            }
            return;
        }

        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!IsSlotPostable(slotIndex))
            {
                continue;
            }

            _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
            _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
            _ = column3.Post(slotIndex, in value3, postPolicy, fullPolicy);
            _ = column4.Post(slotIndex, in value4, postPolicy, fullPolicy);
            _ = column5.Post(slotIndex, in value5, postPolicy, fullPolicy);
            _ = column6.Post(slotIndex, in value6, postPolicy, fullPolicy);
            _ = column7.Post(slotIndex, in value7, postPolicy, fullPolicy);
            _ = column8.Post(slotIndex, in value8, postPolicy, fullPolicy);
            _ = column9.Post(slotIndex, in value9, postPolicy, fullPolicy);
            _ = column10.Post(slotIndex, in value10, postPolicy, fullPolicy);
            _ = column11.Post(slotIndex, in value11, postPolicy, fullPolicy);
            _ = column12.Post(slotIndex, in value12, postPolicy, fullPolicy);
        }
    }

    public override IEnumerable<IActor> EnumerateActors()
    {
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (_states[slotIndex] != ActorSlotState.Alive)
            {
                continue;
            }

            if (_actors[slotIndex] is IActor actor)
            {
                yield return actor;
            }
        }
    }

    public void BuildColumns(ActorTypeMeta<TActor> meta, ActorWorld world)
    {
        _meta = meta;
        BuildCallRoutes(meta);
        BuildCallColumns(meta, world);

        foreach (ActorBehaviourEntry entry in meta.Behaviours)
        {
            EnsureEventColumnCapacity(entry.EventTypeId);
            _columnsByEventId[entry.EventTypeId] = entry.Factory(this, entry.Invoker, world, entry.BehaviourType);
        }
    }

    internal void ForEachActor(Action<TActor> action)
    {
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (_states[slotIndex] != ActorSlotState.Alive)
            {
                continue;
            }

            if (_actors[slotIndex] is TActor actor)
            {
                action(actor);
            }
        }
    }

    internal void ForEachActor<TState>(ref TState state, ActorForEachAction<TActor, TState> action)
    {
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (_states[slotIndex] != ActorSlotState.Alive)
            {
                continue;
            }

            if (_actors[slotIndex] is TActor actor)
            {
                action(actor, ref state);
            }
        }
    }

    internal void ForEachStorage<TState>(ref TState state, ActorStorageForEachAction<TActor, TState> action)
    {
        action(_actors, _states, _enabled, MaxSlot, ref state);
    }

    internal ActorEventColumnRuntime BuildColumnDirect<TEvent>(
        ActorWorld world,
        ActorBehaviourInvoker<TActor, TEvent> invoker,
        BehaviourType behaviourType)
        where TEvent : struct
    {
        int eventTypeId = EventTypeId<TEvent>.Id;
        ActorMailOptions options = world.ResolveMailOptions(eventTypeId);
        var column = new EventColumn<TActor, TEvent>(
            world: world,
            owner: this,
            invoker: invoker,
            options: options,
            behaviourType: behaviourType,
            bucketIndex: eventTypeId,
            initialSlotCapacity: _actors.Length);

        world.RegisterColumn<TEvent>(eventTypeId, column);
        if (behaviourType == BehaviourType.PrewarmHot && column.SupportsFastCacheBinding())
        {
            AddPrewarmHotBinder((bindWorld, fastIndex, version, slotIndex, generation) =>
            {
                bindWorld.GetOrCreateFastCache<TEvent>().Bind(
                    fastIndex,
                    version,
                    slotIndex,
                    generation,
                    column.Mails,
                    column.DirtySlots,
                    column.BucketIndex,
                    column.Options);
            });
        }

        return column;
    }

    private void BuildCallColumns(ActorTypeMeta<TActor> meta, ActorWorld world)
    {
        foreach (ActorCallEntry entry in meta.CallBehaviours)
        {
            EnsureCallRouteCapacity(entry.RouteId);
            _callColumnsByRouteId[entry.RouteId] = entry.Factory(this, entry.Invoker, world);
        }
    }

    internal ActorCallColumnRuntime BuildCallColumnDirect<TRequest, TResponse>(
        ActorWorld world,
        ActorCallInvoker<TActor, TRequest, TResponse> invoker)
        where TRequest : struct
        where TResponse : struct
    {
        int routeId = ActorCallRouteId<TRequest, TResponse>.Id;
        var column = new ActorCallColumn<TActor, TRequest, TResponse>(
            owner: this,
            invoker: invoker,
            options: world.DefaultMailOptions,
            initialSlotCapacity: _actors.Length);

        world.RegisterCallColumn<TRequest, TResponse>(routeId, column);
        return column;
    }

    private void BuildCallRoutes(ActorTypeMeta<TActor> meta)
    {
        foreach (ActorCallEntry entry in meta.CallBehaviours)
        {
            EnsureCallRouteCapacity(entry.RouteId);

            if (_callInvokersByRouteId[entry.RouteId] != null)
            {
                throw new InvalidOperationException(
                    $"Duplicate ActorCall route on actor type {typeof(TActor).Name}.");
            }

            _callInvokersByRouteId[entry.RouteId] = entry.Invoker;
            _callRequestTypesByRouteId[entry.RouteId] = entry.RequestType;
            _callResponseTypesByRouteId[entry.RouteId] = entry.ResponseType;
        }
    }

    private void EnsureCallRouteCapacity(int routeId)
    {
        if ((uint)routeId < (uint)_callInvokersByRouteId.Length)
        {
            return;
        }

        int newSize = _callInvokersByRouteId.Length == 0 ? 4 : _callInvokersByRouteId.Length;
        while (newSize <= routeId)
        {
            newSize *= 2;
        }

        Array.Resize(ref _callInvokersByRouteId, newSize);
        Array.Resize(ref _callColumnsByRouteId, newSize);
        Array.Resize(ref _callRequestTypesByRouteId, newSize);
        Array.Resize(ref _callResponseTypesByRouteId, newSize);
    }

    private void AddPrewarmHotBinder(PrewarmHotBinder binder)
    {
        if (_prewarmHotBinderCount == _prewarmHotBinders.Length)
        {
            int newSize = _prewarmHotBinders.Length == 0 ? 4 : _prewarmHotBinders.Length * 2;
            Array.Resize(ref _prewarmHotBinders, newSize);
        }

        _prewarmHotBinders[_prewarmHotBinderCount] = binder;
        _prewarmHotBinderCount++;
    }

    public override void BindPrewarmHotFastCaches(
        ActorWorld world,
        int fastIndex,
        int slotIndex,
        int generation,
        int version)
    {
        for (int i = 0; i < _prewarmHotBinderCount; i++)
        {
            _prewarmHotBinders[i](world, fastIndex, version, slotIndex, generation);
        }
    }

    public override bool TryBindHotFastCache<TEvent>(
        ActorWorld world,
        int fastIndex,
        int version,
        int slotIndex,
        int generation)
        where TEvent : struct
    {
        if (!TryGetColumn(out EventColumn<TActor, TEvent>? column)
            || column == null
            || !column.SupportsFastCacheBinding())
        {
            return false;
        }

        if ((uint)slotIndex >= (uint)_actors.Length
            || _states[slotIndex] != ActorSlotState.Alive
            || _actors[slotIndex] == null
            || _generations[slotIndex] != generation)
        {
            return false;
        }

        world.GetOrCreateFastCache<TEvent>().Bind(
            fastIndex,
            version,
            slotIndex,
            generation,
            column.Mails,
            column.DirtySlots,
            column.BucketIndex,
            column.Options);
        return true;
    }

    internal void RegisterLifecycleInterfaces(
        TActor actor,
        ActorId actorId,
        int slotIndex,
        ActorWorld world)
    {
        ActorLifecycleHandles handles = ActorLifecycleHandles.Empty;

        if (actor is IUpdate update)
        {
            handles.Update = world.Lifecycle.AddUpdate(actorId, update);
        }

        if (actor is ILateUpdate lateUpdate)
        {
            handles.LateUpdate = world.Lifecycle.AddLateUpdate(actorId, lateUpdate);
        }

        if (actor is IFixedUpdate fixedUpdate)
        {
            handles.FixedUpdate = world.Lifecycle.AddFixedUpdate(actorId, fixedUpdate);
        }

        _lifecycleHandles[slotIndex] = handles;

        if (actor is IStart start)
        {
            start.Start();
        }
    }

    public override bool MarkPendingDestroy(int slotIndex, int generation)
    {
        if (!IsAlive(slotIndex, generation))
        {
            return false;
        }

        _states[slotIndex] = ActorSlotState.PendingDestroy;
        _enabled[slotIndex] = false;
        _slotFlags[slotIndex] |= ActorSlotFlags.PendingDestroy;
        _slotFlags[slotIndex] &= ~ActorSlotFlags.Enabled;
        return true;
    }

    public override int CountAlive()
    {
        int count = 0;
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (_states[slotIndex] == ActorSlotState.Alive)
            {
                count++;
            }
        }

        return count;
    }

    public override int CountEnabled()
    {
        int count = 0;
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (_states[slotIndex] == ActorSlotState.Alive && _enabled[slotIndex])
            {
                count++;
            }
        }

        return count;
    }

    public override int CountPendingDestroy()
    {
        int count = 0;
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (_states[slotIndex] == ActorSlotState.PendingDestroy)
            {
                count++;
            }
        }

        return count;
    }

    public override bool HasAnyAlive()
    {
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (_states[slotIndex] == ActorSlotState.Alive)
            {
                return true;
            }
        }

        return false;
    }

    public override int GetTotalPendingMailCount()
    {
        int count = 0;
        foreach (ActorEventColumnRuntime? column in _columnsByEventId)
        {
            if (column != null)
            {
                count += column.GetTotalPendingCount();
            }
        }

        foreach (ActorCallColumnRuntime? column in _callColumnsByRouteId)
        {
            if (column != null)
            {
                count += column.GetTotalPendingCount();
            }
        }

        return count;
    }

    public override ActorDebugInfo GetDebugInfo(ActorId actorId, string archetypeInfo)
    {
        int slotIndex = actorId.SlotIndex;
        if ((uint)slotIndex >= (uint)_actors.Length)
        {
            return ActorDebugInfo.Invalid(actorId, "Invalid SlotIndex.");
        }

        if (_generations[slotIndex] != actorId.Generation)
        {
            return ActorDebugInfo.Invalid(actorId, "Generation mismatch.");
        }

        TActor? actor = _actors[slotIndex];
        if (actor == null)
        {
            return ActorDebugInfo.Invalid(actorId, "Actor slot is empty.");
        }

        ActorSlotState state = _states[slotIndex];
        ActorLifecycleHandles handles = _lifecycleHandles[slotIndex];

        return new ActorDebugInfo(
            actorId,
            isValid: true,
            isAlive: state == ActorSlotState.Alive,
            isEnabled: _enabled[slotIndex],
            isPendingDestroy: state == ActorSlotState.PendingDestroy,
            actorTypeName: typeof(TActor).Name,
            archetypeInfo: archetypeInfo,
            tags: GetTagNames(),
            groups: GetGroupNames(),
            pendingMailCount: GetPendingMailCount(slotIndex),
            hasUpdate: handles.Update.IsValid,
            hasLateUpdate: handles.LateUpdate.IsValid,
            hasFixedUpdate: handles.FixedUpdate.IsValid,
            failureReason: string.Empty);
    }

    public override void AppendDebugRow(StringBuilder builder, int archetypeId, string archetypeInfo)
    {
        builder.Append("| ");
        builder.Append(archetypeId);
        builder.Append(" | ");
        builder.Append(archetypeInfo);
        builder.Append(" | ");
        builder.Append(typeof(TActor).Name);
        builder.Append(" | ");
        builder.Append(CountAlive());
        builder.Append(" | ");
        builder.Append(CountEnabled());
        builder.Append(" | ");
        builder.Append(CountPendingDestroy());
        builder.Append(" | ");
        builder.Append(GetTotalPendingMailCount());
        builder.AppendLine(" |");
    }

    public override void SweepPendingDestroy(ActorWorld world)
    {
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (_states[slotIndex] != ActorSlotState.PendingDestroy)
            {
                continue;
            }

            DestroyNow(slotIndex, _generations[slotIndex], world);
        }
    }

    private int AllocateNewSlot()
    {
        int slotIndex = _nextSlotIndex;
        _nextSlotIndex++;
        EnsureActorCapacity(slotIndex + 1);
        return slotIndex;
    }

    private void EnsureActorCapacity(int required)
    {
        if (required <= _actors.Length)
        {
            return;
        }

        int oldSize = _actors.Length;
        int newSize = _actors.Length == 0 ? 4 : _actors.Length;
        while (newSize < required)
        {
            newSize *= 2;
        }

        Array.Resize(ref _actors, newSize);
        Array.Resize(ref _generations, newSize);
        Array.Resize(ref _fastIndices, newSize);
        Array.Resize(ref _states, newSize);
        Array.Resize(ref _slotFlags, newSize);
        Array.Resize(ref _enabled, newSize);
        Array.Resize(ref _createdFromPool, newSize);
        Array.Resize(ref _lifecycleHandles, newSize);
        for (int i = oldSize; i < newSize; i++)
        {
            _fastIndices[i] = -1;
            _lifecycleHandles[i] = ActorLifecycleHandles.Empty;
        }
    }

    private void EnsureColumnCapacity(int slotIndex)
    {
        foreach (ActorEventColumnRuntime? column in _columnsByEventId)
        {
            column?.EnsureSlotCapacity(slotIndex);
        }

        foreach (ActorCallColumnRuntime? column in _callColumnsByRouteId)
        {
            column?.EnsureSlotCapacity(slotIndex);
        }
    }

    private void EnsureEventColumnCapacity(int eventTypeId)
    {
        if ((uint)eventTypeId < (uint)_columnsByEventId.Length)
        {
            return;
        }

        int newSize = _columnsByEventId.Length == 0 ? 4 : _columnsByEventId.Length;
        while (newSize <= eventTypeId)
        {
            newSize *= 2;
        }

        Array.Resize(ref _columnsByEventId, newSize);
    }

    private bool DestroyNow(int slotIndex, int generation, ActorWorld world)
    {
        if ((uint)slotIndex >= (uint)_actors.Length)
        {
            return false;
        }

        if (_generations[slotIndex] != generation)
        {
            return false;
        }

        ActorSlotState state = _states[slotIndex];
        if (state == ActorSlotState.Destroying || state == ActorSlotState.Empty)
        {
            return false;
        }

        TActor? actor = _actors[slotIndex];
        if (actor == null)
        {
            return false;
        }

        _states[slotIndex] = ActorSlotState.Destroying;
        _slotFlags[slotIndex] |= ActorSlotFlags.Destroying;

        if (actor is IDestroy destroy)
        {
            destroy.Destroy();
        }

        UnregisterLifecycleInterfaces(slotIndex, world);
        ClearAllMails(slotIndex);

        bool returnToPool = _createdFromPool[slotIndex];
        int fastIndex = _fastIndices[slotIndex];

        _actors[slotIndex] = null;
        _enabled[slotIndex] = false;
        _states[slotIndex] = ActorSlotState.Empty;
        _slotFlags[slotIndex] = ActorSlotFlags.None;
        _fastIndices[slotIndex] = -1;
        _createdFromPool[slotIndex] = false;
        _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;

        unchecked
        {
            _generations[slotIndex]++;
        }

        _freeList.Push(slotIndex);
        if (fastIndex >= 0)
        {
            world.ReleaseFastIndex(fastIndex);
        }

        if (returnToPool)
        {
            ActorPoolCache<TActor>.Pool.Return(actor);
        }

        return true;
    }

    private void UnregisterLifecycleInterfaces(int slotIndex, ActorWorld world)
    {
        ActorLifecycleHandles handles = _lifecycleHandles[slotIndex];
        world.Lifecycle.RemoveUpdate(handles.Update);
        world.Lifecycle.RemoveLateUpdate(handles.LateUpdate);
        world.Lifecycle.RemoveFixedUpdate(handles.FixedUpdate);
        _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;
    }

    private void ClearAllMails(int slotIndex)
    {
        foreach (ActorEventColumnRuntime? column in _columnsByEventId)
        {
            column?.ClearMail(slotIndex);
        }

        foreach (ActorCallColumnRuntime? column in _callColumnsByRouteId)
        {
            column?.ClearMail(slotIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSlotPostable(int slotIndex)
    {
        return _states[slotIndex] == ActorSlotState.Alive
               && _actors[slotIndex] != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetColumn<TEvent>(out EventColumn<TActor, TEvent>? column)
        where TEvent : struct
    {
        int eventId = EventTypeId<TEvent>.Id;
        if ((uint)eventId >= (uint)_columnsByEventId.Length)
        {
            column = null;
            return false;
        }

        column = _columnsByEventId[eventId] as EventColumn<TActor, TEvent>;
        return column != null;
    }

    private static bool CanUsePostAllFastPath<TEvent>(
        EventColumn<TActor, TEvent>? column,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct
    {
        return postPolicy == null
               && fullPolicy == null
               && column != null
               && column.CanUseDefaultPostFastPath();
    }

    private int GetPendingMailCount(int slotIndex)
    {
        int count = 0;
        foreach (ActorEventColumnRuntime? column in _columnsByEventId)
        {
            if (column != null)
            {
                count += column.GetPendingCount(slotIndex);
            }
        }

        foreach (ActorCallColumnRuntime? column in _callColumnsByRouteId)
        {
            if (column != null)
            {
                count += column.GetPendingCount(slotIndex);
            }
        }

        return count;
    }

    private string[] GetTagNames()
    {
        return typeof(TActor)
            .GetCustomAttributes(inherit: false)
            .Where(static attribute => attribute.GetType().IsGenericType
                                       && attribute.GetType().GetGenericTypeDefinition() == typeof(TagAttribute<>))
            .Select(static attribute => attribute.GetType().GetGenericArguments()[0].Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private string[] GetGroupNames()
    {
        return typeof(TActor)
            .GetCustomAttributes(inherit: false)
            .Where(static attribute => attribute.GetType().IsGenericType
                                       && attribute.GetType().GetGenericTypeDefinition() == typeof(GroupAttribute<>))
            .Select(static attribute => attribute.GetType().GetGenericArguments()[0].Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
