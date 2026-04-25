using System;

namespace LayerBase.Core.ResponsibilityChain;

/// <summary>
///     ������ʶ��ID
/// </summary>
public struct RcOwnerToken : IEquatable<RcOwnerToken>
{
    public long Id;

    public static RcOwnerToken CreateId()
    {
        return new RcOwnerToken(Guid.NewGuid().GetHashCode());
    }

    public RcOwnerToken(long val)
    {
        Id = val;
    }

    public static RcOwnerToken Zero => new(0);

    public bool IsOwnedBy(RcOwnerToken token)
    {
        return token.Id == Id;
    }

    public void Reset()
    {
        Id = 0;
    }

    public static bool operator ==(RcOwnerToken a, RcOwnerToken b)
    {
        return a.Id == b.Id;
    }

    public static bool operator !=(RcOwnerToken a, RcOwnerToken b)
    {
        return !(a == b);
    }

    public bool Equals(RcOwnerToken other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is RcOwnerToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

