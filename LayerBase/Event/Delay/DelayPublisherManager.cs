using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Event.Delay;

internal sealed class DelayPublisherManager : IDelayPublisherManager
{
    private readonly List<IDelayPublisherInternal> _publishers = new();
    private readonly Dictionary<DelayContractKey, int> _contractToActivePublisher = new();
    private readonly DelayBufferWheel _wheel;
    private readonly object _lock = new();

    public static DelayPublisherManager Instance { get; private set; } = null!;

    public EventRuntimePolicyTable? PolicyTable { get; private set; }

    internal static void Initialize(DelayBufferOptions options, EventRuntimePolicyTable policyTable)
    {
        Instance = new DelayPublisherManager(options, policyTable);
    }

    private DelayPublisherManager(DelayBufferOptions options, EventRuntimePolicyTable policyTable)
    {
        _wheel = new DelayBufferWheel(options, this);
        PolicyTable = policyTable;
    }

    public int RegisterPublisher(IDelayPublisherInternal publisher)
    {
        lock (_lock)
        {
            int id = _publishers.Count;
            _publishers.Add(publisher);
            return id;
        }
    }

    public DelayTimerHandle ScheduleExpire(int publisherId, int valueVersion, float ttlSeconds, DelayTimerHandle oldHandle)
    {
        if (oldHandle.IsValid) _wheel.Cancel(oldHandle);
        return _wheel.Schedule(publisherId, valueVersion, ttlSeconds);
    }

    public void Tick(float deltaTime)
    {
        _wheel.Tick(deltaTime);
    }

    internal void ExpirePublisher(int publisherId, int valueVersion)
    {
        IDelayPublisherInternal? pub = null;
        lock (_lock)
        {
            if (publisherId >= 0 && publisherId < _publishers.Count)
            {
                pub = _publishers[publisherId];
            }
        }
        pub?.TryExpire(valueVersion);
    }

    public void NotifyPublished(int publisherId, int contractId)
    {
        if (contractId == 0) return;
        
        var key = new DelayContractKey(0, contractId);
        lock (_lock)
        {
            if (_contractToActivePublisher.TryGetValue(key, out int activeId))
            {
                if (activeId != publisherId)
                {
                    _publishers[activeId].ClearValue();
                }
            }
            _contractToActivePublisher[key] = publisherId;
        }
    }

    public void Update(float deltaTime) => Tick(deltaTime);

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var pub in _publishers) pub.ClearValue();
            _contractToActivePublisher.Clear();
        }
    }
}
