namespace LayerBase.Actor;

public readonly struct ActorId : IEquatable<ActorId>
{
    public static readonly ActorId Invalid = new(
        archetypeId: -1,
        slotIndex: -1,
        generation: -1);

    public readonly int ArchetypeId;
    public readonly int SlotIndex;
    public readonly int Generation;

    public bool IsValid
    {
        get
        {
            return ArchetypeId >= 0
                   && SlotIndex >= 0;
        }
    }

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
