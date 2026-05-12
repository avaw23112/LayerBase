using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Event.Delay;

internal sealed class DelayPublisher<T> : IDelayPublisher<T>, IDelayPublisherInternal where T : struct
{
    private T _value;
    private bool _hasValue;
    private int _valueVersion;
    private DelayTimerHandle _timerHandle = DelayTimerHandle.Invalid;

    private int _publisherId = -1;
    private bool _deactivated;
    private readonly DelayPublisherManager _manager;
    private readonly object _lock = new();

    public DelayPublisher(DelayPublisherManager manager, Layer owner)
    {
        _manager = manager;
        Owner = owner;
    }

    public int PublisherId => _publisherId;

    internal void SetId(int id) => _publisherId = id;

    public Layer Owner { get; }

    public bool HasValue
    {
        get
        {
            lock (_lock) return _hasValue;
        }
    }

    public bool HasActiveDelays => HasValue;

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

        bool wasEmpty;

        lock (_lock)
        {
            if (_deactivated)
                throw new ObjectDisposedException(nameof(DelayPublisher<T>));
            _manager.ThrowIfDisposed();

            var eventId = EventTypeId<T>.Id;
            var policy = _manager.PolicyTable?.GetBufferPolicy(eventId);

            finalTtl = ttlSeconds > 0 ? ttlSeconds : (policy?.DefaultTtlSeconds ?? 0.5f);
            finalContractId = contractId != 0 ? contractId : (policy?.UseContractReplace == true ? eventId : 0);

            wasEmpty = !_hasValue;
            _value = value;
            _hasValue = true;
            _valueVersion++;
            valueVersion = _valueVersion;
            ContractId = finalContractId;
            oldHandle = _timerHandle;
        }

        if (wasEmpty) Owner.OwnerContext?.MarkDelayDirty();

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

    public void Deactivate()
    {
        DelayTimerHandle oldHandle;
        lock (_lock)
        {
            if (_deactivated) return;
            _deactivated = true;
            oldHandle = _timerHandle;
            ClearInternal();
            _publisherId = -1;
        }

        _manager.CancelExpire(oldHandle);
    }

    public void Reset()
    {
        ClearValue();
    }

    private void ClearInternal()
    {
        bool wasActive = _hasValue;
        _hasValue = false;
        _value = default;
        _timerHandle = DelayTimerHandle.Invalid;
        ContractId = 0;
        if (wasActive) Owner.OwnerContext?.MarkDelayDirty();
    }
}