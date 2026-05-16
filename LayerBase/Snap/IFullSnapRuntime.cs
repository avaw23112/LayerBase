using System.Text.Json;

namespace LayerBase.Snap;

public interface IFullSnapRuntime
{
    SnapDocument Serialize();

    void Deserialize(SnapDocument document);

    string SerializeJson(JsonSerializerOptions? options = null);

    void DeserializeJson(string json, JsonSerializerOptions? options = null);
}
