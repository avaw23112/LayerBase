namespace LayerBase.Core.Event;

public interface ITimerAction
{
    bool Execute(PostScheduler scheduler);
}

internal sealed class PostEventAction<TEvent> : ITimerAction where TEvent : struct
{
    public TEvent Event;
    public PostTypePlan? Plan;

    public PostEventAction(TEvent @event, PostTypePlan? plan = null)
    {
        Event = @event;
        Plan = plan;
    }

    public bool Execute(PostScheduler scheduler)
    {
        return Plan.HasValue
            ? scheduler.TryPostWithPolicy(Event, Plan.Value).IsSuccess
            : scheduler.TryPost(Event).IsSuccess;
    }
}