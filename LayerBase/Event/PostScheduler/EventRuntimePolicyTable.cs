using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Core.Event;

public sealed class EventRuntimePolicyTable
{
    private EventPostPolicy[] _postPolicies = new EventPostPolicy[64];
    private EventTimerPolicy?[] _timerPolicies = new EventTimerPolicy?[64];
    private EventBufferPolicy?[] _bufferPolicies = new EventBufferPolicy?[64];
    private ActorMailOptions?[] _actorMailOptionsByEventId = new ActorMailOptions?[64];
    private IEventMetaData?[] _metaDatas = new IEventMetaData?[64];
    private readonly object _lock = new();
    private readonly BackpressurePolicy _defaultBackpressure;

    public EventRuntimePolicyTable(BackpressurePolicy defaultBackpressure = BackpressurePolicy.RejectNew)
    {
        _defaultBackpressure = defaultBackpressure;
        for (int i = 0; i < _postPolicies.Length; i++)
        {
            _postPolicies[i] = new EventPostPolicy(PostDeliveryMode.Normal, _defaultBackpressure, 0);
        }
    }

    public void SetMetaData(int eventTypeId, IEventMetaData metaData)
    {
        EnsureMetaDataCapacity(eventTypeId);
        _metaDatas[eventTypeId] = metaData;
    }

    public EventMetaData<T>? GetMetaData<T>(int eventTypeId) where T : struct
    {
        var metas = _metaDatas;
        if (eventTypeId < 0 || eventTypeId >= metas.Length) return null;
        return metas[eventTypeId] as EventMetaData<T>;
    }

    public void SetPostPolicy(int eventTypeId, EventPostPolicy policy)
    {
        if (eventTypeId >= _postPolicies.Length)
        {
            int oldSize = _postPolicies.Length;
            int newSize = Math.Max(eventTypeId + 1, oldSize * 2);
            Array.Resize(ref _postPolicies, newSize);
            for (int i = oldSize; i < newSize; i++)
            {
                _postPolicies[i] = new EventPostPolicy(PostDeliveryMode.Normal, _defaultBackpressure, 0);
            }
        }
        _postPolicies[eventTypeId] = policy;
    }

    public void SetTimerPolicy(int eventTypeId, EventTimerPolicy policy)
    {
        if (eventTypeId >= _timerPolicies.Length)
        {
            int oldSize = _timerPolicies.Length;
            int newSize = Math.Max(eventTypeId + 1, oldSize * 2);
            Array.Resize(ref _timerPolicies, newSize);
        }
        _timerPolicies[eventTypeId] = policy;
    }

    public void SetBufferPolicy(int eventTypeId, EventBufferPolicy policy)
    {
        if (eventTypeId >= _bufferPolicies.Length)
        {
            int oldSize = _bufferPolicies.Length;
            int newSize = Math.Max(eventTypeId + 1, oldSize * 2);
            Array.Resize(ref _bufferPolicies, newSize);
        }
        _bufferPolicies[eventTypeId] = policy;
    }

    public void SetActorMailOptions(int eventTypeId, ActorMailOptions options)
    {
        EnsureActorMailCapacity(eventTypeId);
        _actorMailOptionsByEventId[eventTypeId] = options;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventPostPolicy GetPostPolicy(int eventTypeId)
    {
        var policies = _postPolicies;
        if (eventTypeId < 0 || eventTypeId >= policies.Length)
            return new EventPostPolicy(PostDeliveryMode.Normal, _defaultBackpressure, 0);
        
        return policies[eventTypeId];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventTimerPolicy? GetTimerPolicy(int eventTypeId)
    {
        var policies = _timerPolicies;
        if (eventTypeId < 0 || eventTypeId >= policies.Length)
            return null;
        
        return policies[eventTypeId];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventBufferPolicy? GetBufferPolicy(int eventTypeId)
    {
        var policies = _bufferPolicies;
        if (eventTypeId < 0 || eventTypeId >= policies.Length)
            return null;
        
        return policies[eventTypeId];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ActorMailOptions GetActorMailOptions(int eventTypeId)
    {
        var options = _actorMailOptionsByEventId;
        if (eventTypeId < 0 || eventTypeId >= options.Length)
        {
            return ActorMailOptions.Default;
        }

        return options[eventTypeId] ?? ActorMailOptions.Default;
    }

    public IEnumerable<EventPolicySnapshot> ExportSnapshots()
    {
        IEventMetaData?[] metas= _metaDatas.ToArray();
        EventPostPolicy[] postPolicies= _postPolicies.ToArray();
        EventTimerPolicy?[] timerPolicies= _timerPolicies.ToArray();
        EventBufferPolicy?[] bufferPolicies = _bufferPolicies.ToArray();

        for (int i = 0; i < metas.Length; i++)
        {
            var meta = metas[i];
            if (meta == null) continue;

            yield return new EventPolicySnapshot(
                runtimeId: i,
                identity: meta.GetIdentity(),
                postPolicy: i < postPolicies.Length ? postPolicies[i] : null,
                timerPolicy: i < timerPolicies.Length ? timerPolicies[i] : null,
                bufferPolicy: i < bufferPolicies.Length ? bufferPolicies[i] : null
            );
        }
    }

    private void EnsureMetaDataCapacity(int eventTypeId)
    {
        if (eventTypeId < _metaDatas.Length)
        {
            return;
        }

        Array.Resize(ref _metaDatas, Math.Max(eventTypeId + 1, _metaDatas.Length * 2));
    }

    private void EnsureActorMailCapacity(int eventTypeId)
    {
        if ((uint)eventTypeId < (uint)_actorMailOptionsByEventId.Length)
        {
            return;
        }

        int newSize = _actorMailOptionsByEventId.Length == 0 ? 8 : _actorMailOptionsByEventId.Length;
        while (newSize <= eventTypeId)
        {
            newSize *= 2;
        }

        Array.Resize(ref _actorMailOptionsByEventId, newSize);
    }
}
