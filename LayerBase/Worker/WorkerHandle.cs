namespace LayerBase.Worker;

public readonly struct WorkerHandle : IEquatable<WorkerHandle>
{
    internal WorkerHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }

    internal int Index { get; }

    internal int Version { get; }

    public bool IsValid => Index >= 0 && Version > 0;

    public static WorkerHandle Invalid => new(-1, 0);

    public bool Equals(WorkerHandle other)
    {
        return Index == other.Index && Version == other.Version;
    }

    public override bool Equals(object? obj)
    {
        return obj is WorkerHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Version);
    }

    public static bool operator ==(WorkerHandle left, WorkerHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WorkerHandle left, WorkerHandle right)
    {
        return !left.Equals(right);
    }
}
