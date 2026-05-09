namespace LayerBase.Actor;

internal struct ActorFastState
{
    public int Version;
    public int SlotIndex;
    public int Generation;
    public int StorageRouteId;

    public void Bind(
        int slotIndex,
        int generation,
        int storageRouteId)
    {
        Version++;
        SlotIndex = slotIndex;
        Generation = generation;
        StorageRouteId = storageRouteId;
    }

    public void MarkDead()
    {
        Version++;
        SlotIndex = -1;
        Generation = 0;
        StorageRouteId = -1;
    }
}
