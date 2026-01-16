namespace LayerBase.Tools.Timer
{
    /// <summary>
    /// 定时任务的唯一标识，用于追踪与取消。
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

        public static bool operator ==(TimerToken a, TimerToken b) => a.Index == b.Index && a.Version == b.Version && a.TypeId == b.TypeId;

        public static bool operator !=(TimerToken a, TimerToken b) => !(a == b);

        public bool Equals(TimerToken other) => this == other;

        public override bool Equals(object? obj) => obj is TimerToken other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Index, Version, TypeId);

        public override string ToString() => IsValid ? $"{TypeId}:{Index}:{Version}" : "None";
    }
}
