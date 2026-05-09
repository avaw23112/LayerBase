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
        ActorSlotState slotState = _owner.GetSlotState(slotIndex);
        if (slotState == ActorSlotState.PendingDestroy)
        {
            return PostResult.Failure(
                ActorPostStatus.ActorPendingDestroy,
                "Actor is pending destroy.",
                PostFailureKind.PendingDestroy);
        }

        if (slotState == ActorSlotState.Destroying)
        {
            return PostResult.Failure(
                ActorPostStatus.ActorNotAlive,
                "Actor is destroying.",
                PostFailureKind.Destroying);
        }

        if (_options.DisabledPolicy == ActorMailDisabledPolicy.Reject
            && !_owner.IsSlotEnabled(slotIndex))
        {
            return PostResult.Failure(
                ActorPostStatus.ActorDisabledRejected,
                "Actor is disabled.",
                PostFailureKind.DisabledActor);
        }

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

    public ActorColumnPumpResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats)
    {
        while (_dirtySlots.TryPeek(out int slotIndex))
        {
            ref EventMail<TEvent> mail = ref _mails[slotIndex];
            if (!EventMailReader.TryDequeue(ref mail, _bufferPool, out TEvent value))
            {
                _dirtySlots.Pop();
                EventMailReader.ReleaseIfEmpty(ref mail, _bufferPool, _options);
                continue;
            }

            if (!_owner.IsAliveSlot(slotIndex))
            {
                _dirtySlots.Pop();
                EventMailReader.ReleaseIfEmpty(ref mail, _bufferPool, _options);
                continue;
            }

            long actorKey = _owner.GetActorPumpKey(slotIndex);
            if (!stats.CanProcessActor(actorKey, options))
            {
                _dirtySlots.MoveHeadToTail();
                stats.ActorLimitHits++;
                return ActorColumnPumpResult.ActorLimited;
            }

            TActor? actor = _owner.Actors[slotIndex];
            if (actor == null)
            {
                _dirtySlots.Pop();
                EventMailReader.ReleaseIfEmpty(ref mail, _bufferPool, _options);
                continue;
            }

            _invoker(actor, in value);
            budget.ConsumeEvent();
            stats.RecordActorProcessed(actorKey);

            if (mail.Count == 0)
            {
                _dirtySlots.Pop();
                EventMailReader.ReleaseIfEmpty(ref mail, _bufferPool, _options);
            }
            else
            {
                _dirtySlots.MoveHeadToTail();
            }

            return ActorColumnPumpResult.Processed;
        }

        return ActorColumnPumpResult.NoWork;
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

    public override void ClearMail(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_mails.Length)
        {
            return;
        }

        ref EventMail<TEvent> mail = ref _mails[slotIndex];
        EventMailReader.ForceRelease(ref mail, _bufferPool);
    }

    public override int GetPendingCount(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_mails.Length)
        {
            return 0;
        }

        return _mails[slotIndex].Count;
    }

    public override int GetTotalPendingCount()
    {
        int count = 0;
        for (int i = 0; i < _mails.Length; i++)
        {
            count += _mails[i].Count;
        }

        return count;
    }

    public bool HasPendingWork()
    {
        return _dirtySlots.Count > 0;
    }
}
