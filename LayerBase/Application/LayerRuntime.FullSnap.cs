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

    public LBTask DeserializeFullSnapAsync(
        SnapDocument document,
        FullSnapLimits limits,
        CancellationToken cancellationToken = default)
    {
        RequireOwnerThreadDebug();
        return RequireFullSnapRuntime().DeserializeAsync(document, limits, cancellationToken);
    }

    public async LBTask<string> SerializeFullSnapJsonAsync(
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = await SerializeFullSnapAsync(cancellationToken);
        return JsonSnapCodec.EncodeToString(document);
    }

    public async LBTask<byte[]> SerializeFullSnapJsonBytesAsync(
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = await SerializeFullSnapAsync(cancellationToken);
        return JsonSnapCodec.EncodeToUtf8Bytes(document);
    }

    public LBTask DeserializeFullSnapJsonAsync(
        string json,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = JsonSnapCodec.DecodeFromString(json, FullSnapLimits.Default);
        return DeserializeFullSnapAsync(document, cancellationToken);
    }

    public LBTask DeserializeFullSnapJsonAsync(
        string json,
        FullSnapLimits limits,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = JsonSnapCodec.DecodeFromString(json, limits);
        return DeserializeFullSnapAsync(document, limits, cancellationToken);
    }

    internal async LBTask<string> SerializeFullSnapJsonAsync(
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = await SerializeFullSnapAsync(cancellationToken);
        return JsonSnapCodec.EncodeToString(document, options);
    }

    internal async LBTask<byte[]> SerializeFullSnapJsonBytesAsync(
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = await SerializeFullSnapAsync(cancellationToken);
        return JsonSnapCodec.EncodeToUtf8Bytes(document, options);
    }

    internal LBTask DeserializeFullSnapJsonAsync(
        string json,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = JsonSnapCodec.DecodeFromString(json, FullSnapLimits.Default, options);
        return DeserializeFullSnapAsync(document, cancellationToken);
    }

    internal LBTask DeserializeFullSnapJsonAsync(
        string json,
        FullSnapLimits limits,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        SnapDocument document = JsonSnapCodec.DecodeFromString(json, limits, options);
        return DeserializeFullSnapAsync(document, limits, cancellationToken);
    }

    private FullSnapRuntime RequireFullSnapRuntime()
    {
        if (_fullSnap == null)
            throw new InvalidOperationException("Runtime not built.");

        return _fullSnap;
    }
}
