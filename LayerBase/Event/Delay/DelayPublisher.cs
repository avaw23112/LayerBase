using LayerBase.Layers;

namespace LayerBase.Event.Delay;

internal sealed class DelayPublisher<T> : IDelayPublisher<T>, IDelayPublisherUpdater where T : struct
{
    private int _hasValueInt;
    private int _ttlBits;
    private T _value;

    public DelayPublisher(Layer owner)
    {
        Owner = owner;
    }

    public bool HasValue => Volatile.Read(ref _hasValueInt) == 1 &&
                            BitConverter.Int32BitsToSingle(Volatile.Read(ref _ttlBits)) > 0;

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


        float current, next;
        int initial, computed;
        do
        {
            initial = Volatile.Read(ref _ttlBits);
            current = BitConverter.Int32BitsToSingle(initial);
            if (current <= 0) break;
            next = current - deltaTime;
            computed = BitConverter.SingleToInt32Bits(next);
        } while (Interlocked.CompareExchange(ref _ttlBits, computed, initial) != initial);

        if (BitConverter.Int32BitsToSingle(Volatile.Read(ref _ttlBits)) <= 0) ClearValue();
    }

    public void ClearValue()
    {
        if (Interlocked.Exchange(ref _hasValueInt, 0) == 0) return;
        Volatile.Write(ref _ttlBits, 0);
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
        Volatile.Write(ref _ttlBits, BitConverter.SingleToInt32Bits(ttlSeconds));
        Direction = direction;
        ContractId = contractId;
        Interlocked.Exchange(ref _hasValueInt, 1);
    }
}