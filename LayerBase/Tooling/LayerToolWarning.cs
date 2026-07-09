namespace LayerBase.Tooling;

public sealed class LayerToolWarning
{
    public LayerToolWarning(string code, string message, LayerToolEntry? entry = null)
    {
        Code = !string.IsNullOrWhiteSpace(code)
            ? code
            : throw new ArgumentException("Warning code cannot be null or empty.", nameof(code));
        Message = !string.IsNullOrWhiteSpace(message)
            ? message
            : throw new ArgumentException("Warning message cannot be null or empty.", nameof(message));
        Entry = entry;
    }

    public string Code { get; }

    public string Message { get; }

    public LayerToolEntry? Entry { get; }
}
