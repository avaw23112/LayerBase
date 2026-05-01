using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Event.Delay;

internal sealed class DelayPublisher<T> : IDelayPublisher<T>, IDelayPublisherInternal where T : struct
{
    private T _value;
    private bool _hasValue;
    private int _valueVersion;
    private DelayTimerHandle _timerHandle = DelayTimerHandle.Invalid;

    private int _publisherId;
    private readonly DelayPublisherManager _manager;
    private readonly object _lock = new();

    public DelayPublisher(DelayPublisherManager manager, Layer owner)
    {
        _manager = manager;
        Owner = owner;
    }

    internal void SetId(int id) => _publisherId = id;

    public Layer Owner { get; }

    public bool HasValue
    {
        get { lock (_lock) return _hasValue; }
    }

    public int ContractId { get; private set; }

    public bool TryGet(out T value)
    {
        lock (_lock)
        {
            if (!_hasValue)
            {
                value = default;
                return false;
            }
            value = _value;
            return true;
        }
    }

    public bool TryTake(out T value)
    {
        lock (_lock)
        {
            if (!_hasValue)
            {
                value = default;
                return false;
            }
            value = _value;
            ClearInternal();
            return true;
        }
    }

    public void Publish(in T value, float ttlSeconds, int contractId = 0)
    {
        int finalContractId;
        int valueVersion;
        float finalTtl;
        DelayTimerHandle oldHandle;

        lock (_lock)
        {
            var eventId = EventTypeId<T>.Id;
            var policy = _manager.PolicyTable?.GetBufferPolicy(eventId);

            finalTtl = ttlSeconds > 0 ? ttlSeconds : (policy?.DefaultTtlSeconds ?? 0.5f);
            finalContractId = contractId != 0 ? contractId : (policy?.UseContractReplace == true ? eventId : 0);

            _value = value;
            _hasValue = true;
            _valueVersion++;
            valueVersion = _valueVersion;
            ContractId = finalContractId;
            oldHandle = _timerHandle;
        }

        if (finalContractId != 0)
        {
            _manager.NotifyPublished(_publisherId, finalContractId);
        }

        var newHandle = _manager.ScheduleExpire(_publisherId, valueVersion, finalTtl, oldHandle);
        lock (_lock)
        {
            if (_hasValue && _valueVersion == valueVersion)
            {
                _timerHandle = newHandle;
            }
        }
    }

    public bool TryExpire(int valueVersion)
    {
        lock (_lock)
        {
            if (!_hasValue || _valueVersion != valueVersion) return false;
            ClearInternal();
            return true;
        }
    }

    public void ClearValue()
    {
        lock (_lock)
        {
            ClearInternal();
        }
    }

    public void Reset()
    {
        ClearValue();
    }

    private void ClearInternal()
    {
        _hasValue = false;
        _value = default;
        _timerHandle = DelayTimerHandle.Invalid;
        ContractId = 0;
    }
}
