using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using LayerBase.Core.Event;
using LayerBase.Async;

namespace LayerBase.Actor;

internal sealed class TypedActorStorage<TActor> : TypedStorageRuntime
    where TActor : class, IActor
{
    private ActorEventColumnRuntime[] _columnsByEventId;
    private TActor?[] _actors;
    private int[] _generations;
    private ActorSlotState[] _states;
    private ActorSlotFlags[] _slotFlags;
    private ActorStructuralDirtyFlags[] _structuralDirtyFlags;
    private int[] _alivePostGenerations;
    private int[] _enabledPostGenerations;
    private bool[] _enabled;
    private bool[] _createdFromPool;
    private ActorLifecycleHandles[] _lifecycleHandles;
    private ActorSlotFreeList _freeList;
    private int _nextSlotIndex;
    private readonly int _archetypeId;
    private ActorTypeMeta<TActor>? _meta;
    private object?[] _callInvokersByRouteId = Array.Empty<object?>();
    private ActorCallColumnRuntime?[] _callColumnsByRouteId = Array.Empty<ActorCallColumnRuntime?>();
    private Type?[] _callRequestTypesByRouteId = Array.Empty<Type?>();
    private Type?[] _callResponseTypesByRouteId = Array.Empty<Type?>();

    internal int ArchetypeId => _archetypeId;
    public override string ActorTypeName => typeof(TActor).Name;
    public TActor?[] Actors => _actors;
    internal int[] Generations => _generations;
    internal ActorSlotFlags[] SlotFlags => _slotFlags;
    internal int[] AlivePostGenerations => _alivePostGenerations;
    internal int[] EnabledPostGenerations => _enabledPostGenerations;
    public ActorSlotState[] States => _states;
    public bool[] Enabled => _enabled;
    public int MaxSlot => Math.Min(_nextSlotIndex, _actors.Length);

    public TypedActorStorage(int archetypeId, int maxEventTypeId, int initialCapacity)
    {
        _archetypeId = archetypeId;
        _columnsByEventId = new ActorEventColumnRuntime[Math.Max(maxEventTypeId + 1, 1)];
        int capacity = Math.Max(initialCapacity, 1);
        _actors = new TActor?[capacity];
        _generations = new int[_actors.Length];
        _states = new ActorSlotState[_actors.Length];
        _slotFlags = new ActorSlotFlags[_actors.Length];
        _structuralDirtyFlags = new ActorStructuralDirtyFlags[_actors.Length];
        _alivePostGenerations = new int[_actors.Length];
        _enabledPostGenerations = new int[_actors.Length];
        _enabled = new bool[_actors.Length];
        _createdFromPool = new bool[_actors.Length];
        _lifecycleHandles = new ActorLifecycleHandles[_actors.Length];
        for (int i = 0; i < _lifecycleHandles.Length; i++)
        {
            _lifecycleHandles[i] = ActorLifecycleHandles.Empty;
        }

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
        bool reusedSlot = _freeList.TryPop(out int freeSlot);
        int slotIndex = reusedSlot
            ? freeSlot
            : AllocateNewSlot();

        _actors[slotIndex] = actor;
        _states[slotIndex] = ActorSlotState.Alive;
        _enabled[slotIndex] = true;
        _slotFlags[slotIndex] = ActorSlotFlags.Alive | ActorSlotFlags.Enabled;
        _structuralDirtyFlags[slotIndex] = reusedSlot
            ? ActorStructuralDirtyFlags.SlotRecycle
            : ActorStructuralDirtyFlags.None;
        _createdFromPool[slotIndex] = createdFromPool;
        _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;
        EnsureColumnCapacity(slotIndex);
        RefreshPostGenerations(slotIndex);
        return slotIndex;
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

    public override void PostAll<TEvent>(
        ActorWorld world,
        EventPostState<TEvent> state,
        byte routeCode,
        in TEvent value)
        where TEvent : struct
    {
        EventPostRow<TEvent>[] rows = state.RowsByArchetype;
        if ((uint)_archetypeId >= (uint)rows.Length)
        {
            return;
        }

        EventPostRow<TEvent> row = rows[_archetypeId];
        if (!row.IsValid)
        {
            return;
        }

        byte validation = (byte)(routeCode & ActorPostRouteCode.ValidationMask);
        byte writeMode = (byte)(routeCode & ActorPostRouteCode.WriteModeMask);

        switch (writeMode)
        {
            case ActorPostRouteCode.WriteQueuedGrow:
                PostAllQueuedGrow(world, row, state, validation, in value);
                break;

            case ActorPostRouteCode.WriteQueuedRejectNew:
                PostAllQueuedRejectNew(world, row, state, validation, in value);
                break;

            case ActorPostRouteCode.WriteQueuedDropOldest:
                PostAllQueuedDropOldest(world, row, state, validation, in value);
                break;

            case ActorPostRouteCode.WriteLatest:
                PostAllLatest(world, row, state, validation, in value);
                break;

            case ActorPostRouteCode.WriteDirty:
                PostAllDirty(world, row, state, validation, in value);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CanPumpSlot(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_slotFlags.Length)
        {
            return false;
        }

        ActorSlotFlags flags = _slotFlags[slotIndex];
        return (flags & ActorSlotFlags.Alive) != 0
               && (flags & ActorSlotFlags.PendingDestroy) == 0
               && (flags & ActorSlotFlags.Destroying) == 0
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

        if (_enabled[slotIndex] == enable)
        {
            return true;
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
        _structuralDirtyFlags[slotIndex] |= ActorStructuralDirtyFlags.EnableChanged;
        RefreshPostGenerations(slotIndex);
        
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
            _columnsByEventId[entry.EventTypeId] = entry.Factory(this, entry.Invoker, world);
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
        ActorBehaviourInvoker<TActor, TEvent> invoker)
        where TEvent : struct
    {
        ActorEventPostPlan<TEvent> plan = ActorEventPostPlanBuilder.Build<TEvent>(world.DefaultMailOptions);
        EventPostState<TEvent> state = world.GetOrCreateEventPostState(plan);
        var column = new EventColumn<TActor, TEvent>(
            world: world,
            owner: this,
            invoker: invoker,
            mailPool: state.Pool,
            options: plan.MailOptions,
            bucketIndex: plan.EventId,
            initialSlotCapacity: _actors.Length,
            plan: plan);

        world.RegisterColumn<TEvent>(plan.EventId, column);
        column.RefreshPostRowBinding();

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
        _structuralDirtyFlags[slotIndex] |= ActorStructuralDirtyFlags.PendingDestroy;
        RefreshPostGenerations(slotIndex);
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
            if ((_structuralDirtyFlags[slotIndex] & ActorStructuralDirtyFlags.PendingDestroy) == 0)
            {
                continue;
            }

            ClearAllMails(slotIndex);
            FinalizeDestroySlot(slotIndex, world);
            RefreshPostGenerations(slotIndex);
            _structuralDirtyFlags[slotIndex] = ActorStructuralDirtyFlags.None;
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
        Array.Resize(ref _states, newSize);
        Array.Resize(ref _slotFlags, newSize);
        Array.Resize(ref _structuralDirtyFlags, newSize);
        Array.Resize(ref _alivePostGenerations, newSize);
        Array.Resize(ref _enabledPostGenerations, newSize);
        Array.Resize(ref _enabled, newSize);
        Array.Resize(ref _createdFromPool, newSize);
        Array.Resize(ref _lifecycleHandles, newSize);
        for (int i = oldSize; i < newSize; i++)
        {
            _lifecycleHandles[i] = ActorLifecycleHandles.Empty;
        }

        foreach (ActorEventColumnRuntime? column in _columnsByEventId)
        {
            column?.RefreshPostRowBinding();
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

    private void FinalizeDestroySlot(int slotIndex, ActorWorld world)
    {
        ActorSlotState state = _states[slotIndex];
        if (state == ActorSlotState.Destroying || state == ActorSlotState.Empty)
        {
            return;
        }

        TActor? actor = _actors[slotIndex];
        if (actor == null)
        {
            return;
        }

        _states[slotIndex] = ActorSlotState.Destroying;
        _slotFlags[slotIndex] |= ActorSlotFlags.Destroying;
        RefreshPostGenerations(slotIndex);

        if (actor is IDestroy destroy)
        {
            destroy.Destroy();
        }

        UnregisterLifecycleInterfaces(slotIndex, world);

        bool returnToPool = _createdFromPool[slotIndex];

        _actors[slotIndex] = null;
        _enabled[slotIndex] = false;
        _states[slotIndex] = ActorSlotState.Empty;
        _slotFlags[slotIndex] = ActorSlotFlags.None;
        _createdFromPool[slotIndex] = false;
        _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;
        _structuralDirtyFlags[slotIndex] |= ActorStructuralDirtyFlags.SlotRecycle;

        unchecked
        {
            _generations[slotIndex]++;
        }

        _freeList.Push(slotIndex);
        RefreshPostGenerations(slotIndex);

        if (returnToPool)
        {
            ActorPoolCache<TActor>.Pool.Return(actor);
        }
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

    internal void RefreshPostGenerations(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_slotFlags.Length)
        {
            return;
        }

        ActorSlotFlags flags = _slotFlags[slotIndex];
        int generation = _generations[slotIndex];

        bool alivePostable =
            (flags & ActorSlotFlags.Alive) != 0 &&
            (flags & ActorSlotFlags.PendingDestroy) == 0 &&
            (flags & ActorSlotFlags.Destroying) == 0 &&
            _actors[slotIndex] != null;

        _alivePostGenerations[slotIndex] = alivePostable
            ? generation
            : -1;

        bool enabledPostable =
            alivePostable &&
            (flags & ActorSlotFlags.Enabled) != 0;

        _enabledPostGenerations[slotIndex] = enabledPostable
            ? generation
            : -1;
    }

    private void PostAllQueuedGrow<TEvent>(
        ActorWorld world,
        EventPostRow<TEvent> row,
        EventPostState<TEvent> state,
        byte validation,
        in TEvent value)
        where TEvent : struct
    {
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!CanPostAllSlot(row, validation, slotIndex))
            {
                continue;
            }

            _ = world.PostQueuedGrowCore(
                slotIndex,
                in value,
                row.Mails,
                row.DirtySlots,
                row.BucketIndex,
                state.Pool,
                state.Options);
        }
    }

    private void PostAllQueuedRejectNew<TEvent>(
        ActorWorld world,
        EventPostRow<TEvent> row,
        EventPostState<TEvent> state,
        byte validation,
        in TEvent value)
        where TEvent : struct
    {
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!CanPostAllSlot(row, validation, slotIndex))
            {
                continue;
            }

            _ = world.PostQueuedRejectNewCore(
                slotIndex,
                in value,
                row.Mails,
                row.DirtySlots,
                row.BucketIndex,
                state.Pool,
                state.Options);
        }
    }

    private void PostAllQueuedDropOldest<TEvent>(
        ActorWorld world,
        EventPostRow<TEvent> row,
        EventPostState<TEvent> state,
        byte validation,
        in TEvent value)
        where TEvent : struct
    {
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!CanPostAllSlot(row, validation, slotIndex))
            {
                continue;
            }

            _ = world.PostQueuedDropOldestCore(
                slotIndex,
                in value,
                row.Mails,
                row.DirtySlots,
                row.BucketIndex,
                state.Pool,
                state.Options);
        }
    }

    private void PostAllLatest<TEvent>(
        ActorWorld world,
        EventPostRow<TEvent> row,
        EventPostState<TEvent> state,
        byte validation,
        in TEvent value)
        where TEvent : struct
    {
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!CanPostAllSlot(row, validation, slotIndex))
            {
                continue;
            }

            _ = world.PostLatestCore(
                slotIndex,
                in value,
                row.Mails,
                row.DirtySlots,
                row.BucketIndex,
                state.Pool);
        }
    }

    private void PostAllDirty<TEvent>(
        ActorWorld world,
        EventPostRow<TEvent> row,
        EventPostState<TEvent> state,
        byte validation,
        in TEvent value)
        where TEvent : struct
    {
        for (int slotIndex = 0; slotIndex < MaxSlot; slotIndex++)
        {
            if (!CanPostAllSlot(row, validation, slotIndex))
            {
                continue;
            }

            _ = world.PostDirtyCore(
                slotIndex,
                in value,
                row.Mails,
                row.DirtySlots,
                row.BucketIndex,
                state.Pool);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanPostAllSlot<TEvent>(
        EventPostRow<TEvent> row,
        byte validation,
        int slotIndex)
        where TEvent : struct
    {
        if (_states[slotIndex] != ActorSlotState.Alive
            || _actors[slotIndex] == null)
        {
            return false;
        }

        if (validation != ActorPostRouteCode.ValidationPostableStamp)
        {
            return true;
        }

        int[]? postableGenerations = row.PostableGenerations;
        return postableGenerations != null
               && postableGenerations[slotIndex] == _generations[slotIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
}

