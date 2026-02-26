namespace LayerBase.Event.Delay
{
    public interface IDelayPublisherManager
    {
        void Update(float deltaTime);
        void Clear();
    }
}
