using LayerBase.Layers;

namespace LayerBase.Event.Delay;

internal sealed class DelayPublisher<T> : IDelayPublisher<T>, IDelayPublisherUpdater where T : struct
{
    private int _hasValueInt; // 使用 int 以支持 Interlocked
    private float _ttl;
    private T _value;

    public DelayPublisher(Layer owner)
    {
        Owner = owner;
    }

    public bool HasValue => Volatile.Read(ref _hasValueInt) == 1 && Volatile.Read(ref _ttl) > 0;
    public DelayDirection Direction { get; private set; }

    public int ContractId { get; private set; }

    public bool TryGet(out T value)
    {
        if (!HasValue)
        {
            value = default;
            return false;
        }

        value = _value;
        return true;
    }

    public bool TryTake(out T value)
    {
        if (!HasValue)
        {
            value = default;
            return false;
        }

        value = _value;
        ClearValue();
        return true;
    }

    public Layer Owner { get; }

    public void Update(float deltaTime)
    {
        if (Volatile.Read(ref _hasValueInt) == 0) return;

        var newTtl = Volatile.Read(ref _ttl) - deltaTime;
        Volatile.Write(ref _ttl, newTtl);

        if (newTtl <= 0) ClearValue();
    }

    public void ClearValue()
    {
        Volatile.Write(ref _hasValueInt, 0);
        Volatile.Write(ref _ttl, 0);
        _value = default;
        Direction = DelayDirection.None;
        ContractId = 0;
    }

    public void Reset()
    {
        ClearValue();
    }

    internal void Publish(in T value, float ttlSeconds, DelayDirection direction, int contractId = 0)
    {
        _value = value;
        Volatile.Write(ref _ttl, ttlSeconds);
        Direction = direction;
        ContractId = contractId;
        Volatile.Write(ref _hasValueInt, 1);
    }
}