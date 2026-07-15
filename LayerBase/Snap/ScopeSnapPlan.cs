using System.Text.Json.Nodes;

namespace LayerBase.Snap;

internal readonly struct ScopeSnapNodePlan
{
    public ScopeSnapNodePlan(
        int layerIndex,
        int objectSlot,
        IGeneratedFullSnapNode node)
    {
        LayerIndex = layerIndex;
        ObjectSlot = objectSlot;
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Key = node.__SnapKey;
        Version = node.__SnapVersion;
    }

    public int LayerIndex { get; }

    public int ObjectSlot { get; }

    public string Key { get; }

    public int Version { get; }

    public IGeneratedFullSnapNode Node { get; }
}

internal sealed class ScopeSnapPlan
{
    public static ScopeSnapPlan Empty { get; } = new(Array.Empty<ScopeSnapNodePlan>());

    public ScopeSnapPlan(ScopeSnapNodePlan[] nodes)
    {
        Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        Array.Sort(Nodes, Compare);
    }

    public ScopeSnapNodePlan[] Nodes { get; }

    private static int Compare(ScopeSnapNodePlan left, ScopeSnapNodePlan right)
    {
        int layer = left.LayerIndex.CompareTo(right.LayerIndex);
        if (layer != 0) return layer;

        int slot = left.ObjectSlot.CompareTo(right.ObjectSlot);
        if (slot != 0) return slot;

        return string.CompareOrdinal(left.Key, right.Key);
    }
}

internal static class ScopeSnapExecutor
{
    public static SnapSection[] Write(ScopeSnapPlan plan)
    {
        if (plan == null)
            throw new ArgumentNullException(nameof(plan));

        var sections = new SnapSection[plan.Nodes.Length];
        for (int i = 0; i < plan.Nodes.Length; i++)
        {
            ScopeSnapNodePlan nodePlan = plan.Nodes[i];
            var data = new JsonObject();
            var writer = new SnapWriter(data);

            nodePlan.Node.WriteFullSnap(ref writer);

            sections[i] = new SnapSection
            {
                Key = nodePlan.Key,
                Version = nodePlan.Version,
                Data = data
            };
        }

        return sections;
    }

    public static void Read(ScopeSnapPlan plan, SnapDocument document)
    {
        if (plan == null)
            throw new ArgumentNullException(nameof(plan));
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        for (int i = 0; i < plan.Nodes.Length; i++)
        {
            ScopeSnapNodePlan nodePlan = plan.Nodes[i];

            if (!document.TryGetSection(nodePlan.Key, out SnapSection? section) || section == null)
                continue;

            if (section.Data == null)
                throw new SnapFormatException($"Snap section '{nodePlan.Key}' has null data.");

            var reader = new SnapReader(section.Data, section.Version);
            nodePlan.Node.ReadFullSnap(ref reader);
        }
    }
}
