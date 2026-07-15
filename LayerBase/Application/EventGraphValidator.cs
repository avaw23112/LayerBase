using System.Text;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase;

public sealed class EventCycleException : Exception
{
    public EventCycleException(string message) : base(message)
    {
    }
}

internal static class EventGraphValidator
{
    public static void Validate(IEnumerable<Layer> layers, LayerRuntime runtime)
    {
        var subscribersByScope = new SortedDictionary<int, List<IAutoSubscribe>>();
        foreach (var layer in layers.OrderBy(static layer => layer.RouteIndex))
        foreach (var subscriber in layer.DiscoveredSubscribers)
        {
            int ownerScopeId = ResolveSubscriberScopeId(subscriber, layer);
            if (!subscribersByScope.TryGetValue(ownerScopeId, out var subscribers))
            {
                subscribers = new List<IAutoSubscribe>();
                subscribersByScope[ownerScopeId] = subscribers;
            }

            subscribers.Add(subscriber);
        }

        foreach (var item in subscribersByScope)
            Validate(item.Value, runtime, item.Key);
    }

    public static void Validate(IEnumerable<IAutoSubscribe> subscribers, LayerRuntime runtime)
    {
        Validate(subscribers, runtime, ScopeDefinitionIds.Main);
    }

    private static void Validate(IEnumerable<IAutoSubscribe> subscribers, LayerRuntime runtime, int scopeId)
    {
        var sentEvents = new HashSet<Type>();
        var subscribedEvents = new HashSet<Type>();
        var adj = new Dictionary<Type, HashSet<Type>>();

        foreach (var sub in subscribers)
        {
            foreach (var dep in sub.GetEventDependencies())
            {
                sentEvents.Add(dep.Target);
                if (!adj.TryGetValue(dep.Source, out var targets))
                {
                    targets = new HashSet<Type>();
                    adj[dep.Source] = targets;
                }

                targets.Add(dep.Target);
            }

            foreach (var evtType in sub.GetSubscribedEvents()) subscribedEvents.Add(evtType);
        }

        if (adj.Count > 0)
        {
            var colors = new Dictionary<Type, NodeColor>();
            var pathStack = new List<Type>();
            foreach (var node in adj.Keys)
                if (!colors.TryGetValue(node, out var color) || color == NodeColor.White)
                    if (CheckCycle(node, adj, colors, pathStack, out var cyclePath))
                        ThrowCycleError(cyclePath!, scopeId);
        }

        if (runtime.IsDebugMode)
            foreach (var sent in sentEvents)
                if (!subscribedEvents.Contains(sent))
                    runtime.ReportWarning(-1, "TopologyAudit", sent.Name,
                        "This event is dispatched synchronously but has no subscribers in current topology.");
    }

    private static int ResolveSubscriberScopeId(IAutoSubscribe subscriber, Layer layer)
    {
        var binding = ServiceLayerBinder.GetBinding(subscriber);
        if (binding != null)
            return binding.OwnerScope.Descriptor.ScopeId;

        var layerBinding = ServiceLayerBinder.GetBinding(layer);
        if (layerBinding != null)
            return layerBinding.OwnerScope.Descriptor.ScopeId;

        return ScopeDefinitionIds.Main;
    }

    private static bool CheckCycle(Type u, Dictionary<Type, HashSet<Type>> adj, Dictionary<Type, NodeColor> colors,
                                   List<Type> path, out List<Type>? cyclePath)
    {
        colors[u] = NodeColor.Gray;
        path.Add(u);
        cyclePath = null;

        if (adj.TryGetValue(u, out var neighbors))
            foreach (var v in neighbors)
            {
                if (!colors.TryGetValue(v, out var vColor)) vColor = NodeColor.White;

                if (vColor == NodeColor.Gray)
                {
                    var startIndex = path.IndexOf(v);
                    cyclePath = path.Skip(startIndex).ToList();
                    cyclePath.Add(v);
                    return true;
                }

                if (vColor == NodeColor.White)
                    if (CheckCycle(v, adj, colors, path, out cyclePath))
                        return true;
            }

        path.RemoveAt(path.Count - 1);
        colors[u] = NodeColor.Black;
        return false;
    }

    private static void ThrowCycleError(List<Type> path, int scopeId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Synchronous event cycle detected!");
        sb.AppendLine($"Scope {scopeId}");
        sb.AppendLine("To ensure performance, synchronous dispatch of events that may cause a loop is prohibited.");
        sb.Append("Cycle path: ");
        for (var i = 0; i < path.Count; i++)
        {
            sb.Append(path[i].Name);
            if (i < path.Count - 1) sb.Append(" -> ");
        }

        sb.AppendLine();
        sb.AppendLine(
            "Solution: Change any [Send] in the loop to [Post] (asynchronous dispatch) to break the synchronous call stack.");

        throw new EventCycleException(sb.ToString());
    }

    private enum NodeColor
    {
        White,
        Gray,
        Black
    }
}
