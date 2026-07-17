using System.Buffers;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core.EventStateTrace;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Tools.Timer;

internal interface ITimerQueue
{
    bool TryInvoke(in TimerToken token);
    bool Cancel(in    TimerToken token);
}

internal sealed class TimerQueue<T> : ITimerQueue where T : struct
{
    private readonly object _lock = new();
    private readonly FreeList<TimerTask<T>> _tasks = new(128);

    public bool TryInvoke(in TimerToken token)
    {
        if (token.TypeId != EventTypeId<T>.Id) return false;

        SlotRef slotRef;
        TimerTask<T> task;

        lock (_lock)
        {
            if (!_tasks.TryBorrow(token.Index, token.Version, out slotRef)) return false;

            ref var slot = ref _tasks.Resolve(slotRef);
            task = slot.Value;
            slot.Value = default;
        }

        try
        {
            if (task.Kind != TimerTaskKind.None) ExecuteTask(ref task);
            return true;
        }
        catch (Exception ex)
        {
            LayerHub.ReportLayerEventError(-1, "TimerQueue.TryInvoke", typeof(T).Name, ex);
            return true;
        }
        finally
        {
            lock (_lock)
            {
                _tasks.Release(slotRef);
            }
        }
    }

    public bool Cancel(in TimerToken token)
    {
        if (token.TypeId != EventTypeId<T>.Id) return false;

        lock (_lock)
        {
            if (!_tasks.TryBorrow(token.Index, token.Version, out var slotRef)) return false;

            ref var slot = ref _tasks.Resolve(slotRef);
            slot.Value = default;

            _tasks.Release(slotRef);
            return true;
        }
    }

    internal TimerToken ScheduleDelegate(double executeAt, in T value, EventHandleDelegate<T> handle)
    {
        lock (_lock)
        {
            var slotRef = _tasks.Rent();
            ref var slot = ref _tasks.Resolve(slotRef);
            slot.Value = TimerTask<T>.FromDelegate(executeAt, value, handle);
            return new TimerToken(EventTypeId<T>.Id, slotRef.GlobalIndex, slotRef.Version);
        }
    }

    internal TimerToken ScheduleDelegateAsync(double executeAt, in T value, EventHandleDelegateAsync<T> handle)
    {
        lock (_lock)
        {
            var slotRef = _tasks.Rent();
            ref var slot = ref _tasks.Resolve(slotRef);
            slot.Value = TimerTask<T>.FromDelegateAsync(executeAt, value, handle);
            return new TimerToken(EventTypeId<T>.Id, slotRef.GlobalIndex, slotRef.Version);
        }
    }

    internal TimerToken ScheduleHandler(double executeAt, in T value, IEventHandler<T> handler)
    {
        lock (_lock)
        {
            var slotRef = _tasks.Rent();
            ref var slot = ref _tasks.Resolve(slotRef);
            slot.Value = TimerTask<T>.FromHandler(executeAt, value, handler);
            return new TimerToken(EventTypeId<T>.Id, slotRef.GlobalIndex, slotRef.Version);
        }
    }

    internal TimerToken ScheduleHandlerAsync(double executeAt, in T value, IEventHandlerAsync<T> handler)
    {
        lock (_lock)
        {
            var slotRef = _tasks.Rent();
            ref var slot = ref _tasks.Resolve(slotRef);
            slot.Value = TimerTask<T>.FromHandlerAsync(executeAt, value, handler);
            return new TimerToken(EventTypeId<T>.Id, slotRef.GlobalIndex, slotRef.Version);
        }
    }

    internal TimerToken ScheduleEventAction(double executeAt, in T value, Action<Event<T>> action)
    {
        lock (_lock)
        {
            var slotRef = _tasks.Rent();
            ref var slot = ref _tasks.Resolve(slotRef);
            slot.Value = TimerTask<T>.FromEventAction(executeAt, value, action);
            return new TimerToken(EventTypeId<T>.Id, slotRef.GlobalIndex, slotRef.Version);
        }
    }

    private static void ExecuteTask(ref TimerTask<T> task)
    {
        try
        {
            switch (task.Kind)
            {
                case TimerTaskKind.EventHandlerDelegate:
                    task.HandlerDelegate!.Invoke(in task.Payload);
                    break;
                case TimerTaskKind.EventHandlerDelegateAsync:
                {
                    var payload = task.Payload;
                    var kind = task.Kind;
                    task.HandlerDelegateAsync!.Invoke(payload)
                        .Forget(ex => ReportTaskException(kind, in payload, ex));
                    break;
                }
                case TimerTaskKind.EventHandler:
                    task.Handler!.Deal(in task.Payload);
                    break;
                case TimerTaskKind.EventHandlerAsync:
                {
                    var payload = task.Payload;
                    var kind = task.Kind;
                    task.HandlerAsync!.Deal(payload)
                        .Forget(ex => ReportTaskException(kind, in payload, ex));
                    break;
                }
                case TimerTaskKind.EventAction:
                    task.EventAction!.Invoke(new Event<T>(task.Payload));
                    break;
            }
        }
        catch (Exception ex)
        {
            ReportTaskException(task.Kind, in task.Payload, ex);
        }
    }

    private static void ReportTaskException(TimerTaskKind kind, in T payload, Exception ex)
    {
        var metaData = EventMetaDataHandler.ResolveRegisteredMetaData<T>();
        metaData?.OnEventExpectation(payload, ex);
        LayerHub.ReportLayerEventError(-1, $"TimerScheduler.{kind}", typeof(T).Name, ex);
    }
}

internal enum TimerTaskKind
{
    None = 0,
    EventHandlerDelegate,
    EventHandlerDelegateAsync,
    EventHandler,
    EventHandlerAsync,
    EventAction
}

internal struct TimerTask<T> where T : struct
{
    public double ExecuteAt;
    public T Payload;
    public TimerTaskKind Kind;
    public EventHandleDelegate<T>? HandlerDelegate;
    public EventHandleDelegateAsync<T>? HandlerDelegateAsync;
    public IEventHandler<T>? Handler;
    public IEventHandlerAsync<T>? HandlerAsync;
    public Action<Event<T>>? EventAction;

    public static TimerTask<T> FromDelegate(double executeAt, in T payload, EventHandleDelegate<T> handle)
    {
        return new TimerTask<T>
        {
            ExecuteAt = executeAt,
            Payload = payload,
            HandlerDelegate = handle,
            Kind = TimerTaskKind.EventHandlerDelegate
        };
    }

    public static TimerTask<T> FromDelegateAsync(double executeAt, in T payload, EventHandleDelegateAsync<T> handle)
    {
        return new TimerTask<T>
        {
            ExecuteAt = executeAt,
            Payload = payload,
            HandlerDelegateAsync = handle,
            Kind = TimerTaskKind.EventHandlerDelegateAsync
        };
    }

    public static TimerTask<T> FromHandler(double executeAt, in T payload, IEventHandler<T> handler)
    {
        return new TimerTask<T>
        {
            ExecuteAt = executeAt,
            Payload = payload,
            Handler = handler,
            Kind = TimerTaskKind.EventHandler
        };
    }

    public static TimerTask<T> FromHandlerAsync(double executeAt, in T payload, IEventHandlerAsync<T> handler)
    {
        return new TimerTask<T>
        {
            ExecuteAt = executeAt,
            Payload = payload,
            HandlerAsync = handler,
            Kind = TimerTaskKind.EventHandlerAsync
        };
    }

    public static TimerTask<T> FromEventAction(double executeAt, in T payload, Action<Event<T>> action)
    {
        return new TimerTask<T>
        {
            ExecuteAt = executeAt,
            Payload = payload,
            EventAction = action,
            Kind = TimerTaskKind.EventAction
        };
    }
}
