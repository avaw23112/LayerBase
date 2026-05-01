namespace LayerBase.Event.Delay;

public interface IDelayPublisher
{
    bool HasValue { get; }
}

public interface IDelayPublisher<T> : IDelayPublisher where T : struct
{
    int ContractId { get; }
    bool TryGet(out T value);
    bool TryTake(out T value);
    void Publish(in T value, float ttlSeconds, int contractId = 0);
}
