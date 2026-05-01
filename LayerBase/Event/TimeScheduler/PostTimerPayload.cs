namespace LayerBase.Core.Event;

public readonly struct PostTimerPayload<TEvent> where TEvent : struct
{
    public readonly TEvent Event;
    public readonly EventPostPolicy? PostPolicyOverride;

    public PostTimerPayload(TEvent eventValue, EventPostPolicy? postPolicyOverride = null)
    {
        Event = eventValue;
        PostPolicyOverride = postPolicyOverride;
    }
}
