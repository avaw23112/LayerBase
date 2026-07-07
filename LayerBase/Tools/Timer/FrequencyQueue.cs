using System.Buffers;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Tools.Timer;

internal interface IFrequencyQueue
{
    void ExecuteAll();
    bool Cancel(in TimerToken token);
}

internal sealed class FrequencyQueue<T> : IFrequencyQueue where T : struct
{
    private readonly Stack<int> _free = new();
    private readonly object _lock = new();
    private readonly List<FrequencyTask<T>> _tasks = new();

    public void ExecuteAll()
    {
        FrequencyTask<T>[]? snapshot = null;
        var count = 0;

        lock (_lock)
        {
            if (_tasks.Count == 0) return;
            snapshot = ArrayPool<FrequencyTask<T>>.Shared.Rent(_tasks.Count);
            for (var i = 0; i < _tasks.Count; i++)
            {
                var task = _tasks[i];
                if (task.Active) snapshot[count++] = task;
            }
        }

        try
        {
            for (var i = 0; i < count; i++) ExecuteTask(snapshot[i]);
        }
        finally
        {
            if (snapshot != null)
            {
                Array.Clear(snapshot, 0, count);
                ArrayPool<FrequencyTask<T>>.Shared.Return(snapshot);
            }
        }
    }

    public bool Cancel(in TimerToken token)
    {
        if (token.TypeId != EventTypeId<T>.Id) return false;

        lock (_lock)
        {
            if (token.Index < 0 || token.Index >= _tasks.Count) return false;

            var entry = _tasks[token.Index];
            if (!entry.Active || entry.Version != token.Version) return false;

            _tasks[token.Index] = default;
            _free.Push(token.Index);
            return true;
        }
    }

    internal TimerToken RegisterDelegate(in T value, EventHandleDelegate<T> handle)
    {
        lock (_lock)
        {
            var (index, version) = Rent();
            _tasks[index] = FrequencyTask<T>.FromDelegate(value, handle, version);
            return new TimerToken(EventTypeId<T>.Id, index, version);
        }
    }

    internal TimerToken RegisterDelegateAsync(in T value, EventHandleDelegateAsync<T> handle)
    {
        lock (_lock)
        {
            var (index, version) = Rent();
            _tasks[index] = FrequencyTask<T>.FromDelegateAsync(value, handle, version);
            return new TimerToken(EventTypeId<T>.Id, index, version);
        }
    }

    internal TimerToken RegisterHandler(in T value, IEventHandler<T> handler)
    {
        lock (_lock)
        {
            var (index, version) = Rent();
            _tasks[index] = FrequencyTask<T>.FromHandler(value, handler, version);
            return new TimerToken(EventTypeId<T>.Id, index, version);
        }
    }

    internal TimerToken RegisterHandlerAsync(in T value, IEventHandlerAsync<T> handler)
    {
        lock (_lock)
        {
            var (index, version) = Rent();
            _tasks[index] = FrequencyTask<T>.FromHandlerAsync(value, handler, version);
            return new TimerToken(EventTypeId<T>.Id, index, version);
        }
    }

    internal TimerToken RegisterEventAction(in T value, Action<Event<T>> action)
    {
        lock (_lock)
        {
            var (index, version) = Rent();
            _tasks[index] = FrequencyTask<T>.FromEventAction(value, action, version);
            return new TimerToken(EventTypeId<T>.Id, index, version);
        }
    }

    private (int index, ushort version) Rent()
    {
        var index = _free.Count > 0 ? _free.Pop() : _tasks.Count;
        if (index == _tasks.Count) _tasks.Add(default);

        var version = NextVersion(_tasks[index].Version);
        return (index, version);
    }

    private static ushort NextVersion(ushort current)
    {
        var next = (ushort)(current + 1);
        if (next == 0) next = 1;
        return next;
    }

    private static void ExecuteTask(FrequencyTask<T> task)
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
        EventMetaDataHandler.OnEventExpectation(payload, ex);
        LayerHub.ReportLayerEventError(-1, $"TimerScheduler.Frequency.{kind}", typeof(T).Name, ex);
    }
}

internal struct FrequencyTask<T> where T : struct
{
    public bool Active;
    public ushort Version;
    public T Payload;
    public TimerTaskKind Kind;
    public EventHandleDelegate<T>? HandlerDelegate;
    public EventHandleDelegateAsync<T>? HandlerDelegateAsync;
    public IEventHandler<T>? Handler;
    public IEventHandlerAsync<T>? HandlerAsync;
    public Action<Event<T>>? EventAction;

    public static FrequencyTask<T> FromDelegate(in T payload, EventHandleDelegate<T> handle, ushort version)
    {
        return new FrequencyTask<T>
        {
            Active = true, Version = version, Payload = payload,
            HandlerDelegate = handle, Kind = TimerTaskKind.EventHandlerDelegate
        };
    }

    public static FrequencyTask<T> FromDelegateAsync(in T payload, EventHandleDelegateAsync<T> handle, ushort version)
    {
        return new FrequencyTask<T>
        {
            Active = true, Version = version, Payload = payload,
            HandlerDelegateAsync = handle, Kind = TimerTaskKind.EventHandlerDelegateAsync
        };
    }

    public static FrequencyTask<T> FromHandler(in T payload, IEventHandler<T> handler, ushort version)
    {
        return new FrequencyTask<T>
        {
            Active = true, Version = version, Payload = payload,
            Handler = handler, Kind = TimerTaskKind.EventHandler
        };
    }

    public static FrequencyTask<T> FromHandlerAsync(in T payload, IEventHandlerAsync<T> handler, ushort version)
    {
        return new FrequencyTask<T>
        {
            Active = true, Version = version, Payload = payload,
            HandlerAsync = handler, Kind = TimerTaskKind.EventHandlerAsync
        };
    }

    public static FrequencyTask<T> FromEventAction(in T payload, Action<Event<T>> action, ushort version)
    {
        return new FrequencyTask<T>
        {
            Active = true, Version = version, Payload = payload,
            EventAction = action, Kind = TimerTaskKind.EventAction
        };
    }
}
