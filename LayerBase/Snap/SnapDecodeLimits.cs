namespace LayerBase.Snap;

public sealed class SnapDecodeLimits
{
    public int MaxInputChars { get; init; } = 16 * 1024 * 1024;

    public int MaxSections { get; init; } = 4096;

    public int MaxArrayItems { get; init; } = 1_000_000;

    public int MaxStringChars { get; init; } = 1024 * 1024;

    public int MaxDepth { get; init; } = 64;

    public static SnapDecodeLimits Default { get; } = new();
}
