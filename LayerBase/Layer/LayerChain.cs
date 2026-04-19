using System.Linq;
using System.Text;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
using LayerBase.LayerHub;

namespace LayerBase.Layers;

internal sealed class LayerChain
{
    private readonly ResponsibilityChain responsibilityChain;
    private Layer?[] _indexedLayers = Array.Empty<Layer?>();
    private ulong _logicActiveMask;

    internal LayerChain(ResponsibilityChain chain)
    {
        responsibilityChain = chain;
    }

    internal void AddNode(Node node)
    {
        responsibilityChain.AddLast(node);
    }

    internal void Build(int eventStateSlabSize, bool releaseMode)
    {
        AssignEventBus();

        // 汇总逻辑活跃状态并构建各层的 DI 容器
        _logicActiveMask = 0;
        var allSubscribers = new List<IAutoSubscribe>();
        foreach (var node in responsibilityChain)
            if (node is Layer layer)
            {
                layer.Build();
                allSubscribers.AddRange(layer.DiscoveredSubscribers);

                if (layer.HasActiveLogic) _logicActiveMask |= 1UL << layer.RouteIndex;
            }

        // 核心步骤：启动期全量环路审计
        EventGraphValidator.Validate(allSubscribers);
    }

    internal void SetLogTracing(Action<string>? logger, int logQueueCapacity)
    {
    }

    internal void Pump(float deltaTime)
    {
        // 1. 获取全局事件挂起状态（位图）
        var eventMask = LayerHub.LayerHub.EventCenter.GetEventPendingMask();

        // 2. 合并逻辑活跃状态
        var activeMask = eventMask | _logicActiveMask;
        if (activeMask == 0) return;

        // 3. 高性能位图遍历：利用硬件指令彻底跳过空闲层级
        var center = LayerHub.LayerHub.EventCenter;
        while (activeMask != 0)
        {
            var index = center.FindFirstBit(activeMask);
            if (index == -1 || index >= _indexedLayers.Length) break;

            var layer = _indexedLayers[index];
            layer?.Pump(deltaTime);

            activeMask &= ~(1UL << index);
        }
    }

    internal void PrintLog()
    {
    }

    private void AssignEventBus()
    {
        var maxIndex = -1;
        foreach (var node in responsibilityChain)
            if (node is Layer layer)
            {
                if (layer.RouteIndex == -1)
                {
                    var index = LayerHub.LayerHub.GetNextLayerIndex();
                    layer.SetRouteIndex(index);
                    LayerHub.LayerHub.EventCenter.EnsureSlots(index + 1, layer.GetType().Name);
                }

                if (layer.RouteIndex > maxIndex) maxIndex = layer.RouteIndex;
            }

        // 建立快速索引数组，确保 O(1) 获取层级实例
        if (maxIndex != -1)
        {
            _indexedLayers = new Layer?[maxIndex + 1];
            foreach (var node in responsibilityChain)
                if (node is Layer layer)
                    _indexedLayers[layer.RouteIndex] = layer;
        }
    }

    internal string GetTopologySummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("LayerBase 拓扑结构审计报告：");
        sb.AppendLine("------------------------------------------------");
        for (var i = 0; i < _indexedLayers.Length; i++)
        {
            var layer = _indexedLayers[i];
            if (layer == null) continue;

            sb.AppendLine($"Layer {i}: {layer.GetType().Name} [{(layer.HasActiveLogic ? "Active" : "Passive")}]");
            foreach (var sub in layer.DiscoveredSubscribers)
            {
                sb.AppendLine($"  -> [M] {sub.GetType().Name}");
                var subs = sub.GetSubscribedEvents().ToList();
                if (subs.Count > 0)
                    sb.AppendLine($"       |-- 订阅: {string.Join(", ", subs.Select(t => t.Name))}");
                
                var deps = sub.GetEventDependencies().ToList();
                if (deps.Count > 0)
                    sb.AppendLine($"       |-- 派发: {string.Join(", ", deps.Select(d => d.Target.Name))}");
            }
        }

        sb.AppendLine("------------------------------------------------");
        return sb.ToString();
    }
}