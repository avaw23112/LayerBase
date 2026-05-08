using System.Reflection;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

internal sealed class TypedActorStorage<TActor> : TypedStorageRuntime
    where TActor : class, IActor
{
    private static readonly MethodInfo s_buildColumnMethod = typeof(TypedActorStorage<TActor>)
        .GetMethod(nameof(BuildColumnCore), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private ActorEventColumnRuntime[] _columnsByEventId;
    private TActor?[] _actors;
    private int[] _generations;
    private ActorSlotState[] _states;
    private bool[] _enabled;
    private ActorLifecycleHandles[] _lifecycleHandles;
    private ActorSlotFreeList _freeList;
    private int _nextSlotIndex;

    public ushort TypeStorageIndex { get; }
    public TActor?[] Actors => _actors;

    public TypedActorStorage(ushort typeStorageIndex, int maxEventTypeId, int initialCapacity)
    {
        TypeStorageIndex = typeStorageIndex;
        _columnsByEventId = new ActorEventColumnRuntime[Math.Max(maxEventTypeId + 1, 1)];
        int capacity = Math.Max(initialCapacity, 1);
        _actors = new TActor?[capacity];
        _generations = new int[_actors.Length];
        _states = new ActorSlotState[_actors.Length];
        _enabled = new bool[_actors.Length];
        _lifecycleHandles = new ActorLifecycleHandles[_actors.Length];
        for (int i = 0; i < _lifecycleHandles.Length; i++)
        {
            _lifecycleHandles[i] = ActorLifecycleHandles.Empty;
        }

        _freeList = new ActorSlotFreeList(_actors.Length);
        _nextSlotIndex = 0;
    }

    public int AllocateSlot(TActor actor)
    {
        int slotIndex = _freeList.TryPop(out int freeSlot)
            ? freeSlot
            : AllocateNewSlot();

        _actors[slotIndex] = actor;
        _states[slotIndex] = ActorSlotState.Alive;
        _enabled[slotIndex] = true;
        _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;
        EnsureColumnCapacity(slotIndex);
        return slotIndex;
    }

    public int GetGeneration(int slotIndex)
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

    internal bool IsAliveSlot(int slotIndex)
    {
        return (uint)slotIndex < (uint)_actors.Length
               && _states[slotIndex] == ActorSlotState.Alive
               && _actors[slotIndex] != null;
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
        return true;
    }

    public override PostResult Post<TEvent>(
        int slotIndex,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct
    {
        int eventId = EventTypeId<TEvent>.Id;
        if ((uint)eventId >= (uint)_columnsByEventId.Length)
        {
            return PostResult.Failure("Invalid event type id.");
        }

        ActorEventColumnRuntime? runtime = _columnsByEventId[eventId];
        if (runtime == null)
        {
            return PostResult.Failure(
                $"Actor type {typeof(TActor).Name} does not support event {typeof(TEvent).Name}.");
        }

        var column = (EventColumn<TActor, TEvent>)runtime;
        return column.Post(slotIndex, in value, postPolicy, fullPolicy);
    }

    public override void PostToAliveActors<TEvent>(
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
    {
        int eventId = EventTypeId<TEvent>.Id;
        if ((uint)eventId >= (uint)_columnsByEventId.Length)
        {
            return;
        }

        if (_columnsByEventId[eventId] is not EventColumn<TActor, TEvent> column)
        {
            return;
        }

        int maxSlot = Math.Min(_nextSlotIndex, _actors.Length);
        for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
        {
            if (_states[slotIndex] != ActorSlotState.Alive)
            {
                continue;
            }

            if (_actors[slotIndex] == null)
            {
                continue;
            }

            _ = column.Post(slotIndex, in value, postPolicy, fullPolicy);
        }
    }

    public override IEnumerable<IActor> EnumerateActors()
    {
        int maxSlot = Math.Min(_nextSlotIndex, _actors.Length);
        for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
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
        foreach (ActorBehaviourEntry entry in meta.Behaviours)
        {
            BuildColumnFromEntry(entry, world, world.ResolveMailOptions(entry.EventTypeId));
        }
    }

    private void BuildColumnFromEntry(ActorBehaviourEntry entry, ActorWorld world, ActorMailOptions options)
    {
        MethodInfo method = s_buildColumnMethod.MakeGenericMethod(entry.EventType);
        method.Invoke(this, new object?[] { entry.Invoker, world, entry.EventTypeId, options });
    }

    private void BuildColumnCore<TEvent>(object invokerObject, ActorWorld world, int eventTypeId, ActorMailOptions options)
        where TEvent : struct
    {
        var invoker = (ActorBehaviourInvoker<TActor, TEvent>)invokerObject;
        var column = new EventColumn<TActor, TEvent>(
            owner: this,
            invoker: invoker,
            options: options,
            initialSlotCapacity: _actors.Length);

        EnsureEventColumnCapacity(eventTypeId);
        _columnsByEventId[eventTypeId] = column;
        world.RegisterColumn(eventTypeId, column);
    }

    internal void RegisterLifecycleInterfaces(
        TActor actor,
        ActorId actorId,
        int slotIndex,
        ActorWorld world)
    {
        ActorLifecycleHandles handles = ActorLifecycleHandles.Empty;

        if (actor is IStart start)
        {
            handles.Start = world.Lifecycle.AddStart(actorId, start);
        }

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
    }

    public override bool MarkPendingDestroy(int slotIndex, int generation)
    {
        if (!IsAlive(slotIndex, generation))
        {
            return false;
        }

        _states[slotIndex] = ActorSlotState.PendingDestroy;
        _enabled[slotIndex] = false;
        return true;
    }

    public override void SweepPendingDestroy(ActorWorld world)
    {
        int maxSlot = Math.Min(_nextSlotIndex, _actors.Length);
        for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
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
        Array.Resize(ref _states, newSize);
        Array.Resize(ref _enabled, newSize);
        Array.Resize(ref _lifecycleHandles, newSize);
        for (int i = oldSize; i < newSize; i++)
        {
            _lifecycleHandles[i] = ActorLifecycleHandles.Empty;
        }
    }

    private void EnsureColumnCapacity(int slotIndex)
    {
        foreach (ActorEventColumnRuntime? column in _columnsByEventId)
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

        TActor? actor = _actors[slotIndex];
        if (actor == null)
        {
            return false;
        }

        if (actor is IDestroy destroy)
        {
            destroy.Destroy();
        }

        UnregisterLifecycleInterfaces(slotIndex, world);
        ClearAllMails(slotIndex);

        _actors[slotIndex] = null;
        _enabled[slotIndex] = false;
        _states[slotIndex] = ActorSlotState.Empty;
        _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;

        unchecked
        {
            _generations[slotIndex]++;
        }

        _freeList.Push(slotIndex);
        return true;
    }

    private void UnregisterLifecycleInterfaces(int slotIndex, ActorWorld world)
    {
        ActorLifecycleHandles handles = _lifecycleHandles[slotIndex];
        world.Lifecycle.RemoveStart(handles.Start);
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
    }
}
