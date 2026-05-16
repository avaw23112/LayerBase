using System.Text.Json.Nodes;

namespace LayerBase.Snap;

public sealed class SnapSection
{
    public string Key { get; init; } = string.Empty;

    public int Version { get; init; } = 1;

    public JsonObject Data { get; init; } = new();
}
