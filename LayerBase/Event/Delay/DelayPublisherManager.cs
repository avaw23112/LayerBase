using System;
using System.Collections.Generic;
using LayerBase.Layers;

namespace LayerBase.Event.Delay
{
    internal interface IDelayPublisherUpdater
    {
        void Update(float deltaTime);
        Layer Owner { get; }
        void Reset();
        int ContractId { get; }
        bool HasValue { get; }
        void ClearValue();
    }

    internal sealed class DelayPublisherManager : IDelayPublisherManager
    {
        private static readonly DelayPublisherManager s_instance = new DelayPublisherManager();
        private readonly List<IDelayPublisherUpdater> _publishers = new List<IDelayPublisherUpdater>(64);
        private readonly HashSet<IDelayPublisherUpdater> _set = new HashSet<IDelayPublisherUpdater>();
        private readonly object _lock = new object();

        public static DelayPublisherManager Instance => s_instance;

        private DelayPublisherManager()
        {
        }

        internal void Register(IDelayPublisherUpdater publisher)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));

            lock (_lock)
            {
                if (_set.Add(publisher))
                {
                    _publishers.Add(publisher);
                }
            }
        }

        internal void Unregister(IDelayPublisherUpdater publisher)
        {
            if (publisher == null) return;
            lock (_lock)
            {
                if (_set.Remove(publisher))
                {
                    _publishers.Remove(publisher);
                }
            }
        }

        internal void UnregisterRange(IEnumerable<IDelayPublisherUpdater> publishers)
        {
            if (publishers == null) return;
            lock (_lock)
            {
                foreach (var publisher in publishers)
                {
                    if (_set.Remove(publisher))
                    {
                        _publishers.Remove(publisher);
                    }
                }
            }
        }

        public void Update(float deltaTime)
        {
            if (deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            lock (_lock)
            {
                for (int i = 0; i < _publishers.Count; i++)
                {
                    _publishers[i].Update(deltaTime);
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                foreach (var publisher in _publishers)
                {
                    publisher.Reset();
                }
                _publishers.Clear();
                _set.Clear();
            }
        }

        internal void NotifyPublished(Layer owner, int contractId, IDelayPublisherUpdater source)
        {
            if (contractId == 0)
            {
                return;
            }

            lock (_lock)
            {
                for (int i = 0; i < _publishers.Count; i++)
                {
                    var pub = _publishers[i];
                    if (!ReferenceEquals(pub, source) && ReferenceEquals(pub.Owner, owner) && pub.HasValue && pub.ContractId == contractId)
                    {
                        pub.ClearValue();
                    }
                }
            }
        }
    }
}
