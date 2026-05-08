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
    private ActorSlotFreeList _freeList;
    private int _nextSlotIndex;

    public ushort TypeStorageIndex { get; }
    public TActor?[] Actors => _actors;

    public TypedActorStorage(ushort typeStorageIndex, int maxEventTypeId, int initialCapacity)
    {
        TypeStorageIndex = typeStorageIndex;
        _columnsByEventId = new ActorEventColumnRuntime[Math.Max(maxEventTypeId + 1, 1)];
        _actors = new TActor?[Math.Max(initialCapacity, 1)];
        _generations = new int[_actors.Length];
        _freeList = new ActorSlotFreeList(_actors.Length);
        _nextSlotIndex = 0;
    }

    public int AllocateSlot(TActor actor)
    {
        int slotIndex = _freeList.TryPop(out int freeSlot)
            ? freeSlot
            : AllocateNewSlot();

        _actors[slotIndex] = actor;
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
               && _actors[slotIndex] != null
               && _generations[slotIndex] == generation;
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

    public void BuildColumns(ActorTypeMeta<TActor> meta, ActorWorld world)
    {
        foreach (ActorBehaviourEntry entry in meta.Behaviours)
        {
            BuildColumnFromEntry(entry, world);
        }
    }

    private void BuildColumnFromEntry(ActorBehaviourEntry entry, ActorWorld world)
    {
        MethodInfo method = s_buildColumnMethod.MakeGenericMethod(entry.EventType);
        method.Invoke(this, new object?[] { entry.Invoker, world, entry.EventTypeId });
    }

    private void BuildColumnCore<TEvent>(object invokerObject, ActorWorld world, int eventTypeId)
        where TEvent : struct
    {
        var invoker = (ActorBehaviourInvoker<TActor, TEvent>)invokerObject;
        var column = new EventColumn<TActor, TEvent>(
            owner: this,
            invoker: invoker,
            options: ActorMailOptions.Default,
            initialSlotCapacity: _actors.Length);

        EnsureEventColumnCapacity(eventTypeId);
        _columnsByEventId[eventTypeId] = column;
        world.RegisterColumn(eventTypeId, column);
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

        int newSize = _actors.Length == 0 ? 4 : _actors.Length;
        while (newSize < required)
        {
            newSize *= 2;
        }

        Array.Resize(ref _actors, newSize);
        Array.Resize(ref _generations, newSize);
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
}
