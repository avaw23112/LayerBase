namespace LayerBase.Scope;

public readonly struct ScopeAddress : IEquatable<ScopeAddress>
{
    public ScopeAddress(int runtimeId, int runtimeGeneration, int scopeId)
    {
        RuntimeId = runtimeId;
        RuntimeGeneration = runtimeGeneration;
        ScopeId = scopeId;
    }

    public int RuntimeId { get; }
    public int RuntimeGeneration { get; }
    public int ScopeId { get; }

    public bool Equals(ScopeAddress other)
    {
        return RuntimeId == other.RuntimeId &&
               RuntimeGeneration == other.RuntimeGeneration &&
               ScopeId == other.ScopeId;
    }

    public override bool Equals(object? obj)
    {
        return obj is ScopeAddress other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RuntimeId, RuntimeGeneration, ScopeId);
    }

    public static bool operator ==(ScopeAddress left, ScopeAddress right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScopeAddress left, ScopeAddress right)
    {
        return !left.Equals(right);
    }
}
