using System;
using System.Collections.Generic;
using System.Diagnostics;
using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Core.EventStateTrace;

/// <summary>
/// 事件状态追踪器：负责事件生命周期、计数与分类回调。
/// </summary>
internal sealed class EventStateTracer
{
    private readonly EventCounter _counter = new();
    private readonly FreeList<EventState> _eventStates;
    private readonly List<SlotRef> _completed = new();
    private readonly object _lock = new();

    public EventCompletedHandler OnEventCompleted = static (ref EventState state) => { };
    public ClassifiedEventCompletedHandler OnClassifiedEventCompleted = static (ref EventCategoryToken category, ref EventState state) => { };
    public ClassifiedEventCreatedHandler OnClassifiedEventCreated = static (ref EventCategoryToken category, ref EventState state) => { };

    public EventStateTracer(int slabSize = 512)
    {
        if (slabSize <= 0) throw new ArgumentOutOfRangeException(nameof(slabSize));
        _eventStates = new FreeList<EventState>(slabSize);
    }

    public EventStateToken Register<T>(in Event<T> @event) where T : struct
    {
        var handledState = @event.IsVaild() ? EventHandledState.Created : EventHandledState.Handled;
        var category = EventMetaDataHandler.Category<T>();

        lock (_lock)
        {
            var slotRef = _eventStates.Rent();
            ref var slot = ref _eventStates.Resolve(slotRef);
            slot.Value = new EventState
            {
                EventTypeId = EventTypeId<T>.Id,
                ForwardDir = @event.ForwardDir,
                HandledState = handledState,
                PendingCount = 1,
                StartTimestamp = Stopwatch.GetTimestamp(),
                CreatedTimestamp = Stopwatch.GetTimestamp(),
                CatalogueToken = category,
            };

            if (_counter.Increment<T>() == 1 && !category.IsEmpty)
            {
                OnClassifiedEventCreated(ref slot.Value.CatalogueToken, ref slot.Value);
            }

            return slotRef.ToToken();
        }
    }

    internal bool TryIncrementPending(in EventStateToken token, int count = 1)
    {
        if (count <= 0) return false;

        lock (_lock)
        {
            if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef))
            {
                return false;
            }

            ref var slot = ref _eventStates.Resolve(slotRef);
            slot.Value.PendingCount += count;
            return true;
        }
    }

    internal bool TryComplete(in EventStateToken token)
    {
        lock (_lock)
        {
            if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef))
            {
                return false;
            }

            ref var slot = ref _eventStates.Resolve(slotRef);
            if (slot.Completed)
            {
                return false;
            }

            slot.Value.PendingCount -= 1;
            if (slot.Value.PendingCount <= 0)
            {
                slot.Completed = true;
                FinalizeClassification(ref slot.Value);
                _completed.Add(slotRef);
            }

            return true;
        }
    }

    private void FinalizeClassification(ref EventState state)
    {
        var type = EventTypeId.GetType(state.EventTypeId);
        if (type == null)
        {
            return;
        }

        if (_counter.Decrement(type) == 0 && !state.CatalogueToken.IsEmpty)
        {
            OnClassifiedEventCompleted(ref state.CatalogueToken, ref state);
        }
    }

    public void Pump()
    {
        List<SlotRef> completedCopy;
        lock (_lock)
        {
            if (_completed.Count == 0)
            {
                return;
            }

            completedCopy = new List<SlotRef>(_completed);
            _completed.Clear();
        }

        for (int i = 0; i < completedCopy.Count; i++)
        {
            var token = completedCopy[i].ToToken();
            lock (_lock)
            {
                if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef))
                {
                    continue;
                }

                ref var slot = ref _eventStates.Resolve(slotRef);
                OnEventCompleted(ref slot.Value);
                _eventStates.Release(slotRef);
            }
        }
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
        {
            throw new InvalidOperationException("EventStateToken 无效或事件已完成。");
        }

        return ref _eventStates.Resolve(slotRef).Value;
    }

    public bool TryUpdateHandledState(in EventStateToken token, EventHandledState handledState)
    {
        lock (_lock)
        {
            if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef))
            {
                return false;
            }

            ref var slot = ref _eventStates.Resolve(slotRef);
            slot.Value.HandledState = handledState;
            return true;
        }
    }

    public bool TryUpdateForwardDir(in EventStateToken token, EventForwardDir forwardDir)
    {
        lock (_lock)
        {
            if (!_eventStates.TryBorrow(token.Index, token.Version, out var slotRef))
            {
                return false;
            }

            ref var slot = ref _eventStates.Resolve(slotRef);
            slot.Value.ForwardDir = forwardDir;
            return true;
        }
    }
}
