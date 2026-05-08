namespace LayerBase.Actor;

public readonly struct ActorId : IEquatable<ActorId>
{
    public readonly int ArchetypeId;
    public readonly ushort TypeStorageIndex;
    public readonly int SlotIndex;
    public readonly int Generation;

    public ActorId(int archetypeId, ushort typeStorageIndex, int slotIndex, int generation)
    {
        ArchetypeId = archetypeId;
        TypeStorageIndex = typeStorageIndex;
        SlotIndex = slotIndex;
        Generation = generation;
    }

    public bool Equals(ActorId other)
    {
        return ArchetypeId == other.ArchetypeId
            && TypeStorageIndex == other.TypeStorageIndex
            && SlotIndex == other.SlotIndex
            && Generation == other.Generation;
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ArchetypeId, TypeStorageIndex, SlotIndex, Generation);
    }
}
