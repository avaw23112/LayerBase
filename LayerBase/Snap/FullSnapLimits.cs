namespace LayerBase.Snap;

public readonly record struct FullSnapLimits(
    long MaxTotalBytes,
    long MaxScopeBytes,
    int MaxScopeCount,
    int MaxSectionsPerScope,
    int MaxJsonDepth,
    int MinFormatVersion,
    int MaxFormatVersion)
{
    public static FullSnapLimits Default { get; } = new(
        MaxTotalBytes: 16 * 1024 * 1024,
        MaxScopeBytes: 4 * 1024 * 1024,
        MaxScopeCount: 1024,
        MaxSectionsPerScope: 1024,
        MaxJsonDepth: 64,
        MinFormatVersion: 1,
        MaxFormatVersion: 1);

    internal void ThrowIfInvalid()
    {
        if (MaxTotalBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxTotalBytes), "MaxTotalBytes must be positive.");
        if (MaxScopeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxScopeBytes), "MaxScopeBytes must be positive.");
        if (MaxScopeCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxScopeCount), "MaxScopeCount must be positive.");
        if (MaxSectionsPerScope <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxSectionsPerScope), "MaxSectionsPerScope must be positive.");
        if (MaxJsonDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxJsonDepth), "MaxJsonDepth must be positive.");
        if (MinFormatVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(MinFormatVersion), "MinFormatVersion must be positive.");
        if (MaxFormatVersion < MinFormatVersion)
            throw new ArgumentOutOfRangeException(nameof(MaxFormatVersion), "MaxFormatVersion must be greater than or equal to MinFormatVersion.");
    }

    internal SnapReadLimits ToReadLimits()
    {
        ThrowIfInvalid();

        return new SnapReadLimits
        {
            MaxJsonBytes = ClampToInt(MaxTotalBytes),
            MaxSections = ClampToInt((long)MaxScopeCount * MaxSectionsPerScope),
            MaxSectionBytes = ClampToInt(MaxScopeBytes),
            MaxTotalSectionBytes = ClampToInt(MaxTotalBytes),
            MaxJsonDepth = MaxJsonDepth
        };
    }

    private static int ClampToInt(long value)
    {
        return value >= int.MaxValue ? int.MaxValue : (int)value;
    }
}
