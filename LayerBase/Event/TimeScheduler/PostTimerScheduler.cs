using System;
using System.Runtime.CompilerServices;

namespace LayerBase.Core.Event;

internal sealed class PostTimerScheduler : IDisposable,
    IExpiredTimerSink<PostTimerPayload>,
    ITimerPayloadReleaser<PostTimerPayload>
{
    private readonly int _runtimeId;
    private readonly TimeScheduler<PostTimerPayload> _timer;
    private readonly EventPayloadStorage _payloadStorage;
    private readonly PostScheduler _postScheduler;
    private EventBuildPolicyTable _policyTable;

    internal int PendingCount => _timer.PendingCount;

    public PostTimerScheduler(
        int runtimeId,
        TimeSchedulerOptions options,
        EventPayloadStorage payloadStorage,
        PostScheduler postScheduler,
        EventBuildPolicyTable policyTable)
    {
        _runtimeId = runtimeId;
        _payloadStorage = payloadStorage;
        _postScheduler = postScheduler;
        _policyTable = policyTable;
        _timer = new TimeScheduler<PostTimerPayload>(options, payloadReleaser: this);
    }

    public TimerHandle Schedule<TEvent>(
        in TEvent value,
        float delaySeconds,
        int repeatCount = 0,
        float intervalSeconds = 0)
        where TEvent : struct
    {
        int eventId = EventTypeId<TEvent>.Id;
        PayloadHandle payloadHandle = _payloadStorage.Store(_runtimeId, in value);

        bool hasOverride = false;
        PostTypePlan overridePlan = default;

        EventTimerPolicy? timerPolicy = _policyTable.GetTimerPolicy(eventId);

        if (timerPolicy?.ExpiredPostPolicy is { } expiredPolicy)
        {
            EventPostPolicyRules.Validate(
                in expiredPolicy,
                $"{typeof(TEvent).FullName}.TimerPolicy.ExpiredPostPolicy");

            overridePlan = PostTypePlan.FromPolicy(
                eventId,
                in expiredPolicy,
                _postScheduler.Options.DefaultBackpressure);

            hasOverride = true;
        }

        var payload = new PostTimerPayload(
            payloadHandle,
            in overridePlan,
            hasOverride);

        try
        {
            TimerRepeatMode? repeatMode = timerPolicy?.RepeatMode;
            TimerCatchUpPolicy? catchUpPolicy = timerPolicy?.CatchUpPolicy;

            TimerHandle handle = _timer.Schedule(
                in payload,
                delaySeconds,
                repeatCount,
                intervalSeconds,
                repeatMode,
                catchUpPolicy);

            if (handle.IsInvalid)
                _payloadStorage.Release(payloadHandle);

            return handle;
        }
        catch
        {
            _payloadStorage.Release(payloadHandle);
            throw;
        }
    }

    public bool Cancel(TimerHandle handle)
    {
        return _timer.Cancel(handle);
    }

    public void Tick(float deltaTime)
    {
        _timer.Tick(deltaTime, this);
    }

    public void UpdatePolicyTable(EventBuildPolicyTable policyTable)
    {
        _policyTable = policyTable ?? throw new ArgumentNullException(nameof(policyTable));
    }

    public void PrewarmEvent<TEvent>()
        where TEvent : struct
    {
        _payloadStorage.EnsureStore<TEvent>(_runtimeId);
    }

    bool IExpiredTimerSink<PostTimerPayload>.TryAcceptExpired(
        in PostTimerPayload payload,
        TimerHandle handle)
    {
        PostResult result = payload.HasOverridePlan
            ? _payloadStorage.Post(
                payload.PayloadHandle,
                _postScheduler,
                in payload.OverridePlan)
            : _payloadStorage.Post(
                payload.PayloadHandle,
                _postScheduler);

        return result.IsSuccess;
    }

    void ITimerPayloadReleaser<PostTimerPayload>.Release(
        in PostTimerPayload payload)
    {
        _payloadStorage.Release(payload.PayloadHandle);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
