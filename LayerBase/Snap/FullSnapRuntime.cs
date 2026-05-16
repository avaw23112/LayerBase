using System.Text.Json;
using System.Text.Json.Nodes;

namespace LayerBase.Snap;

internal sealed class FullSnapRuntime : IFullSnapRuntime
{
    private readonly List<IGeneratedFullSnapNode> _nodes = new();

    public FullSnapRuntime(LayerRuntime runtime)
    {
        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }
    }

    internal void Register(IGeneratedFullSnapNode node)
    {
        _nodes.Add(node ?? throw new ArgumentNullException(nameof(node)));
    }

    public SnapDocument Serialize()
    {
        var document = new SnapDocument();

        for (int i = 0; i < _nodes.Count; i++)
        {
            IGeneratedFullSnapNode node = _nodes[i];
            var data = new JsonObject();
            var writer = new SnapWriter(data);

            node.WriteFullSnap(ref writer);

            document.AddSection(new SnapSection
            {
                Key = node.__SnapKey,
                Version = node.__SnapVersion,
                Data = data
            });
        }

        return document;
    }

    public void Deserialize(SnapDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        for (int i = 0; i < _nodes.Count; i++)
        {
            IGeneratedFullSnapNode node = _nodes[i];

            if (!document.TryGetSection(node.__SnapKey, out SnapSection? section) || section == null)
            {
                continue;
            }

            if (section.Data == null)
            {
                throw new SnapFormatException($"Snap section '{node.__SnapKey}' has null data.");
            }

            var reader = new SnapReader(section.Data, section.Version);
            node.ReadFullSnap(ref reader);
        }
    }

    public string SerializeJson(JsonSerializerOptions? options = null)
    {
        return JsonSnapCodec.EncodeToString(Serialize(), options);
    }

    public void DeserializeJson(string json, JsonSerializerOptions? options = null)
    {
        Deserialize(JsonSnapCodec.DecodeFromString(json, options));
    }
}
