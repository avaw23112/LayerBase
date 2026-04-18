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
        /// 执行全局事件依赖审计，检测是否存在 A -> B -> A 形式的死循环。
        /// 采用三色标记算法 (White-Gray-Black) 确保 100% 稳定性且防止递归挂起。
        /// </summary>
        public static void Validate(IEnumerable<IAutoSubscribe> subscribers)
        {
            // 1. 构建邻接表
            var adj = new Dictionary<Type, HashSet<Type>>();
            foreach (var sub in subscribers)
            {
                foreach (var dep in sub.GetEventDependencies())
                {
                    if (!adj.TryGetValue(dep.Source, out var targets))
                    {
                        targets = new HashSet<Type>();
                        adj[dep.Source] = targets;
                    }
                    targets.Add(dep.Target);
                }
            }

            if (adj.Count == 0) return;

            // 2. 运行三色 DFS 算法
            var colors = new Dictionary<Type, NodeColor>();
            var pathStack = new List<Type>();

            foreach (var startNode in adj.Keys)
            {
                if (!colors.TryGetValue(startNode, out var color) || color == NodeColor.White)
                {
                    if (CheckCycle(startNode, adj, colors, pathStack, out var cyclePath))
                    {
                        ThrowCycleError(cyclePath);
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
