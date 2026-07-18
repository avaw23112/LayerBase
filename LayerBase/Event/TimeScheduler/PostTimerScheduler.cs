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
    private TimerPostTypePlan[] _timerPlans = Array.Empty<TimerPostTypePlan>();

    internal int PendingCount => _timer.PendingCount;

    public PostTimerScheduler(
        int runtimeId,
        TimeSchedulerOptions options,
        EventPayloadStorage payloadStorage,
        PostScheduler postScheduler)
    {
        _runtimeId = runtimeId;
        _payloadStorage = payloadStorage;
        _postScheduler = postScheduler;
        _timer = new TimeScheduler<PostTimerPayload>(options, payloadReleaser: this);
    }

    public void CompilePlans(EventBuildPolicyTable policyTable, int maxEventTypeId)
    {
        if (maxEventTypeId < 0)
        {
            _timerPlans = Array.Empty<TimerPostTypePlan>();
            return;
        }

        var plans = new TimerPostTypePlan[maxEventTypeId + 1];

        for (int eventId = 0; eventId <= maxEventTypeId; eventId++)
        {
            EventTimerPolicy? timerPolicy = policyTable.GetTimerPolicy(eventId);

            bool hasOverride = false;
            PostTypePlan expiredPlan = default;

            if (timerPolicy?.ExpiredPostPolicy is { } expiredPolicy)
            {
                EventPostPolicyRules.Validate(
                    in expiredPolicy,
                    $"EventTypeId={eventId}.TimerPolicy.ExpiredPostPolicy");

                expiredPlan = PostTypePlan.FromPolicy(
                    eventId,
                    in expiredPolicy,
                    _postScheduler.Options.DefaultBackpressure);

                hasOverride = true;
            }

            plans[eventId] = new TimerPostTypePlan(
                eventId,
                hasOverride,
                expiredPlan,
                timerPolicy?.RepeatMode,
                timerPolicy?.CatchUpPolicy);
        }

        _timerPlans = plans;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerHandle Schedule<TEvent>(
        in TEvent value,
        float delaySeconds,
        int repeatCount = 0,
        float intervalSeconds = 0)
        where TEvent : struct
    {
        int eventId = EventTypeId<TEvent>.Id;
        PayloadHandle payloadHandle = _payloadStorage.Store(_runtimeId, in value);

        TimerPostTypePlan plan = _timerPlans[eventId];
        PostTypePlan expiredPlan = plan.ExpiredPlan;

        var payload = new PostTimerPayload(
            payloadHandle,
            in expiredPlan,
            plan.HasExpiredOverride);

        try
        {
            TimerHandle handle = _timer.Schedule(
                in payload,
                delaySeconds,
                repeatCount,
                intervalSeconds,
                plan.RepeatMode,
                plan.CatchUpPolicy);

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
