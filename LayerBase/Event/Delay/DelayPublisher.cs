using LayerBase.Layers;

namespace LayerBase.Event.Delay;

internal sealed class DelayPublisher<T> : IDelayPublisher<T>, IDelayPublisherUpdater where T : struct
{
    private int _contractId;
    private DelayDirection _direction;
    private int _ttlBits;
    private T _value;
    private int _version;
    private int _writeLock;

    public DelayPublisher(Layer owner)
    {
        Owner = owner;
    }

    public bool HasValue
    {
        get
        {
            while (true)
            {
                var before = Volatile.Read(ref _version);
                if ((before & 1) != 0)
                {
                    Thread.Yield();
                    continue;
                }

                var ttlBits = Volatile.Read(ref _ttlBits);
                var after = Volatile.Read(ref _version);
                if (before == after) return BitConverter.Int32BitsToSingle(ttlBits) > 0;
            }
        }
    }

    public DelayDirection Direction
    {
        get
        {
            while (true)
            {
                var before = Volatile.Read(ref _version);
                if ((before & 1) != 0)
                {
                    Thread.Yield();
                    continue;
                }

                var direction = _direction;
                var after = Volatile.Read(ref _version);
                if (before == after) return direction;
            }
        }
    }

    public int ContractId
    {
        get
        {
            while (true)
            {
                var before = Volatile.Read(ref _version);
                if ((before & 1) != 0)
                {
                    Thread.Yield();
                    continue;
                }

                var contractId = _contractId;
                var after = Volatile.Read(ref _version);
                if (before == after) return contractId;
            }
        }
    }

    public bool TryGet(out T value)
    {
        while (true)
        {
            var before = Volatile.Read(ref _version);
            if ((before & 1) != 0)
            {
                Thread.Yield();
                continue;
            }

            var ttlBits = Volatile.Read(ref _ttlBits);
            var snapshot = _value;
            var after = Volatile.Read(ref _version);
            if (before != after) continue;

            if (BitConverter.Int32BitsToSingle(ttlBits) <= 0)
            {
                value = default;
                return false;
            }

            value = snapshot;
            return true;
        }
    }

    public bool TryTake(out T value)
    {
        AcquireWrite();
        try
        {
            BeginWrite();
            if (BitConverter.Int32BitsToSingle(_ttlBits) <= 0)
            {
                EndWrite();
                value = default;
                return false;
            }

            value = _value;
            ClearFields();
            EndWrite();
            return true;
        }
        finally
        {
            ReleaseWrite();
        }
    }

    public Layer Owner { get; }

    public void Update(float deltaTime)
    {
        AcquireWrite();
        try
        {
            BeginWrite();
            var current = BitConverter.Int32BitsToSingle(_ttlBits);
            if (current > 0)
            {
                var next = current - deltaTime;
                if (next > 0)
                    _ttlBits = BitConverter.SingleToInt32Bits(next);
                else
                    ClearFields();
            }

            EndWrite();
        }
        finally
        {
            ReleaseWrite();
        }
    }

    public void ClearValue()
    {
        AcquireWrite();
        try
        {
            BeginWrite();
            ClearFields();
            EndWrite();
        }
        finally
        {
            ReleaseWrite();
        }
    }

    public void Reset()
    {
        ClearValue();
    }

    internal void Publish(in T value, float ttlSeconds, DelayDirection direction, int contractId = 0)
    {
        AcquireWrite();
        try
        {
            BeginWrite();
            _value = value;
            _ttlBits = BitConverter.SingleToInt32Bits(ttlSeconds);
            _direction = direction;
            _contractId = contractId;
            EndWrite();
        }
        finally
        {
            ReleaseWrite();
        }
    }

    private void AcquireWrite()
    {
        var spin = new SpinWait();
        while (Interlocked.CompareExchange(ref _writeLock, 1, 0) != 0) spin.SpinOnce();
    }

    private void ReleaseWrite()
    {
        Volatile.Write(ref _writeLock, 0);
    }

    private void BeginWrite()
    {
        var version = Volatile.Read(ref _version);
        if ((version & 1) == 0) version++;
        Volatile.Write(ref _version, version);
    }

    private void EndWrite()
    {
        Volatile.Write(ref _version, Volatile.Read(ref _version) + 1);
    }

    private void ClearFields()
    {
        _ttlBits = 0;
        _value = default;
        _direction = DelayDirection.None;
        _contractId = 0;
    }
}
