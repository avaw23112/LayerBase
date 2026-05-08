using LayerBase.Core.Event;

namespace LayerBase.Actor;

public sealed class ActorTypeMetaBuilder
{
    private readonly List<ActorBehaviourEntry> _entries = new();
    private readonly HashSet<int> _eventIds = new();

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

    internal ActorTypeMeta<TActor> Build<TActor>()
        where TActor : class, IActor
    {
        ActorBehaviourEntry[] entries = _entries
            .OrderBy(static entry => entry.EventTypeId)
            .ToArray();

        int[] eventTypeIds = entries
            .Select(static entry => entry.EventTypeId)
            .ToArray();

        return new ActorTypeMeta<TActor>(
            new BehaviourSignature(eventTypeIds),
            entries);
    }
}
