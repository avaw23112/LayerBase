using LayerBase.Core.Event;

namespace LayerBase.Actor;

internal sealed class EventColumn<TActor, TEvent> :
    ActorEventColumnRuntime,
    IActorEventColumn<TEvent>
    where TActor : class, IActor
    where TEvent : struct
{
    private readonly TypedActorStorage<TActor> _owner;
    private readonly ActorBehaviourInvoker<TActor, TEvent> _invoker;
    private readonly RingQueueBuffer<TEvent> _bufferPool;
    private readonly DirtySlotList _dirtySlots;
    private readonly ActorMailOptions _options;
    private EventMail<TEvent>[] _mails;

    public EventColumn(
        TypedActorStorage<TActor> owner,
        ActorBehaviourInvoker<TActor, TEvent> invoker,
        ActorMailOptions options,
        int initialSlotCapacity)
    {
        _owner = owner;
        _invoker = invoker;
        _options = options;
        _mails = new EventMail<TEvent>[Math.Max(initialSlotCapacity, 1)];
        _bufferPool = new RingQueueBuffer<TEvent>();
        _dirtySlots = new DirtySlotList();
    }

    public PostResult Post(
        int slotIndex,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
    {
        EnsureSlotCapacity(slotIndex);
        return EventMailWriter.Enqueue(
            ref _mails[slotIndex],
            in value,
            _bufferPool,
            _dirtySlots,
            slotIndex,
            _options,
            postPolicy,
            fullPolicy);
    }

    public bool PumpOne(ref RuntimeFrameBudget budget)
    {
        if (!_dirtySlots.TryPeek(out int slotIndex))
        {
            return false;
        }

        ref EventMail<TEvent> mail = ref _mails[slotIndex];
        if (!EventMailReader.TryDequeue(ref mail, _bufferPool, out TEvent value))
        {
            _dirtySlots.Pop();
            EventMailReader.ReleaseIfEmpty(ref mail, _bufferPool, _options);
            return false;
        }

        TActor? actor = _owner.Actors[slotIndex];
        if (actor == null)
        {
            _dirtySlots.Pop();
            EventMailReader.ReleaseIfEmpty(ref mail, _bufferPool, _options);
            return false;
        }

        _invoker(actor, in value);
        budget.ConsumeEvent();

        if (mail.Count == 0)
        {
            _dirtySlots.Pop();
            EventMailReader.ReleaseIfEmpty(ref mail, _bufferPool, _options);
        }

        return true;
    }

    public override void EnsureSlotCapacity(int slotIndex)
    {
        if ((uint)slotIndex < (uint)_mails.Length)
        {
            return;
        }

        int newSize = _mails.Length == 0 ? 4 : _mails.Length;
        while (newSize <= slotIndex)
        {
            newSize *= 2;
        }

        Array.Resize(ref _mails, newSize);
    }
}
