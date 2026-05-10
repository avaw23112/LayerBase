namespace LayerBase.Actor;

internal sealed class EventPostState<TEvent>
    where TEvent : struct
{
    public readonly ActorPostRouteKind Route;
    public readonly EventMailPool<TEvent> Pool;
    public readonly ActorMailOptions Options;
    public readonly ActorSlotFlags RejectMask;
    public readonly bool RejectDisabled;
    public EventPostRow<TEvent>[] RowsByArchetype;

    public EventPostState(
        ActorPostRouteKind route,
        EventMailPool<TEvent> pool,
        ActorMailOptions options,
        ActorSlotFlags rejectMask,
        bool rejectDisabled,
        EventPostRow<TEvent>[] rowsByArchetype)
    {
        Route = route;
        Pool = pool;
        Options = options;
        RejectMask = rejectMask;
        RejectDisabled = rejectDisabled;
        RowsByArchetype = rowsByArchetype;
    }
}
