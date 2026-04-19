using System.Text;
using LayerBase.DI;

namespace LayerBase;

/// <summary>
///     事件环路异常：在启动期检测到同步事件分发存在死循环风险时抛出。
/// </summary>
public sealed class EventCycleException : Exception
{
    public EventCycleException(string message) : base(message)
    {
    }
}

internal static class EventGraphValidator
{
    /// <summary>
    ///     执行全局事件依赖审计，检测环路并识别“无人订阅”的空事件。
    /// </summary>
    public static void Validate(IEnumerable<IAutoSubscribe> subscribers, LayerRuntime runtime)
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
                        ThrowCycleError(cyclePath!);
        }

        // 🚀 使用传入的 runtime 进行报错，不再依赖静态 LayerHub
        if (runtime.IsDebugMode)
            foreach (var sent in sentEvents)
                if (!subscribedEvents.Contains(sent))
                    runtime.ReportWarning(-1, "TopologyAudit", sent.Name,
                        "该事件被某些组件同步分发，但在当前拓扑中没有任何订阅者。这可能导致逻辑空转。");
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

    private static void ThrowCycleError(List<Type> path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("检测到同步事件分发死循环！");
        sb.AppendLine("环路路径: ");
        for (var i = 0; i < path.Count; i++)
        {
            sb.Append(path[i].Name);
            if (i < path.Count - 1) sb.Append(" -> ");
        }
        sb.AppendLine();
        throw new EventCycleException(sb.ToString());
    }

    private enum NodeColor { White, Gray, Black }
}