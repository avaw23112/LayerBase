using System.Runtime.CompilerServices;
using LayerBase.Async;
using LayerBase.Core.EventHandler;

namespace LayerBase.Core.Event;

/// <summary>
/// 非泛型事件桶接口，用于 IL2CPP 安全的非泛型订阅路径。
/// 避免运行时 MakeGenericMethod，所有 EventBucket&lt;T&gt; 都实现此接口。
/// </summary>
internal interface IHandlerBucket
{
    void Reset();
}

internal sealed class HandlerCircuit
{
    private int _disabled;
    public bool IsDisabled => Volatile.Read(ref _disabled) == 1;

    public bool TryDisable()
    {
        return Interlocked.Exchange(ref _disabled, 1) == 0;
    }

    public void Reset()
    {
        Volatile.Write(ref _disabled, 0);
    }
}

internal sealed class HandlerBucket<T> : IHandlerBucket where T : struct
{
    private readonly Action _onDirty;
    internal List<NotifyHandlerEntry<T>> MasterNotify = new();
    internal List<OrderedHandlerEntry<T>> MasterOrdered = new();
    internal List<NotifyHandlerEntry<T>> MasterSubscribe = new();
    internal List<UnorderedHandlerEntry<T>> MasterUnordered = new();

    public HandlerBucket(Action onDirty)
    {
        _onDirty = onDirty;
    }

    public bool HasHandlers => MasterOrdered.Count > 0 || MasterUnordered.Count > 0 ||
                               MasterNotify.Count > 0 || MasterSubscribe.Count > 0;

    public void Reset()
    {
        foreach (var h in MasterOrdered) h.Circuit.Reset();
        foreach (var h in MasterUnordered) h.Circuit.Reset();
        foreach (var h in MasterNotify) h.Circuit.Reset();
        foreach (var h in MasterSubscribe) h.Circuit.Reset();
    }

    public void Add(IEventHandler<T> h)
    {
        MasterUnordered.Add(UnorderedHandlerEntry<T>.Create(h));
        _onDirty();
    }

    public void Add(IEventHandlerAsync<T> h)
    {
        MasterUnordered.Add(UnorderedHandlerEntry<T>.Create(h));
        _onDirty();
    }

    public void AddNotify(EventNotifyDelegate<T> h)
    {
        MasterNotify.Add(NotifyHandlerEntry<T>.Create(h));
        _onDirty();
    }

    public void AddSubscribe(EventNotifyDelegate<T> h)
    {
        MasterSubscribe.Add(NotifyHandlerEntry<T>.Create(h));
        _onDirty();
    }


    public void Add(EventHandleDelegate<T> h)
    {
        MasterOrdered.Add(OrderedHandlerEntry<T>.Create(h));
        _onDirty();
    }

    public void Add(EventHandleDelegateAsync<T> h)
    {
        MasterOrdered.Add(OrderedHandlerEntry<T>.Create(h));
        _onDirty();
    }


    public void Remove(IEventHandler<T> h)
    {
        RemoveAll(MasterUnordered, x => x.Source == h);
        _onDirty();
    }

    public void Remove(IEventHandlerAsync<T> h)
    {
        RemoveAll(MasterUnordered, x => x.Source == h);
        _onDirty();
    }

    public void Remove(EventHandleDelegate<T> h)
    {
        RemoveAll(MasterOrdered, x => x.SyncHandler == h);
        _onDirty();
    }

    public void Remove(EventHandleDelegateAsync<T> h)
    {
        RemoveAll(MasterOrdered, x => x.AsyncHandler == h);
        _onDirty();
    }

    public void RemoveSubscribe(EventNotifyDelegate<T> h)
    {
        RemoveAll(MasterSubscribe, x => x.Handler == h);
        _onDirty();
    }

    public void RemoveNotify(EventNotifyDelegate<T> h)
    {
        RemoveAll(MasterNotify, x => x.Handler == h);
        _onDirty();
    }

    private static void RemoveAll<TEntry>(List<TEntry> source, Predicate<TEntry> remove)
    {
        for (var i = source.Count - 1; i >= 0; i--)
        {
            if (remove(source[i]))
                source.RemoveAt(i);
        }
    }
}

internal readonly struct NotifyHandlerEntry<T> where T : struct
{
    public readonly EventNotifyDelegate<T> Handler;
    public readonly int HandlerNameId;
    public readonly HandlerCircuit Circuit;
    public readonly object? Source;

    private NotifyHandlerEntry(EventNotifyDelegate<T> h, int n, HandlerCircuit c, object? src)
    {
        Handler = h;
        HandlerNameId = n;
        Circuit = c;
        Source = src;
    }

    public static NotifyHandlerEntry<T> Create(EventNotifyDelegate<T> h)
    {
        return new NotifyHandlerEntry<T>(h, HandlerNameSymbol.FromDelegate(h), new HandlerCircuit(), null);
    }
}

internal readonly struct OrderedHandlerEntry<T> where T : struct
{
    public readonly EventHandleDelegate<T>? SyncHandler;
    public readonly EventHandleDelegateAsync<T>? AsyncHandler;
    public readonly int HandlerNameId;
    public readonly HandlerCircuit Circuit;

    private OrderedHandlerEntry(EventHandleDelegate<T>? s, EventHandleDelegateAsync<T>? a, int n, HandlerCircuit c)
    {
        SyncHandler = s;
        AsyncHandler = a;
        HandlerNameId = n;
        Circuit = c;
    }

    public static OrderedHandlerEntry<T> Create(EventHandleDelegate<T> h)
    {
        return new OrderedHandlerEntry<T>(h, null, HandlerNameSymbol.FromDelegate(h), new HandlerCircuit());
    }

    public static OrderedHandlerEntry<T> Create(EventHandleDelegateAsync<T> h)
    {
        return new OrderedHandlerEntry<T>(null, h, HandlerNameSymbol.FromDelegate(h), new HandlerCircuit());
    }
}

internal readonly struct UnorderedHandlerEntry<T> where T : struct
{
    public readonly EventHandleDelegate<T>? SyncWrapper;
    public readonly EventHandleDelegateAsync<T>? AsyncWrapper;
    public readonly int HandlerNameId;
    public readonly HandlerCircuit Circuit;
    public readonly object Source;

    private UnorderedHandlerEntry(EventHandleDelegate<T>? s, EventHandleDelegateAsync<T>? a, int n, HandlerCircuit c,
                                  object                  src)
    {
        SyncWrapper = s;
        AsyncWrapper = a;
        HandlerNameId = n;
        Circuit = c;
        Source = src;
    }

    public static UnorderedHandlerEntry<T> Create(IEventHandler<T> h)
    {
        return new UnorderedHandlerEntry<T>(
            new SyncHandlerWrapper<T>(h).Invoke,
            null,
            HandlerNameSymbol.FromInstance(h),
            new HandlerCircuit(),
            h);
    }

    public static UnorderedHandlerEntry<T> Create(IEventHandlerAsync<T> h)
    {
        return new UnorderedHandlerEntry<T>(
            null,
            new AsyncHandlerWrapper<T>(h).Invoke,
            HandlerNameSymbol.FromInstance(h),
            new HandlerCircuit(),
            h);
    }


    private sealed class SyncHandlerWrapper<TValue> where TValue : struct
    {
        private readonly IEventHandler<TValue> _handler;

        public SyncHandlerWrapper(IEventHandler<TValue> handler)
        {
            _handler = handler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventHandledState Invoke(in TValue val)
        {
            _handler.Deal(in val);
            return EventHandledState.Continue;
        }
    }

    private sealed class AsyncHandlerWrapper<TValue> where TValue : struct
    {
        private readonly IEventHandlerAsync<TValue> _handler;

        public AsyncHandlerWrapper(IEventHandlerAsync<TValue> handler)
        {
            _handler = handler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LBTask Invoke(TValue val)
        {
            return _handler.Deal(val);
        }
    }
}

