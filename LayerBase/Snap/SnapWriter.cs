using System.Text.Json.Nodes;

namespace LayerBase.Snap;

public readonly struct SnapWriter
{
    private readonly JsonObject _data;
    private readonly string _path;

    internal SnapWriter(JsonObject data, string path = "$")
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _path = path;
    }

    public void WriteInt32(string key, int value) => WriteValue(key, value);

    public void WriteInt64(string key, long value) => WriteValue(key, value);

    public void WriteSingle(string key, float value) => WriteValue(key, value);

    public void WriteDouble(string key, double value) => WriteValue(key, value);

    public void WriteBoolean(string key, bool value) => WriteValue(key, value);

    public void WriteString(string key, string? value)
    {
        ValidateKey(key);
        _data[key] = value;
    }

    public void WriteEnum<TEnum>(string key, TEnum value)
        where TEnum : struct, Enum
    {
        ValidateKey(key);
        _data[key] = value.ToString();
    }

    public SnapWriter WriteObject(string key)
    {
        ValidateKey(key);

        var child = new JsonObject();
        _data[key] = child;
        return new SnapWriter(child, $"{_path}.{key}");
    }

    public SnapArrayWriter WriteArray(string key)
    {
        ValidateKey(key);

        var child = new JsonArray();
        _data[key] = child;
        return new SnapArrayWriter(child, $"{_path}.{key}");
    }

    private void WriteValue<TValue>(string key, TValue value)
    {
        ValidateKey(key);
        _data[key] = JsonValue.Create(value);
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SnapFormatException("Snap field key cannot be empty.");
        }
    }
}
