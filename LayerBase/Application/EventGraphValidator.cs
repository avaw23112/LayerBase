using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LayerBase.DI;

namespace LayerBase.LayerHub
{
    /// <summary>
    /// 事件环路异常：在启动期检测到同步事件分发存在死循环风险时抛出。
    /// </summary>
    public sealed class EventCycleException : Exception
    {
        public EventCycleException(string message) : base(message) { }
    }

    internal static class EventGraphValidator
    {
        private enum NodeColor { White, Gray, Black }

        /// <summary>
        /// 执行全局事件依赖审计，检测环路并识别“无人订阅”的空事件。
        /// </summary>
        public static void Validate(IEnumerable<IAutoSubscribe> subscribers)
        {
            var sentEvents = new HashSet<Type>();
            var subscribedEvents = new HashSet<Type>();
            var adj = new Dictionary<Type, HashSet<Type>>();

            foreach (var sub in subscribers)
            {
                // 1. 收集发送关系与环路图数据
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

                // 2. 收集订阅数据
                foreach (var evtType in sub.GetSubscribedEvents())
                {
                    subscribedEvents.Add(evtType);
                }
            }

            // --- 审计 A: 环路检测 (三色算法) ---
            if (adj.Count > 0)
            {
                var colors = new Dictionary<Type, NodeColor>();
                var pathStack = new List<Type>();
                foreach (var node in adj.Keys)
                {
                    if (!colors.TryGetValue(node, out var color) || color == NodeColor.White)
                        if (CheckCycle(node, adj, colors, pathStack, out var cyclePath))
                            ThrowCycleError(cyclePath!);
                }
            }

            // --- 审计 B: Dead Letter 检测 (无人订阅预警) ---
            if (LayerHub.IsDebugMode)
            {
                foreach (var sent in sentEvents)
                {
                    if (!subscribedEvents.Contains(sent))
                    {
                        LayerHub.ReportWarning(-1, "TopologyAudit", sent.Name, 
                            $"该事件被某些组件同步分发，但在当前拓扑中没有任何订阅者。这可能导致逻辑空转。");
                    }
                }
            }
        }

        private static bool CheckCycle(Type u, Dictionary<Type, HashSet<Type>> adj, Dictionary<Type, NodeColor> colors, List<Type> path, out List<Type>? cyclePath)
        {
            colors[u] = NodeColor.Gray; // 标记为正在访问
            path.Add(u);
            cyclePath = null;

            if (adj.TryGetValue(u, out var neighbors))
            {
                foreach (var v in neighbors)
                {
                    if (!colors.TryGetValue(v, out var vColor)) vColor = NodeColor.White;

                    if (vColor == NodeColor.Gray)
                    {
                        // 发现回边！从 v 开始截取环路
                        int startIndex = path.IndexOf(v);
                        cyclePath = path.Skip(startIndex).ToList();
                        cyclePath.Add(v); // 闭合环路
                        return true;
                    }

                    if (vColor == NodeColor.White)
                    {
                        if (CheckCycle(v, adj, colors, path, out cyclePath))
                        {
                            return true;
                        }
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            colors[u] = NodeColor.Black; // 标记为完全访问
            return false;
        }

        private static void ThrowCycleError(List<Type> path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("检测到同步事件分发死循环！");
            sb.AppendLine("为了保证极致性能，系统禁止在 Handler 内部同步分发可能导致回环的事件。");
            sb.Append("环路路径: ");
            for (int i = 0; i < path.Count; i++)
            {
                sb.Append(path[i].Name);
                if (i < path.Count - 1) sb.Append(" -> ");
            }
            sb.AppendLine();
            sb.AppendLine("解决方案：请将环路中任意一处 [Send] 改为 [Post] (异步分发)，以打破同步调用栈。");

            throw new EventCycleException(sb.ToString());
        }
    }
}
