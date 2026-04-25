using System;

namespace LayerBase.Tools.Timer;

/// <summary>
///     ��ʱ�����Ψһ��ʶ������׷����ȡ����
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

