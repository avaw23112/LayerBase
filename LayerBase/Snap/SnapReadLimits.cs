namespace LayerBase.Snap;

public sealed class SnapReadLimits
{
    public static SnapReadLimits Default { get; } = new();

    public int MaxJsonBytes { get; init; } = 16 * 1024 * 1024;

    public int MaxSections { get; init; } = 1024;

    public int MaxSectionBytes { get; init; } = 4 * 1024 * 1024;

    public int MaxTotalSectionBytes { get; init; } = 16 * 1024 * 1024;

    public int MaxJsonDepth { get; init; } = 64;

    internal void ThrowIfInvalid()
    {
        if (MaxJsonBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxJsonBytes), "MaxJsonBytes must be positive.");
        if (MaxSections <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxSections), "MaxSections must be positive.");
        if (MaxSectionBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxSectionBytes), "MaxSectionBytes must be positive.");
        if (MaxTotalSectionBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxTotalSectionBytes), "MaxTotalSectionBytes must be positive.");
        if (MaxJsonDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxJsonDepth), "MaxJsonDepth must be positive.");
    }
}
