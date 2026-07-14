namespace LayerBase.Worker;

public readonly struct WorkerHandle : IEquatable<WorkerHandle>
{
    public WorkerHandle(int id, int version)
    {
        Id = id;
        Version = version;
    }

    public int Id { get; }

    public int Version { get; }

    public bool IsInvalid => Id < 0 || Version <= 0;

    public static WorkerHandle Invalid { get; } = new(-1, 0);

    public bool Equals(WorkerHandle other)
    {
        return Id == other.Id && Version == other.Version;
    }

    public override bool Equals(object? obj)
    {
        return obj is WorkerHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Version);
    }
}
