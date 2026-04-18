using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Event.Delay
{
    internal sealed class DelayPublisher<T> : IDelayPublisher<T>, IDelayPublisherUpdater where T : struct
    {
        private readonly Layer _owner;
        private T _value;
        private float _ttl;
        private DelayDirection _direction;
        private int _contractId;
        private int _hasValueInt; // 使用 int 以支持 Interlocked

        public bool HasValue => Volatile.Read(ref _hasValueInt) == 1 && Volatile.Read(ref _ttl) > 0;
        public DelayDirection Direction => _direction;
        public int ContractId => _contractId;

        public DelayPublisher(Layer owner) { _owner = owner; }
        public Layer Owner => _owner;

        public bool TryGet(out T value)
        {
            if (!HasValue) { value = default; return false; }
            value = _value; return true;
        }

        public bool TryTake(out T value)
        {
            if (!HasValue) { value = default; return false; }
            value = _value; ClearValue(); return true;
        }

        internal void Publish(in T value, float ttlSeconds, DelayDirection direction, int contractId = 0)
        {
            _value = value;
            Volatile.Write(ref _ttl, ttlSeconds);
            _direction = direction;
            _contractId = contractId;
            Volatile.Write(ref _hasValueInt, 1);
        }

        public void Update(float deltaTime)
        {
            if (Volatile.Read(ref _hasValueInt) == 0) return;
            
            float newTtl = Volatile.Read(ref _ttl) - deltaTime;
            Volatile.Write(ref _ttl, newTtl);
            
            if (newTtl <= 0) ClearValue();
        }

        public void ClearValue()
        {
            Volatile.Write(ref _hasValueInt, 0);
            Volatile.Write(ref _ttl, 0);
            _value = default;
            _direction = DelayDirection.None;
            _contractId = 0;
        }

        public void Reset() => ClearValue();
    }
}
