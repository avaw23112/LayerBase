using System.Text;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;

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

    internal IEnumerable<Layer> GetNodes()
    {
        for (var i = 0; i < _indexedLayers.Length; i++)
            if (_indexedLayers[i] != null)
                yield return _indexedLayers[i]!;
    }

    internal void AddNode(Node node)
    {
        responsibilityChain.AddLast(node);
    }

    internal void Build(int eventStateSlabSize, bool releaseMode)
    {
        AssignEventBus();

        var builtLayers = new List<Layer>();
        foreach (var node in responsibilityChain)
            if (node is Layer layer)
            {
                layer.PrepareBuild();
                builtLayers.Add(layer);
            }

        SharedFieldBinder.Bind(
            builtLayers.SelectMany(static layer => layer.GetSharedFieldParticipants(true)));

        // 汇总逻辑活跃状态并构建各层的 DI 容器
        _logicActiveMask = 0;
        var allSubscribers = new List<IAutoSubscribe>();
        foreach (var layer in builtLayers)
        {
            layer.FinalizeBuild();
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
        var eventMask = LayerHub.EventCenter.GetEventPendingMask();

        // 2. 逻辑活跃状态
        var logicMask = _logicActiveMask;

        // 3. 合并所有需要处理的层
        var activeMask = eventMask | logicMask;
        if (activeMask == 0) return;

        // 4. 高性能位图遍历：利用硬件指令彻底跳过空闲层级
        var center = LayerHub.EventCenter;
        while (activeMask != 0)
        {
            var index = center.FindFirstBit(activeMask);
            if (index == -1 || index >= _indexedLayers.Length) break;

            var layer = _indexedLayers[index];
            if (layer != null)
            {
                var bit = 1UL << index;

                // 优化：只有当事件位图命中时，才执行事件 Pump (精准打击)
                if ((eventMask & bit) != 0) layer.PumpEvents();

                // 只有当逻辑活跃位图命中时，才执行逻辑 Pump
                if ((logicMask & bit) != 0) layer.Pump(deltaTime);
            }

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
                    var index = LayerHub.GetNextLayerIndex();
                    layer.SetRouteIndex(index);
                    LayerHub.EventCenter.EnsureSlots(index + 1, layer.GetType().Name);
                }

                LayerHub.RegisterLayerInstance(layer);

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