namespace LayerBase.Event.Delay;

internal interface IDelayPublisherManager
{
    void Update(float deltaTime);
    void Clear();
}
