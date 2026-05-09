namespace LayerBase.Actor;

public readonly struct ActorId : IEquatable<ActorId>
{
    public readonly int ArchetypeId;
    public readonly ushort TypeStorageIndex;
    public readonly int SlotIndex;
    public readonly int Generation;
    public readonly int FastIndex;

    public ActorId(int archetypeId, ushort typeStorageIndex, int slotIndex, int generation)
        : this(archetypeId, typeStorageIndex, slotIndex, generation, -1)
    {
    }

    public ActorId(int archetypeId, ushort typeStorageIndex, int slotIndex, int generation, int fastIndex)
    {
        ArchetypeId = archetypeId;
        TypeStorageIndex = typeStorageIndex;
        SlotIndex = slotIndex;
        Generation = generation;
        FastIndex = fastIndex;
    }

    public bool Equals(ActorId other)
    {
        return ArchetypeId == other.ArchetypeId
            && TypeStorageIndex == other.TypeStorageIndex
            && SlotIndex == other.SlotIndex
            && Generation == other.Generation
            && FastIndex == other.FastIndex;
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ArchetypeId, TypeStorageIndex, SlotIndex, Generation, FastIndex);
    }
}
