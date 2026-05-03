using System.Text;
using LayerBase.Core.DataStruct;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.DI;
#if NETCOREAPP || NET5_0_OR_GREATER
using System.Numerics;
#endif

namespace LayerBase.Layers;

internal sealed class LayerChain
{
    private readonly ResponsibilityChain responsibilityChain;
    private readonly LayerRuntime _owner;
    private Layer?[] _indexedLayers = Array.Empty<Layer?>();
    private ulong _logicActiveMask;
    private ulong _hasDelayMask;
    private bool _delayDirty;

    public bool HasAnyDelay
    {
        get
        {
            if (_delayDirty) RebuildDelayMask();
            return _hasDelayMask != 0;
        }
    }

    internal void MarkDelayDirty()
    {
        _delayDirty = true;
    }

    private void RebuildDelayMask()
    {
        _hasDelayMask = 0;
        foreach (var node in responsibilityChain)
        {
            if (node is Layer layer && layer.HasDelayPublisher)
            {
                _hasDelayMask |= 1UL << layer.RouteIndex;
            }
        }
        _delayDirty = false;
    }


    internal LayerChain(ResponsibilityChain chain, LayerRuntime owner)
    {
        responsibilityChain = chain;
        _owner = owner;
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

    internal void DisposeLayers()
    {
        foreach (var node in responsibilityChain)
            if (node is Layer layer)
                layer.RunRuntimeStop();

        foreach (var node in responsibilityChain)
            if (node is Layer layer)
                layer.Dispose();
    }

    internal void Prebuild()
    {
        AssignEventBus();
        foreach (var node in responsibilityChain)
        {
            if (node is Layer layer)
            {
                layer.PrepareBuild();
                layer.BuildAutoBinding();   
            }
        }
    }
    internal void Build(int eventStateSlabSize, bool releaseMode)
    {
        var builtLayers = new List<Layer>();
        foreach (var node in responsibilityChain)
            if (node is Layer layer)
            {
                builtLayers.Add(layer);
            }

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
        {
            layer.RunPostBuild();
        }

        foreach (var layer in builtLayers)
        {
            layer.RunRuntimeStart();
        }

        EventGraphValidator.Validate(allSubscribers, _owner);
    }


    internal void Pump(float deltaTime)
    {
        var activeMask = _logicActiveMask;
        if (activeMask == 0) return;

        while (activeMask != 0)
        {
            var index = FindFirstBit(activeMask);
            if (index == -1 || index >= _indexedLayers.Length) break;

            var layer = _indexedLayers[index];
            if (layer != null)
            {
                layer.Pump(deltaTime);
            }

            activeMask &= ~(1UL << index);
        }
    }

    internal void PumpFixed(float fixedDeltaTime)
    {
        var activeMask = _logicActiveMask;
        if (activeMask == 0) return;

        while (activeMask != 0)
        {
            var index = FindFirstBit(activeMask);
            if (index == -1 || index >= _indexedLayers.Length) break;

            var layer = _indexedLayers[index];
            layer?.PumpFixed(fixedDeltaTime);

            activeMask &= ~(1UL << index);
        }
    }

    private static int FindFirstBit(ulong mask)
    {
        return BitHelper.TrailingZeroCount(mask);
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
                }

                _owner.RegisterLayerInstance(layer);

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
