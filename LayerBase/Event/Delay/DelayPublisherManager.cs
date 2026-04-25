using System;
using System.Collections.Generic;
using LayerBase.Layers;

namespace LayerBase.Event.Delay;

internal interface IDelayPublisherUpdater
{
    Layer Owner { get; }
    int ContractId { get; }
    bool HasValue { get; }
    void Update(float deltaTime);
    void Reset();
    void ClearValue();
}

internal sealed class DelayPublisherManager : IDelayPublisherManager
{
    private readonly object _lock = new();
    private readonly List<IDelayPublisherUpdater> _publishers = new(64);
    private readonly HashSet<IDelayPublisherUpdater> _set = new();

    private DelayPublisherManager()
    {
    }

    public static DelayPublisherManager Instance { get; } = new();

    public void Update(float deltaTime)
    {
        if (deltaTime < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));

        lock (_lock)
        {
            for (var i = 0; i < _publishers.Count; i++) _publishers[i].Update(deltaTime);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var publisher in _publishers) publisher.Reset();
            _publishers.Clear();
            _set.Clear();
        }
    }

    internal void Register(IDelayPublisherUpdater publisher)
    {
        if (publisher == null) throw new ArgumentNullException(nameof(publisher));

        lock (_lock)
        {
            if (_set.Add(publisher)) _publishers.Add(publisher);
        }
    }

    internal void Unregister(IDelayPublisherUpdater publisher)
    {
        if (publisher == null) return;
        lock (_lock)
        {
            if (_set.Remove(publisher)) _publishers.Remove(publisher);
        }
    }

    internal void UnregisterRange(IEnumerable<IDelayPublisherUpdater> publishers)
    {
        if (publishers == null) return;
        lock (_lock)
        {
            foreach (var publisher in publishers)
                if (_set.Remove(publisher))
                    _publishers.Remove(publisher);
        }
    }

    internal void NotifyPublished(Layer owner, int contractId, IDelayPublisherUpdater source)
    {
        if (contractId == 0) return;

        lock (_lock)
        {
            for (var i = 0; i < _publishers.Count; i++)
            {
                var pub = _publishers[i];
                if (!ReferenceEquals(pub, source) && ReferenceEquals(pub.Owner, owner) && pub.HasValue &&
                    pub.ContractId == contractId) pub.ClearValue();
            }
        }
    }
}

