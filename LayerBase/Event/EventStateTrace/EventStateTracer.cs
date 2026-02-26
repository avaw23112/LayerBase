using System.Diagnostics;
using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Core.EventStateTrace;

internal sealed class EventStateTracer
{
    private List<SlotRef> _completed = new();
    private List<SlotRef> _drainBuffer = new();
    private readonly EventCounter _counter = new();
    private readonly FreeList<EventState> _eventStates;

    public ClassifiedEventCompletedHandler OnClassifiedEventCompleted =
        static (ref EventCategoryToken category, ref EventState state) => { };

    public ClassifiedEventCreatedHandler OnClassifiedEventCreated =
        static (ref EventCategoryToken category, ref EventState state) => { };

    public EventCompletedHandler OnEventCompleted = static (ref EventState state) => { };

    public EventStateTracer(int slabSize = 512)
    {
        if (slabSize <= 0) throw new ArgumentOutOfRangeException(nameof(slabSize));
        _eventStates = new FreeList<EventState>(slabSize);
    }

    public EventStateToken Register<T>(in Event<T> @event) where T : struct
    {
        var handledState = @event.IsVaild() ? EventHandledState.Created : EventHandledState.Handled;
        var category = EventMetaDataHandler.Category<T>();
        var now = Stopwatch.GetTimestamp();

        var slotRef = _eventStates.Rent();
        ref var slot = ref _eventStates.Resolve(slotRef);
        slot.Value = new EventState
        {
            EventTypeId = EventTypeId<T>.Id,
            ForwardDir = @event.ForwardDir,
            HandledState = handledState,
            PendingCount = 1,
            StartTimestamp = now,
            CreatedTimestamp = now,
            CatalogueToken = category
        };

        if (_counter.Increment(category) == 1 && !category.IsEmpty)
        {
            OnClassifiedEventCreated(ref slot.Value.CatalogueToken, ref slot.Value);
        }

        return slotRef.ToToken();
    }

    internal bool TryIncrementPending(in EventStateToken token, int count = 1)
    {
        if (count <= 0) return false;

        if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef)) return false;

        ref var slot = ref _eventStates.Resolve(slotRef);
        slot.Value.PendingCount += count;
        return true;
    }

    internal bool TryComplete(in EventStateToken token)
    {
        if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef)) return false;

        ref var slot = ref _eventStates.Resolve(slotRef);
        if (slot.Completed) return false;

        slot.Value.PendingCount -= 1;
        if (slot.Value.PendingCount <= 0)
        {
            slot.Completed = true;
            FinalizeClassification(ref slot.Value);
            _completed.Add(slotRef);
        }

        return true;
    }

    private void FinalizeClassification(ref EventState state)
    {
        if (_counter.Decrement(state.CatalogueToken) == 0 && !state.CatalogueToken.IsEmpty)
        {
            OnClassifiedEventCompleted(ref state.CatalogueToken, ref state);
        }
    }

    public void Pump()
    {
        if (_completed.Count == 0) return;

        var drain = _drainBuffer;
        _drainBuffer = _completed;
        _completed = drain;

        for (var i = 0; i < _drainBuffer.Count; i++)
        {
            var token = _drainBuffer[i].ToToken();

            if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef)) continue;

            ref var slot = ref _eventStates.Resolve(slotRef);
            OnEventCompleted(ref slot.Value);
            _eventStates.Release(slotRef);
        }

        _drainBuffer.Clear();
    }

    public bool TryGet(in EventStateToken token, out EventState state)
    {
        if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef))
        {
            state = default;
            return false;
        }

        state = _eventStates.Resolve(slotRef).Value;
        return true;
    }

    public ref EventState Resolve(in EventStateToken token)
    {
        if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef))
            throw new InvalidOperationException("Invalid EventStateToken or event already completed.");

        return ref _eventStates.Resolve(slotRef).Value;
    }

    public bool TryUpdateHandledState(in EventStateToken token, EventHandledState handledState)
    {
        if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef)) return false;

        ref var slot = ref _eventStates.Resolve(slotRef);
        slot.Value.HandledState = handledState;
        return true;
    }

    public bool TryUpdateForwardDir(in EventStateToken token, EventForwardDir forwardDir)
    {
        if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef)) return false;

        ref var slot = ref _eventStates.Resolve(slotRef);
        slot.Value.ForwardDir = forwardDir;
        return true;
    }
}

