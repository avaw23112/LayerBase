using System;
using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Event.Delay
{
    internal sealed class DelayPublisher<T> : IDelayPublisher<T>, IDelayPublisherUpdater where T : struct
    {
        private readonly Layer _owner;
        private T _value;
        private float _ttl;
        private bool _hasValue;
        private DelayDirection _direction;
        private int _contractId;

        internal DelayPublisher(Layer owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public bool HasValue => _hasValue;
        public DelayDirection Direction => _direction;
        public Layer Owner => _owner;
        public int ContractId => _contractId;

        public bool TryGet(out T value)
        {
            if (_hasValue)
            {
                value = _value;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryTake(out T value)
        {
            if (!TryGet(out value))
            {
                return false;
            }

            ClearValue();
            return true;
        }

        internal void Publish(in T value, float ttlSeconds, DelayDirection direction, int contractId)
        {
            if (ttlSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(ttlSeconds));

            _value = value;
            _ttl = ttlSeconds;
            _direction = direction;
            _contractId = contractId;
            _hasValue = true;
        }

        public void Update(float deltaTime)
        {
            if (!_hasValue)
            {
                return;
            }

            _ttl -= deltaTime;
            if (_ttl <= 0)
            {
                _hasValue = false;
                _ttl = 0;
                _direction = DelayDirection.None;
                _contractId = 0;
            }
        }

        public void Reset()
        {
            _hasValue = false;
            _ttl = 0;
            _direction = DelayDirection.None;
            _contractId = 0;
        }

        public void ClearValue()
        {
            _hasValue = false;
            _ttl = 0;
            _direction = DelayDirection.None;
            _contractId = 0;
        }
    }
}
