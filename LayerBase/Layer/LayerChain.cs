using System.Text;
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
                layer.Dispose();
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

        _logicActiveMask = 0;
        var allSubscribers = new List<IAutoSubscribe>();
        foreach (var layer in builtLayers)
        {
            layer.FinalizeBuild();
            allSubscribers.AddRange(layer.DiscoveredSubscribers);

            if (layer.HasActiveLogic) _logicActiveMask |= 1UL << layer.RouteIndex;
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

    private static int FindFirstBit(ulong mask)
    {
#if NETCOREAPP || NET5_0_OR_GREATER
        return BitOperations.TrailingZeroCount(mask);
#else
        if (mask == 0) return 64;
        int count = 0;
        if ((mask & 0xFFFFFFFF) == 0) { mask >>= 32; count += 32; }
        if ((mask & 0xFFFF) == 0) { mask >>= 16; count += 16; }
        if ((mask & 0xFF) == 0) { mask >>= 8; count += 8; }
        if ((mask & 0xF) == 0) { mask >>= 4; count += 4; }
        if ((mask & 0x3) == 0) { mask >>= 2; count += 2; }
        if ((mask & 0x1) == 0) { count += 1; }
        return count;
#endif
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
