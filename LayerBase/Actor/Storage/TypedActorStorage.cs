using System.Reflection;
using System.Linq;
using System.Text;
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
    private bool[] _createdFromPool;
    private ActorLifecycleHandles[] _lifecycleHandles;
    private ActorSlotFreeList _freeList;
    private int _nextSlotIndex;
    private readonly int _archetypeId;
    private ActorTypeMeta<TActor>? _meta;

    public ushort TypeStorageIndex { get; }
    public override string ActorTypeName => typeof(TActor).Name;
    public TActor?[] Actors => _actors;
    public ActorSlotState[] States => _states;
    public bool[] Enabled => _enabled;
    public int MaxSlot => Math.Min(_nextSlotIndex, _actors.Length);

    public TypedActorStorage(ushort typeStorageIndex, int archetypeId, int maxEventTypeId, int initialCapacity)
    {
        TypeStorageIndex = typeStorageIndex;
        _archetypeId = archetypeId;
        _columnsByEventId = new ActorEventColumnRuntime[Math.Max(maxEventTypeId + 1, 1)];
        int capacity = Math.Max(initialCapacity, 1);
        _actors = new TActor?[capacity];
        _generations = new int[_actors.Length];
        _states = new ActorSlotState[_actors.Length];
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
        int slotIndex = _freeList.TryPop(out int freeSlot)
            ? freeSlot
            : AllocateNewSlot();

        _actors[slotIndex] = actor;
        _states[slotIndex] = ActorSlotState.Alive;
        _enabled[slotIndex] = true;
        _createdFromPool[slotIndex] = createdFromPool;
        _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;
        EnsureColumnCapacity(slotIndex);
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

    internal bool IsSlotEnabled(int slotIndex)
    {
        return (uint)slotIndex < (uint)_enabled.Length
               && _enabled[slotIndex];
    }

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
        return true;
    }

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

    public override void PostToAliveActors<TEvent>(
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
    {
        if (!TryGetColumn(out EventColumn<TActor, TEvent>? column))
        {
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

        foreach (ActorBehaviourEntry entry in meta.Behaviours)
        {
            BuildColumnFromEntry(entry, world, world.ResolveMailOptions(entry.EventTypeId));
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
        Array.Resize(ref _states, newSize);
        Array.Resize(ref _enabled, newSize);
        Array.Resize(ref _createdFromPool, newSize);
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

        if (actor is IDestroy destroy)
        {
            destroy.Destroy();
        }

        UnregisterLifecycleInterfaces(slotIndex, world);
        ClearAllMails(slotIndex);

        bool returnToPool = _createdFromPool[slotIndex];

        _actors[slotIndex] = null;
        _enabled[slotIndex] = false;
        _states[slotIndex] = ActorSlotState.Empty;
        _createdFromPool[slotIndex] = false;
        _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;

        unchecked
        {
            _generations[slotIndex]++;
        }

        _freeList.Push(slotIndex);

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
    }

    private bool IsSlotPostable(int slotIndex)
    {
        return _states[slotIndex] == ActorSlotState.Alive
               && _actors[slotIndex] != null;
    }

    private bool TryGetColumn<TEvent>(out EventColumn<TActor, TEvent>? column)
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
