namespace LayerBase.Core.EventStateTrace;

/// <summary>
/// EventState 的唯一标识，包含位置与版本，用于复用与防止陈旧引用。
/// </summary>
public readonly struct EventStateToken : IEquatable<EventStateToken>
{
    internal readonly int Index;
    internal readonly ushort Version;

    internal EventStateToken(int index, ushort version)
    {
        Index = index;
        Version = version;
    }

    public bool IsValid => Version != 0 && Index >= 0;
    public static EventStateToken None => default;

    public static bool operator ==(EventStateToken a, EventStateToken b) => a.Index == b.Index && a.Version == b.Version;
    public static bool operator !=(EventStateToken a, EventStateToken b) => !(a == b);

    public bool Equals(EventStateToken other) => Index == other.Index && Version == other.Version;
    public override bool Equals(object? obj) => obj is EventStateToken other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Index, Version);
    public override string ToString() => IsValid ? $"{Index}:{Version}" : "None";
}
