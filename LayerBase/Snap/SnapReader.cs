using System.Text.Json.Nodes;

namespace LayerBase.Snap;

public readonly struct SnapReader
{
    private readonly JsonObject _data;
    private readonly SnapDecodeLimits _limits;
    private readonly string _path;

    public int Version { get; }

    internal SnapReader(
        JsonObject data,
        int version,
        string path = "$",
        SnapDecodeLimits? limits = null)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _limits = limits ?? SnapDecodeLimits.Default;
        Version = version;
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public int ReadInt32(string key) => ReadRequiredValue<int>(key);

    public long ReadInt64(string key) => ReadRequiredValue<long>(key);

    public float ReadSingle(string key) => ReadRequiredValue<float>(key);

    public double ReadDouble(string key) => ReadRequiredValue<double>(key);

    public bool ReadBoolean(string key) => ReadRequiredValue<bool>(key);

    public string ReadString(string key)
    {
        string? value = ReadRequiredValue<string?>(key);

        if (value == null)
        {
            throw new SnapFormatException($"Field '{BuildPath(key)}' cannot be null.");
        }

        ValidateStringLength(value, BuildPath(key));
        return value;
    }

    public bool TryReadInt32(string key, out int value) => TryReadValue(key, out value);

    public bool TryReadInt64(string key, out long value) => TryReadValue(key, out value);

    public bool TryReadSingle(string key, out float value) => TryReadValue(key, out value);

    public bool TryReadDouble(string key, out double value) => TryReadValue(key, out value);

    public bool TryReadBoolean(string key, out bool value) => TryReadValue(key, out value);

    public bool TryReadString(string key, out string value)
    {
        if (!TryReadValue<string?>(key, out string? result) || result == null)
        {
            value = string.Empty;
            return false;
        }

        value = result;
        return true;
    }

    public int ReadInt32OrDefault(string key, int defaultValue = default)
    {
        return TryReadInt32(key, out int value) ? value : defaultValue;
    }

    public float ReadSingleOrDefault(string key, float defaultValue = default)
    {
        return TryReadSingle(key, out float value) ? value : defaultValue;
    }

    public TEnum ReadEnum<TEnum>(string key)
        where TEnum : struct, Enum
    {
        string text = ReadString(key);

        if (Enum.TryParse(text, out TEnum value))
        {
            return value;
        }

        throw new SnapFormatException(
            $"Field '{BuildPath(key)}' cannot parse enum {typeof(TEnum).Name} from '{text}'.");
    }

    public SnapReader ReadObject(string key)
    {
        JsonNode node = GetRequiredNode(key);

        if (node is JsonObject obj)
        {
            return new SnapReader(obj, Version, BuildPath(key), _limits);
        }

        throw new SnapFormatException($"Field '{BuildPath(key)}' is not a JSON object.");
    }

    public SnapArrayReader ReadArray(string key)
    {
        JsonNode node = GetRequiredNode(key);

        if (node is JsonArray array)
        {
            if (array.Count > _limits.MaxArrayItems)
            {
                throw new SnapLimitExceededException(
                    $"Snap array '{BuildPath(key)}' has {array.Count} items, exceeding limit {_limits.MaxArrayItems}.");
            }

            return new SnapArrayReader(array, Version, BuildPath(key), _limits);
        }

        throw new SnapFormatException($"Field '{BuildPath(key)}' is not a JSON array.");
    }

    private T ReadRequiredValue<T>(string key)
    {
        JsonNode node = GetRequiredNode(key);

        try
        {
            return node.GetValue<T>();
        }
        catch (Exception ex)
        {
            throw new SnapFormatException(
                $"Field '{BuildPath(key)}' cannot be read as {typeof(T).Name}.",
                ex);
        }
    }

    private bool TryReadValue<T>(string key, out T value)
    {
        value = default!;

        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (!_data.TryGetPropertyValue(key, out JsonNode? node) || node == null)
        {
            return false;
        }

        try
        {
            value = node.GetValue<T>();
            if (value is string text)
            {
                ValidateStringLength(text, BuildPath(key));
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private JsonNode GetRequiredNode(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SnapFormatException("Snap field key cannot be empty.");
        }

        if (!_data.TryGetPropertyValue(key, out JsonNode? node) || node == null)
        {
            throw new SnapFormatException($"Missing required snap field '{BuildPath(key)}'.");
        }

        return node;
    }

    private string BuildPath(string key)
    {
        return $"{_path}.{key}";
    }

    private void ValidateStringLength(string value, string path)
    {
        if (value.Length > _limits.MaxStringChars)
        {
            throw new SnapLimitExceededException(
                $"Snap string '{path}' has {value.Length} characters, exceeding limit {_limits.MaxStringChars}.");
        }
    }
}
