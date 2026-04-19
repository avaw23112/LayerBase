using System.Text;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;

namespace LayerBase.Layers;

internal sealed class LayerChain
{
    private readonly LayerRuntime _owner;
    private readonly ResponsibilityChain responsibilityChain;
    private Layer?[] _indexedLayers = Array.Empty<Layer?>();
    private ulong _logicActiveMask;

    internal LayerChain(ResponsibilityChain chain, LayerRuntime owner)
    {
        responsibilityChain = chain;
        _owner = owner;
    }

    internal void AddNode(Node node)
    {
        responsibilityChain.AddLast(node);
    }

    internal void Build(int eventStateSlabSize, bool releaseMode)
    {
        AssignEventBus();

        _logicActiveMask = 0;
        var allSubscribers = new List<IAutoSubscribe>();
        foreach (var node in responsibilityChain)
            if (node is Layer layer)
            {
                layer.Build();
                allSubscribers.AddRange(layer.DiscoveredSubscribers);

                if (layer.HasActiveLogic) _logicActiveMask |= 1UL << layer.RouteIndex;
            }

        // 环路审计需要 runtime 的状态
        EventGraphValidator.Validate(allSubscribers, _owner);
    }

    internal void Pump(float deltaTime)
    {
        var center = _owner.EventCenter;
        var eventMask = center.GetEventPendingMask();

        var activeMask = eventMask | _logicActiveMask;
        if (activeMask == 0) return;

        while (activeMask != 0)
        {
            var index = center.FindFirstBit(activeMask);
            if (index == -1 || index >= _indexedLayers.Length) break;

            var layer = _indexedLayers[index];
            layer?.Pump(deltaTime);

            activeMask &= ~(1UL << index);
        }
    }

    private void AssignEventBus()
    {
        var maxIndex = -1;
        foreach (var node in responsibilityChain)
            if (node is Layer layer)
            {
                if (layer.RouteIndex == -1)
                {
                    var index = _owner.GetNextLayerIndex();
                    layer.SetRouteIndex(index);
                    _owner.EventCenter.EnsureSlots(index + 1, layer.GetType().Name);
                }

                if (layer.RouteIndex > maxIndex) maxIndex = layer.RouteIndex;
            }

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