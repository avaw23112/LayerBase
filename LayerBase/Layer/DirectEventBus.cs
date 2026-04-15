using System.Threading;
using LayerBase.Core.Event;

namespace LayerBase.Layers;

internal sealed class DirectEventBus
{
    private readonly Layer[] _layers;
    private int _version;

    internal DirectEventBus(IReadOnlyList<Layer> layers)
    {
        _layers = new Layer[layers.Count];
        for (int i = 0; i < layers.Count; i++)
        {
            _layers[i] = layers[i];
        }

        _version = 1;
    }

    internal int Version => Volatile.Read(ref _version);

    internal void Invalidate()
    {
        Interlocked.Increment(ref _version);
    }

    internal EventHandledState Publish<T>(Layer source, in Event<T> @event) where T : struct
    {
        var table = RouteTableCache<T>.Get(this, _layers);
        return @event.ForwardDir switch
        {
            EventForwardDir.Bubble => DispatchRoute(table.Bubble[source.RouteIndex], table.LayerIndexes, in @event),
            EventForwardDir.Drop => DispatchRoute(table.Drop[source.RouteIndex], table.LayerIndexes, in @event),
            EventForwardDir.BroadCast => PublishBroadcast(source, table, in @event),
            _ => EventHandledState.Continue
        };
    }

    internal void PostLocal<T>(Layer source, in Event<T> @event) where T : struct
    {
        source.EnqueueEvent(in @event);
    }

    internal void PostContinuation<T>(Layer source, in Event<T> @event) where T : struct
    {
        if (!@event.ShouldForwardFromQueue)
        {
            return;
        }

        var table = RouteTableCache<T>.Get(this, _layers);
        switch (@event.ForwardDir)
        {
            case EventForwardDir.Bubble:
                EnqueueRoute(table.BroadcastUpper[source.RouteIndex], table.LayerIndexes, in @event);
                break;
            case EventForwardDir.Drop:
                EnqueueRoute(table.BroadcastLower[source.RouteIndex], table.LayerIndexes, in @event);
                break;
            case EventForwardDir.BroadCast:
                EnqueueRoute(table.BroadcastUpper[source.RouteIndex], table.LayerIndexes, in @event);
                EnqueueRoute(table.BroadcastLower[source.RouteIndex], table.LayerIndexes, in @event);
                break;
        }
    }

    private EventHandledState PublishBroadcast<T>(
        Layer source,
        RouteTable<T> table,
        in Event<T> @event) where T : struct
    {
        var sourceState = source.Dispatch(in @event);
        if (sourceState == EventHandledState.Handled)
        {
            return EventHandledState.Handled;
        }

        var upperState = DispatchRoute(table.BroadcastUpper[source.RouteIndex], table.LayerIndexes, in @event);
        var lowerState = DispatchRoute(table.BroadcastLower[source.RouteIndex], table.LayerIndexes, in @event);

        return sourceState == EventHandledState.HandledAndContinue ||
               upperState == EventHandledState.HandledAndContinue ||
               lowerState == EventHandledState.HandledAndContinue
            ? EventHandledState.HandledAndContinue
            : EventHandledState.Continue;
    }

    private EventHandledState DispatchRoute<T>(
        RouteSpan route,
        int[] layerIndexes,
        in Event<T> @event) where T : struct
    {
        bool handledAndContinueSeen = false;
        int end = route.Start + route.Length;
        for (int i = route.Start; i < end; i++)
        {
            var state = _layers[layerIndexes[i]].Dispatch(in @event);
            if (state == EventHandledState.Handled)
            {
                return EventHandledState.Handled;
            }

            if (state == EventHandledState.HandledAndContinue)
            {
                handledAndContinueSeen = true;
            }
        }

        return handledAndContinueSeen ? EventHandledState.HandledAndContinue : EventHandledState.Continue;
    }

    private void EnqueueRoute<T>(
        RouteSpan route,
        int[] layerIndexes,
        in Event<T> @event) where T : struct
    {
        if (route.Length == 0)
        {
            return;
        }

        var routedEvent = @event;
        routedEvent.DisableQueuedForwarding();
        int end = route.Start + route.Length;
        for (int i = route.Start; i < end; i++)
        {
            _layers[layerIndexes[i]].EnqueueEvent(in routedEvent);
        }
    }

    private readonly struct RouteSpan
    {
        internal readonly int Start;
        internal readonly int Length;

        internal RouteSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }
    }

    private sealed class RouteTable<T> where T : struct
    {
        internal readonly int Version;
        internal readonly RouteSpan[] Bubble;
        internal readonly RouteSpan[] Drop;
        internal readonly RouteSpan[] BroadcastUpper;
        internal readonly RouteSpan[] BroadcastLower;
        internal readonly int[] LayerIndexes;

        internal RouteTable(int version, Layer[] layers)
        {
            Version = version;
            Bubble = new RouteSpan[layers.Length];
            Drop = new RouteSpan[layers.Length];
            BroadcastUpper = new RouteSpan[layers.Length];
            BroadcastLower = new RouteSpan[layers.Length];

            var totalIndexes = CountAllRouteIndexes(layers);
            LayerIndexes = totalIndexes == 0 ? Array.Empty<int>() : new int[totalIndexes];

            int write = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                Bubble[i] = WriteBubble(layers, i, LayerIndexes, ref write);
                Drop[i] = WriteDrop(layers, i, LayerIndexes, ref write);
                BroadcastUpper[i] = WriteBroadcastUpper(layers, i, LayerIndexes, ref write);
                BroadcastLower[i] = WriteBroadcastLower(layers, i, LayerIndexes, ref write);
            }
        }

        private static int CountAllRouteIndexes(Layer[] layers)
        {
            int total = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                total += CountBubble(layers, i);
                total += CountDrop(layers, i);
                total += CountBroadcastUpper(layers, i);
                total += CountBroadcastLower(layers, i);
            }

            return total;
        }

        private static RouteSpan WriteBubble(Layer[] layers, int sourceIndex, int[] indexes, ref int write)
        {
            int start = write;
            for (int i = sourceIndex; i >= 0; i--)
            {
                if (layers[i].HasHandlers<T>())
                {
                    indexes[write++] = i;
                }
            }

            return new RouteSpan(start, write - start);
        }

        private static RouteSpan WriteDrop(Layer[] layers, int sourceIndex, int[] indexes, ref int write)
        {
            int start = write;
            for (int i = sourceIndex; i < layers.Length; i++)
            {
                if (layers[i].HasHandlers<T>())
                {
                    indexes[write++] = i;
                }
            }

            return new RouteSpan(start, write - start);
        }

        private static RouteSpan WriteBroadcastUpper(Layer[] layers, int sourceIndex, int[] indexes, ref int write)
        {
            int start = write;
            for (int i = sourceIndex - 1; i >= 0; i--)
            {
                if (layers[i].HasHandlers<T>())
                {
                    indexes[write++] = i;
                }
            }

            return new RouteSpan(start, write - start);
        }

        private static RouteSpan WriteBroadcastLower(Layer[] layers, int sourceIndex, int[] indexes, ref int write)
        {
            int start = write;
            for (int i = sourceIndex + 1; i < layers.Length; i++)
            {
                if (layers[i].HasHandlers<T>())
                {
                    indexes[write++] = i;
                }
            }

            return new RouteSpan(start, write - start);
        }

        private static int CountBubble(Layer[] layers, int sourceIndex)
        {
            int count = 0;
            for (int i = sourceIndex; i >= 0; i--)
            {
                if (layers[i].HasHandlers<T>()) count++;
            }

            return count;
        }

        private static int CountDrop(Layer[] layers, int sourceIndex)
        {
            int count = 0;
            for (int i = sourceIndex; i < layers.Length; i++)
            {
                if (layers[i].HasHandlers<T>()) count++;
            }

            return count;
        }

        private static int CountBroadcastUpper(Layer[] layers, int sourceIndex)
        {
            int count = 0;
            for (int i = sourceIndex - 1; i >= 0; i--)
            {
                if (layers[i].HasHandlers<T>()) count++;
            }

            return count;
        }

        private static int CountBroadcastLower(Layer[] layers, int sourceIndex)
        {
            int count = 0;
            for (int i = sourceIndex + 1; i < layers.Length; i++)
            {
                if (layers[i].HasHandlers<T>()) count++;
            }

            return count;
        }
    }

    private static class RouteTableCache<T> where T : struct
    {
        private static readonly object s_lock = new();
        private static DirectEventBus? s_bus;
        private static RouteTable<T>? s_table;

        internal static RouteTable<T> Get(DirectEventBus bus, Layer[] layers)
        {
            var table = Volatile.Read(ref s_table);
            var version = bus.Version;
            if (ReferenceEquals(s_bus, bus) && table != null && table.Version == version)
            {
                return table;
            }

            lock (s_lock)
            {
                table = s_table;
                version = bus.Version;
                if (ReferenceEquals(s_bus, bus) && table != null && table.Version == version)
                {
                    return table;
                }

                table = new RouteTable<T>(version, layers);
                s_bus = bus;
                Volatile.Write(ref s_table, table);
                return table;
            }
        }
    }
}
