namespace LayerBase.Actor;

internal sealed class EventPostState<TEvent>
    where TEvent : struct
{
    public readonly byte RouteCode;
    public readonly EventMailPool<TEvent> Pool;
    public readonly ActorMailOptions Options;
    public EventPostRow<TEvent>[] RowsByArchetype;

    public EventPostState(
        byte routeCode,
        EventMailPool<TEvent> pool,
        ActorMailOptions options,
        EventPostRow<TEvent>[] rowsByArchetype)
    {
        RouteCode = routeCode;
        Pool = pool;
        Options = options;
        RowsByArchetype = rowsByArchetype;
    }
}
