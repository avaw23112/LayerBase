using System.Text.Json;
using LayerBase.Async;

namespace LayerBase.Snap;

public interface IFullSnapRuntime
{
    SnapDocument Serialize();

    LBTask<SnapDocument> SerializeAsync(CancellationToken cancellationToken = default);

    void Deserialize(SnapDocument document);

    LBTask DeserializeAsync(SnapDocument document, CancellationToken cancellationToken = default);

    string SerializeJson(JsonSerializerOptions? options = null);

    LBTask<string> SerializeJsonAsync(
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default);

    void DeserializeJson(string json, JsonSerializerOptions? options = null);

    LBTask DeserializeJsonAsync(
        string json,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default);
}
