namespace LayerBase.Event.Delay;

internal interface IDelayPublisherInternal
{
    void ClearValue();
    bool TryExpire(int valueVersion);
    void Reset();
}
