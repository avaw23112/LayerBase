using System.Text.Json.Nodes;

namespace LayerBase.Snap;

public readonly struct SnapArrayReader
{
    private readonly JsonArray _array;
    private readonly string _path;

    public int Count => _array.Count;

    public int Version { get; }

    internal SnapArrayReader(JsonArray array, int version, string path)
    {
        _array = array ?? throw new ArgumentNullException(nameof(array));
        Version = version;
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public SnapReader ReadObject(int index)
    {
        JsonNode node = GetRequiredNode(index);

        if (node is JsonObject obj)
        {
            return new SnapReader(obj, Version, BuildPath(index));
        }

        throw new SnapFormatException($"Array element '{BuildPath(index)}' is not a JSON object.");
    }

    public int ReadInt32(int index) => ReadRequiredValue<int>(index);

    public long ReadInt64(int index) => ReadRequiredValue<long>(index);

    public float ReadSingle(int index) => ReadRequiredValue<float>(index);

    public double ReadDouble(int index) => ReadRequiredValue<double>(index);

    public bool ReadBoolean(int index) => ReadRequiredValue<bool>(index);

    public string ReadString(int index)
    {
        string? value = ReadRequiredValue<string?>(index);

        if (value == null)
        {
            throw new SnapFormatException($"Array element '{BuildPath(index)}' cannot be null.");
        }

        return value;
    }

    public TEnum ReadEnum<TEnum>(int index)
        where TEnum : struct, Enum
    {
        string text = ReadString(index);

        if (Enum.TryParse(text, out TEnum value))
        {
            return value;
        }

        throw new SnapFormatException(
            $"Array element '{BuildPath(index)}' cannot parse enum {typeof(TEnum).Name} from '{text}'.");
    }

    public bool TryReadInt32(int index, out int value) => TryReadValue(index, out value);

    public bool TryReadSingle(int index, out float value) => TryReadValue(index, out value);

    public bool TryReadString(int index, out string value)
    {
        if (!TryReadValue<string?>(index, out string? result) || result == null)
        {
            value = string.Empty;
            return false;
        }

        value = result;
        return true;
    }

    private JsonNode GetRequiredNode(int index)
    {
        ValidateIndex(index);

        JsonNode? node = _array[index];

        if (node == null)
        {
            throw new SnapFormatException($"Array element '{BuildPath(index)}' is null.");
        }

        return node;
    }

    private TValue ReadRequiredValue<TValue>(int index)
    {
        JsonNode node = GetRequiredNode(index);

        try
        {
            return node.GetValue<TValue>();
        }
        catch (Exception ex)
        {
            throw new SnapFormatException(
                $"Array element '{BuildPath(index)}' cannot be read as {typeof(TValue).Name}.",
                ex);
        }
    }

    private bool TryReadValue<TValue>(int index, out TValue value)
    {
        value = default!;

        if ((uint)index >= (uint)_array.Count)
        {
            return false;
        }

        JsonNode? node = _array[index];

        if (node == null)
        {
            return false;
        }

        try
        {
            value = node.GetValue<TValue>();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)_array.Count)
        {
            throw new SnapFormatException(
                $"Array index out of range: {BuildPath(index)}, count = {_array.Count}.");
        }
    }

    private string BuildPath(int index)
    {
        return $"{_path}[{index}]";
    }
}
