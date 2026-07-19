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

    private FullSnapRuntime RequireFullSnapRuntime()
    {
        if (_fullSnap == null)
            throw new InvalidOperationException("Runtime not built.");

        return _fullSnap;
    }
}
