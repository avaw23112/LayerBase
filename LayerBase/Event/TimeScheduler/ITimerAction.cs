namespace LayerBase.Core.Event;

public interface ITimerAction
{
    bool Execute(PostScheduler scheduler);
}

internal sealed class PostEventAction<TEvent> : ITimerAction where TEvent : struct
{
    public TEvent Event;
    public EventPostPolicy? PolicyOverride;

    public PostEventAction(TEvent @event, EventPostPolicy? policyOverride = null)
    {
        Event = @event;
        PolicyOverride = policyOverride;
    }

    public bool Execute(PostScheduler scheduler)
    {
        return scheduler.TryPost(Event, PolicyOverride).IsSuccess;
    }
}