namespace LayerBase.Core.Event;

public struct PostTimerSink<TEvent> : IExpiredTimerSink<PostTimerPayload<TEvent>> where TEvent : struct
{
    private readonly PostScheduler _postScheduler;

    public PostTimerSink(PostScheduler postScheduler)
    {
        _postScheduler = postScheduler;
    }

    public bool TryAcceptExpired(in PostTimerPayload<TEvent> payload, TimerHandle handle)
    {
        return _postScheduler.TryPost(payload.Event, payload.PostPolicyOverride).IsSuccess;
    }
}
