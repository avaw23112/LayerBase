using System.Text.Json;
using LayerBase.Async;
using LayerBase.Snap;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    public LBTask<SnapDocument> SerializeFullSnapAsync(
        CancellationToken cancellationToken = default)
    {
        RequireOwnerThreadDebug();
        return RequireFullSnapRuntime().SerializeAsync(cancellationToken);
    }

    public LBTask DeserializeFullSnapAsync(
        SnapDocument document,
        CancellationToken cancellationToken = default)
    {
        RequireOwnerThreadDebug();
        return RequireFullSnapRuntime().DeserializeAsync(document, cancellationToken);
    }

    public async LBTask<string> SerializeFullSnapJsonAsync(
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = await SerializeFullSnapAsync(cancellationToken);
        return JsonSnapCodec.EncodeToString(document);
    }

    public LBTask DeserializeFullSnapJsonAsync(
        string json,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = JsonSnapCodec.DecodeFromString(json);
        return DeserializeFullSnapAsync(document, cancellationToken);
    }

    internal async LBTask<string> SerializeFullSnapJsonAsync(
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = await SerializeFullSnapAsync(cancellationToken);
        return JsonSnapCodec.EncodeToString(document, options);
    }

    internal LBTask DeserializeFullSnapJsonAsync(
        string json,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = JsonSnapCodec.DecodeFromString(json, options);
        return DeserializeFullSnapAsync(document, cancellationToken);
    }

    private FullSnapRuntime RequireFullSnapRuntime()
    {
        if (_fullSnap == null)
            throw new InvalidOperationException("Runtime not built.");

        return _fullSnap;
    }
}
