namespace LayerBase.Actor;

public readonly struct ActorPoolStats
{
    public readonly int CreatedTotal;
    public readonly int RentTotal;
    public readonly int ReturnTotal;
    public readonly int AvailableCount;
    public readonly int DroppedOnReturn;
    public readonly int MaxRetained;

    public ActorPoolStats(
        int createdTotal,
        int rentTotal,
        int returnTotal,
        int availableCount,
        int droppedOnReturn,
        int maxRetained)
    {
        CreatedTotal = createdTotal;
        RentTotal = rentTotal;
        ReturnTotal = returnTotal;
        AvailableCount = availableCount;
        DroppedOnReturn = droppedOnReturn;
        MaxRetained = maxRetained;
    }
}