namespace LayerBase.Core.EventCatalogue;

public readonly struct EventCategoryToken : IEquatable<EventCategoryToken>
{
    public int Id { get; }
    public bool IsValid => Id > 0;
    public bool IsEmpty => Id == 0;

    internal EventCategoryToken(int id)
    {
        Id = id;
    }

    public static EventCategoryToken Empty => new(0);

    public bool Equals(EventCategoryToken other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is EventCategoryToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(EventCategoryToken left, EventCategoryToken right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EventCategoryToken left, EventCategoryToken right)
    {
        return !left.Equals(right);
    }
}