namespace LayerBase.Actor;

public readonly struct ActorId : IEquatable<ActorId>
{
    public readonly int ArchetypeId;
    public readonly int SlotIndex;
    public readonly int Generation;

    public ActorId(int archetypeId, int slotIndex, int generation)
    {
        ArchetypeId = archetypeId;
        SlotIndex = slotIndex;
        Generation = generation;
    }

    public bool Equals(ActorId other)
    {
        return ArchetypeId == other.ArchetypeId
            && SlotIndex == other.SlotIndex
            && Generation == other.Generation;
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ArchetypeId, SlotIndex, Generation);
    }
}
