namespace LayerBase.Core.Event;

public readonly struct DelayContractKey : IEquatable<DelayContractKey>
{
    public readonly int OwnerId;
    public readonly int ContractId;

    public DelayContractKey(int ownerId, int contractId)
    {
        OwnerId = ownerId;
        ContractId = contractId;
    }

    public bool Equals(DelayContractKey other)
    {
        return OwnerId == other.OwnerId && ContractId == other.ContractId;
    }

    public override bool Equals(object? obj) => obj is DelayContractKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(OwnerId, ContractId);
}
