using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed class ActorTypeMetaBuilder
{
    private readonly List<ActorBehaviourEntry> _entries = new();
    private readonly List<ActorCallEntry> _callEntries = new();
    private readonly HashSet<int> _eventIds = new();
    private readonly HashSet<int> _callRouteIds = new();
    private readonly HashSet<int> _tagIds = new();
    private readonly HashSet<int> _groupIds = new();

    public void AddBehaviour<TActor, TEvent>(
        ActorBehaviourInvoker<TActor, TEvent> invoker)
        where TActor : class, IActor
        where TEvent : struct
    {
        if (invoker == null)
        {
            throw new ArgumentNullException(nameof(invoker));
        }

        int eventTypeId = EventTypeId<TEvent>.Id;
        if (!_eventIds.Add(eventTypeId))
        {
            throw new InvalidOperationException(
                $"Actor type {typeof(TActor).Name} already has behaviour for event {typeof(TEvent).Name}.");
        }

        _entries.Add(new ActorBehaviourEntry(
            eventTypeId,
            typeof(TEvent),
            invoker,
            static (storage, rawInvoker, world) =>
            {
                var typedStorage = (TypedActorStorage<TActor>)storage;
                var typedInvoker = (ActorBehaviourInvoker<TActor, TEvent>)rawInvoker;
                return typedStorage.BuildColumnDirect(world, typedInvoker);
            }));
    }

    public void AddCallBehaviour<TActor, TRequest, TResponse>(
        ActorCallInvoker<TActor, TRequest, TResponse> invoker)
        where TActor : class, IActor
        where TRequest : struct
        where TResponse : struct
    {
        if (invoker == null)
        {
            throw new ArgumentNullException(nameof(invoker));
        }

        int routeId = ActorCallRouteId<TRequest, TResponse>.Id;
        if (!_callRouteIds.Add(routeId))
        {
            throw new InvalidOperationException(
                $"Actor type {typeof(TActor).Name} already has call behaviour for request {typeof(TRequest).Name} and response {typeof(TResponse).Name}.");
        }

        _callEntries.Add(new ActorCallEntry(
            routeId,
            typeof(TRequest),
            typeof(TResponse),
            invoker,
            static (storage, rawInvoker, world) =>
            {
                var typedStorage = (TypedActorStorage<TActor>)storage;
                var typedInvoker = (ActorCallInvoker<TActor, TRequest, TResponse>)rawInvoker;
                return typedStorage.BuildCallColumnDirect(world, typedInvoker);
            }));
    }

    public void AddTag<TTag>()
        where TTag : struct, IActorTag
    {
        _tagIds.Add(ActorTagId<TTag>.Id);
    }

    public void AddGroup<TGroup>()
        where TGroup : struct, IActorGroup
    {
        _groupIds.Add(ActorGroupId<TGroup>.Id);
    }

    internal ActorTypeMeta<TActor> Build<TActor>()
        where TActor : class, IActor
    {
        ActorBehaviourEntry[] entries = _entries
            .OrderBy(static entry => entry.EventTypeId)
            .ToArray();

        ActorCallEntry[] callEntries = _callEntries
            .OrderBy(static entry => entry.RouteId)
            .ToArray();

        int[] eventTypeIds = entries
            .Select(static entry => entry.EventTypeId)
            .ToArray();

        int[] tagIds = _tagIds
            .OrderBy(static id => id)
            .ToArray();

        int[] groupIds = _groupIds
            .OrderBy(static id => id)
            .ToArray();

        return new ActorTypeMeta<TActor>(
            new BehaviourSignature(eventTypeIds),
            entries,
            callEntries,
            tagIds,
            groupIds);
    }
}
