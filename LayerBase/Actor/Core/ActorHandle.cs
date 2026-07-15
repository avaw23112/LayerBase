namespace LayerBase.Actor;

public readonly struct ActorHandle : IEquatable<ActorHandle>
{
    public static readonly ActorHandle Invalid = new(-1, -1, -1, -1);

    public readonly int TypeId;
    public readonly int Index;
    public readonly int Version;
    public readonly int RuntimeGeneration;

    public ActorHandle(int typeId, int index, int version, int runtimeGeneration)
    {
        TypeId = typeId;
        Index = index;
        Version = version;
        RuntimeGeneration = runtimeGeneration;
    }

    public bool IsValid => TypeId >= 0 && Index >= 0 && Version >= 0 && RuntimeGeneration >= 0;

    internal ActorId ActorId => IsValid
        ? new ActorId(TypeId, Index, Version)
        : ActorId.Invalid;

    public static ActorHandle FromActorId(ActorId actorId, int runtimeGeneration)
    {
        return actorId.IsValid
            ? new ActorHandle(actorId.ArchetypeId, actorId.SlotIndex, actorId.Generation, runtimeGeneration)
            : Invalid;
    }

    public bool Equals(ActorHandle other)
    {
        return TypeId == other.TypeId &&
               Index == other.Index &&
               Version == other.Version &&
               RuntimeGeneration == other.RuntimeGeneration;
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TypeId, Index, Version, RuntimeGeneration);
    }
}
