using System.Text.Json.Nodes;

namespace LayerBase.Snap;

public readonly struct SnapArrayWriter
{
    private readonly JsonArray _array;
    private readonly string _path;

    internal SnapArrayWriter(JsonArray array, string path)
    {
        _array = array ?? throw new ArgumentNullException(nameof(array));
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public void AddInt32(int value) => _array.Add(value);

    public void AddInt64(long value) => _array.Add(value);

    public void AddSingle(float value) => _array.Add(value);

    public void AddDouble(double value) => _array.Add(value);

    public void AddBoolean(bool value) => _array.Add(value);

    public void AddString(string? value) => _array.Add(value);

    public void AddEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        _array.Add(value.ToString());
    }

    public SnapWriter AddObject()
    {
        var child = new JsonObject();
        _array.Add(child);
        return new SnapWriter(child, $"{_path}[{_array.Count - 1}]");
    }
}
