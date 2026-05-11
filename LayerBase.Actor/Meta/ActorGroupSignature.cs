namespace LayerBase.Actor;

public readonly struct ActorGroupSignature : IEquatable<ActorGroupSignature>
{
    private readonly int[] _ids;

    public static ActorGroupSignature Empty => new(Array.Empty<int>());

    public ActorGroupSignature(int[]? ids)
    {
        _ids = ActorSignatureUtility.Normalize(ids);
    }

    internal ReadOnlySpan<int> Ids => _ids;

    public bool ContainsAll(ActorGroupSignature query)
    {
        return ActorSignatureUtility.ContainsAll(_ids, query._ids);
    }

    public bool ContainsAny(ActorGroupSignature query)
    {
        return ActorSignatureUtility.ContainsAny(_ids, query._ids);
    }

    public bool Equals(ActorGroupSignature other)
    {
        return _ids.AsSpan().SequenceEqual(other._ids);
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorGroupSignature other && Equals(other);
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
