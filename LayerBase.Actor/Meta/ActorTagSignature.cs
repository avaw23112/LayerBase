namespace LayerBase.Actor;

public readonly struct ActorTagSignature : IEquatable<ActorTagSignature>
{
    private readonly int[] _ids;

    public static ActorTagSignature Empty => new(Array.Empty<int>());

    public ActorTagSignature(int[]? ids)
    {
        _ids = ActorSignatureUtility.Normalize(ids);
    }

    internal ReadOnlySpan<int> Ids => _ids;

    public bool ContainsAll(ActorTagSignature query)
    {
        return ActorSignatureUtility.ContainsAll(_ids, query._ids);
    }

    public bool ContainsAny(ActorTagSignature query)
    {
        return ActorSignatureUtility.ContainsAny(_ids, query._ids);
    }

    public bool Equals(ActorTagSignature other)
    {
        return _ids.AsSpan().SequenceEqual(other._ids);
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorTagSignature other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (int id in _ids)
        {
            hash.Add(id);
        }

        return hash.ToHashCode();
    }
}
