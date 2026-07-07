namespace LayerBase.Tools.Timer;

/// <summary>
/// 定时任务令牌，用于标识和取消一个已注册的定时任务。
/// 包含类型 ID、索引和版本号，通过版本号机制防止悬挂引用。
/// </summary>
public readonly struct TimerToken : IEquatable<TimerToken>
{
    internal readonly int Index;
    internal readonly ushort Version;
    internal readonly int TypeId;

    internal TimerToken(int typeId, int index, ushort version)
    {
        TypeId = typeId;
        Index = index;
        Version = version;
    }

    public bool IsValid => Version != 0 && Index >= 0 && TypeId >= 0;

    public static TimerToken None => default;

    public static bool operator ==(TimerToken a, TimerToken b)
    {
        return a.Index == b.Index && a.Version == b.Version && a.TypeId == b.TypeId;
    }

    public static bool operator !=(TimerToken a, TimerToken b)
    {
        return !(a == b);
    }

    public bool Equals(TimerToken other)
    {
        return this == other;
    }

    public override bool Equals(object? obj)
    {
        return obj is TimerToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Version, TypeId);
    }

    public override string ToString()
    {
        return IsValid ? $"{TypeId}:{Index}:{Version}" : "None";
    }
}