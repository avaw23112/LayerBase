using System.Text;
using LayerBase.Core.DataStruct;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;

namespace LayerBase.Layers;

/// <summary>
/// 管理 Layer 链的内部实现。负责 Layer 的索引分配、生命周期构建和逐帧推进。
/// 使用位掩码（ulong）加速活跃 Layer 的遍历，最多支持 64 个 Layer。
/// </summary>
internal sealed class LayerChain
{
    private readonly ResponsibilityChain _responsibilityChain;
    private readonly LayerRuntime _owner;
    private Layer?[] _indexedLayers = Array.Empty<Layer?>();
    private ulong _logicActiveMask;
    private ulong _hasDelayMask;
    private bool _delayDirty;

    /// <summary>
    /// 是否有任何 Layer 包含活跃的延迟发布器。
    /// </summary>
    public bool HasAnyDelay
    {
        get
        {
            if (_delayDirty) RebuildDelayMask();
            return _hasDelayMask != 0;
        }
    }

    /// <summary>
    /// 标记延迟掩码需要重建。
    /// </summary>
    internal void MarkDelayDirty()
    {
        _delayDirty = true;
    }

    /// <summary>
    /// 重新计算包含延迟发布器的 Layer 位掩码。
    /// </summary>
    private void RebuildDelayMask()
    {
        _hasDelayMask = 0;
        foreach (var node in _responsibilityChain)
        {
            if (node is Layer layer && layer.HasDelayPublisher)
                _hasDelayMask |= 1UL << layer.RouteIndex;
        }
        _delayDirty = false;
    }

    internal LayerChain(ResponsibilityChain chain, LayerRuntime owner)
    {
        _responsibilityChain = chain;
        _owner = owner;
    }

    /// <summary>
    /// 遍历所有已索引的 Layer。
    /// </summary>
    internal IEnumerable<Layer> GetNodes()
    {
        for (var i = 0; i < _indexedLayers.Length; i++)
            if (_indexedLayers[i] != null)
                yield return _indexedLayers[i]!;
    }

    /// <summary>
    /// 向责任链末尾添加一个节点。
    /// </summary>
    internal void AddNode(Node node)
    {
        _responsibilityChain.AddLast(node);
    }

    /// <summary>
    /// 依次执行所有 Layer 的 RuntimeStop 和 Dispose。
    /// </summary>
    internal void DisposeLayers()
    {
        foreach (var node in _responsibilityChain)
            if (node is Layer layer)
                layer.RunRuntimeStop();

        foreach (var node in _responsibilityChain)
            if (node is Layer layer)
                layer.Dispose();
    }

    /// <summary>
    /// 预构建阶段：分配事件总线索引、准备构建和自动绑定。
    /// </summary>
    internal void Prebuild()
    {
        AssignEventBus();
        foreach (var node in _responsibilityChain)
        {
            if (node is Layer layer)
            {
                layer.PrepareBuild();
            }
        }
    }

    internal void BuildAutoBindings()
    {
        foreach (var node in _responsibilityChain)
        {
            if (node is Layer layer)
            {
                layer.BuildAutoBinding();
            }
        }
    }

    /// <summary>
    /// 完整构建阶段：绑定共享字段、执行生命周期构建、PostBuild 和 RuntimeStart。
    /// </summary>
    internal void Build(int eventStateSlabSize, bool releaseMode)
    {
        var builtLayers = new List<Layer>();
        foreach (var node in _responsibilityChain)
            if (node is Layer layer)
                builtLayers.Add(layer);

        SharedFieldBinder.Bind(
            builtLayers.SelectMany(static layer => layer.GetSharedFieldParticipants(true)));

        _logicActiveMask = 0;
        _hasDelayMask = 0;
        var allSubscribers = new List<IAutoSubscribe>();
        foreach (var layer in builtLayers)
        {
            layer.LifecycleBuild();
            allSubscribers.AddRange(layer.DiscoveredSubscribers);
            if (layer.HasActiveLogic) _logicActiveMask |= 1UL << layer.RouteIndex;
            if (layer.HasDelayPublisher) _hasDelayMask |= 1UL << layer.RouteIndex;
        }

        foreach (var layer in builtLayers)
            layer.RunPostBuild();

        foreach (var layer in builtLayers)
            layer.RunRuntimeStart();

        EventGraphValidator.Validate(allSubscribers, _owner);
    }

    /// <summary>
    /// 逐帧推进所有活跃 Layer 的 Update。
    /// </summary>
    internal void Pump(float deltaTime)
    {
        var activeMask = _logicActiveMask;
        if (activeMask == 0) return;

        while (activeMask != 0)
        {
            var index = BitHelper.TrailingZeroCount(activeMask);
            if (index == -1 || index >= _indexedLayers.Length) break;

            var layer = _indexedLayers[index];
            layer?.Pump(deltaTime);
            activeMask &= ~(1UL << index);
        }
    }

    /// <summary>
    /// 以固定时间步长推进所有活跃 Layer 的 FixedUpdate。
    /// </summary>
    internal void PumpFixed(float fixedDeltaTime)
    {
        var activeMask = _logicActiveMask;
        if (activeMask == 0) return;

        while (activeMask != 0)
        {
            var index = BitHelper.TrailingZeroCount(activeMask);
            if (index == -1 || index >= _indexedLayers.Length) break;

            var layer = _indexedLayers[index];
            layer?.PumpFixed(fixedDeltaTime);
            activeMask &= ~(1UL << index);
        }
    }

    /// <summary>
    /// 为每个 Layer 分配路由索引并建立索引数组。
    /// </summary>
    private void AssignEventBus()
    {
        var maxIndex = -1;
        foreach (var node in _responsibilityChain)
            if (node is Layer layer)
            {
                if (layer.RouteIndex == -1)
                {
                    var index = _owner.GetNextLayerIndex();
                    layer.SetRouteIndex(index);
                }
                _owner.RegisterLayerInstance(layer);
                if (layer.RouteIndex > maxIndex) maxIndex = layer.RouteIndex;
            }

        if (maxIndex != -1)
        {
            _indexedLayers = new Layer?[maxIndex + 1];
            foreach (var node in _responsibilityChain)
                if (node is Layer layer)
                    _indexedLayers[layer.RouteIndex] = layer;
        }
    }

    /// <summary>
    /// 生成拓扑审计摘要文本。
    /// </summary>
    internal string GetTopologySummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("LayerBase Topology Audit Report");
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
                    sb.AppendLine($"       |-- Subscribed: {string.Join(", ", subs.Select(t => t.Name))}");

                var deps = sub.GetEventDependencies().ToList();
                if (deps.Count > 0)
                    sb.AppendLine($"       |-- Dispatched: {string.Join(", ", deps.Select(d => d.Target.Name))}");
            }
        }
        sb.AppendLine("------------------------------------------------");
        return sb.ToString();
    }
}
