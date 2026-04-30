using LayerBase.Layers;

namespace LayerBase.Event.Delay;

internal sealed class DelayPublisher<T> : IDelayPublisher<T>, IDelayPublisherUpdater where T : struct
{
    private DelayState? _state;

    public DelayPublisher(Layer owner)
    {
        Owner = owner;
    }

    public bool HasValue
    {
        get
        {
            var state = Volatile.Read(ref _state);
            return state != null && BitConverter.Int32BitsToSingle(Volatile.Read(ref state.TtlBits)) > 0;
        }
    }

    public DelayDirection Direction => Volatile.Read(ref _state)?.Direction ?? DelayDirection.None;

    public int ContractId => Volatile.Read(ref _state)?.ContractId ?? 0;

    public bool TryGet(out T value)
    {
        var state = Volatile.Read(ref _state);
        if (state == null || BitConverter.Int32BitsToSingle(Volatile.Read(ref state.TtlBits)) <= 0)
        {
            value = default;
            return false;
        }

        value = state.Value;
        return true;
    }

    public bool TryTake(out T value)
    {
        var state = Volatile.Read(ref _state);
        if (state == null || BitConverter.Int32BitsToSingle(Volatile.Read(ref state.TtlBits)) <= 0)
        {
            value = default;
            return false;
        }

        if (!ReferenceEquals(Interlocked.CompareExchange(ref _state, null, state), state))
        {
            value = default;
            return false;
        }

        value = state.Value;
        return true;
    }

    public Layer Owner { get; }

    public void Update(float deltaTime)
    {
        var state = Volatile.Read(ref _state);
        if (state == null) return;


        float current, next;
        int initial, computed;
        do
        {
            initial = Volatile.Read(ref state.TtlBits);
            current = BitConverter.Int32BitsToSingle(initial);
            if (current <= 0) break;
            next = current - deltaTime;
            computed = BitConverter.SingleToInt32Bits(next);
        } while (Interlocked.CompareExchange(ref state.TtlBits, computed, initial) != initial);

        if (BitConverter.Int32BitsToSingle(Volatile.Read(ref state.TtlBits)) <= 0)
            Interlocked.CompareExchange(ref _state, null, state);
    }

    public void ClearValue()
    {
        Interlocked.Exchange(ref _state, null);
    }

    public void Reset()
    {
        ClearValue();
    }

    internal void Publish(in T value, float ttlSeconds, DelayDirection direction, int contractId = 0)
    {
        Volatile.Write(ref _state, new DelayState(value, ttlSeconds, direction, contractId));
    }

    private sealed class DelayState
    {
        public readonly T Value;
        public readonly DelayDirection Direction;
        public readonly int ContractId;
        public int TtlBits;

        public DelayState(in T value, float ttlSeconds, DelayDirection direction, int contractId)
        {
            Value = value;
            TtlBits = BitConverter.SingleToInt32Bits(ttlSeconds);
            Direction = direction;
            ContractId = contractId;
        }
    }
}
