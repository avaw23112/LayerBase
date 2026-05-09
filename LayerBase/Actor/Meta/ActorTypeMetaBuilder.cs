using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed class ActorTypeMetaBuilder
{
    private readonly List<ActorBehaviourEntry> _entries = new();
    private readonly HashSet<int> _eventIds = new();
    private readonly HashSet<int> _tagIds = new();
    private readonly HashSet<int> _groupIds = new();

    public void AddBehaviour<TActor, TEvent>(ActorBehaviourInvoker<TActor, TEvent> invoker)
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
            invoker));
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
            tagIds,
            groupIds);
    }
}
