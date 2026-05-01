namespace LayerBase.Event.Delay;

internal interface IDelayPublisherInternal
{
    void ClearValue();
    void Deactivate();
    bool TryExpire(int valueVersion);
    void Reset();
}
