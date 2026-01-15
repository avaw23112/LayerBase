using System.Diagnostics;
using LayerBase.Core.Event;

namespace LayerBase.Core.EventStateTrace;

public delegate void EventCompleteHandler(
    ref EventState state // ref：按引用传参；回调里修改 state 会直接改到调用方那份变量
);

/// <summary>
/// TODO:重构项目构成
/// </summary>
internal sealed class EventStateTracer
{
    private readonly FreeList<EventState> _eventStates;
    private readonly List<SlotRef> _completed = new();
    private readonly object _lock = new();
    
    public EventCompleteHandler OnEventCompleted;

    public EventStateTracer(int slabSize = 512)
    {
        if (slabSize <= 0) throw new ArgumentOutOfRangeException(nameof(slabSize));

        _eventStates = new FreeList<EventState>(slabSize);
    }
    public EventStateToken Register<T>(in Event<T> @event) where T : struct
    {
        EventHandledState state = @event.IsVaild() ? EventHandledState.Created : EventHandledState.Handled;
        return Register(EventTypeId<T>.Id, @event.ForwardDir, state);
    }
    public EventStateToken Register(int eventTypeId)
    {
        return Register(eventTypeId, EventForwardDir.BroadCast, EventHandledState.Created);
    }
    public EventStateToken Register(int eventTypeId, EventForwardDir forwardDir, EventHandledState handledState)
    {
        lock (_lock)
        {
            var slotRef = _eventStates.Rent();
            ref var slot = ref _eventStates.Resolve(slotRef);

             slot.Value = new EventState
            {
                EventTypeId = eventTypeId,
                ForwardDir = forwardDir,
                HandledState = handledState,
                PendingCount = 1,
                StartTimestamp = Stopwatch.GetTimestamp(),
                CreatedTimestamp = Stopwatch.GetTimestamp(),
            };
             
            return slotRef.ToToken();
        }
    }

    internal bool TryIncrementPending(in EventStateToken token, int count = 1)
    {
        if (count <= 0) return false;

        lock (_lock)
        {
            if (!_eventStates.TryBorrow(token.Index,token.Version, out var slotRef))
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
            if (!_eventStates.TryBorrow(token.Index,token.Version, out var slotRef))
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
                _completed.Add(slotRef);
            }
            return true;
        }
    }

    public void Pump()
    {
        List<SlotRef> completedCopy;
        lock (_lock)
        {
            if (_completed.Count == 0) return;
            completedCopy = new List<SlotRef>(_completed);
            _completed.Clear();
        }
        for (int i = 0; i < completedCopy.Count; i++)
        {
            lock (_lock)
            {
                var eventStateToken = completedCopy[i].ToToken();
                if (!_eventStates.TryBorrow(eventStateToken.Index,eventStateToken.Version, out var slotRef))
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
        if (!_eventStates.TryBorrow(token.Index,token.Version, out var slotRef))
        {
            state = default;
            return false;
        }
        state =  _eventStates.Resolve(slotRef).Value;
        return true;
    }

    public ref EventState Resolve( in EventStateToken token)
    {
        if (!_eventStates.TryBorrow(token.Index, token.Version, out  var slotRef))
        {
            throw new InvalidOperationException("EventStateToken 无效或槽位不存在。");
        }

        return ref _eventStates.Resolve(slotRef).Value;
    }

    public bool TryUpdateHandledState(in EventStateToken token, EventHandledState handledState)
    {
        lock (_lock)
        {
            if (!_eventStates.TryBorrow(token.Index,token.Version,out var slotRef))
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
            if (!_eventStates.TryBorrow(token.Index,token.Version,out var slotRef))
            {
                return false;
            }

            ref var slot = ref _eventStates.Resolve(slotRef);
            slot.Value.ForwardDir = forwardDir;
            return true;
        }
    }
}
