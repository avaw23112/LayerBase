using LayerBase.Core.Event;

namespace LayerBase.Actor;

internal sealed class EventColumn<TActor, TEvent> :
    ActorEventColumnRuntime
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
        _dirtySlots = new DirtySlotList(initialSlotCapacity);
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
        ref EventMail<TEvent> mail = ref _mails[slotIndex];
        if (CanUseQueuedFastPath(postPolicy, fullPolicy))
        {
            return PostQueuedFast(ref mail, slotIndex, in value);
        }

        int previousCount = mail.Count;
        PostResult result = EventMailWriter.Enqueue(
            ref mail,
            in value,
            _bufferPool,
            _dirtySlots,
            slotIndex,
            _options,
            postPolicy,
            fullPolicy);

        if (previousCount == 0 && result.IsSuccess && result.CountsAsPending && mail.Count > 0)
        {
            NotifyBucketDirty();
        }

        return result;
    }

    public override ActorColumnPumpResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats)
    {
        if (CanUsePumpFastPath(options))
        {
            return PumpOneFast(ref budget);
        }

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
            if (options.MaxMailsPerActorPerPump > 0)
            {
                stats.RecordActorProcessed(actorKey);
            }

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

    public ActorColumnPumpResult PumpOneFast(ref RuntimeFrameBudget budget)
    {
        while (_dirtySlots.TryPeek(out int slotIndex))
        {
            ref EventMail<TEvent> mail = ref _mails[slotIndex];
            if (!EventMailReader.TryDequeue(ref mail, _bufferPool, out TEvent value))
            {
                _dirtySlots.Pop();
                continue;
            }

            if (!_owner.IsAliveSlot(slotIndex))
            {
                _dirtySlots.Pop();
                continue;
            }

            TActor? actor = _owner.Actors[slotIndex];
            if (actor == null)
            {
                _dirtySlots.Pop();
                continue;
            }

            _invoker(actor, in value);
            budget.ConsumeEvent();

            if (mail.Count == 0)
            {
                _dirtySlots.Pop();
            }
            else
            {
                _dirtySlots.MoveHeadToTail();
            }

            return ActorColumnPumpResult.Processed;
        }

        return ActorColumnPumpResult.NoWork;
    }

    public DispatchResult DispatchNow(
        TActor actor,
        in TEvent value)
    {
        try
        {
            _invoker(actor, in value);
            return DispatchResult.Success();
        }
        catch (Exception exception)
        {
            return DispatchResult.Failure(
                DispatchFailureKind.HandlerException,
                $"Actor behaviour '{typeof(TEvent).Name}' threw during DispatchNow.",
                exception);
        }
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

    public override bool HasPendingWork()
    {
        return _dirtySlots.Count > 0;
    }

    internal void PostToAliveSlotsFast(
        TActor?[] actors,
        ActorSlotState[] states,
        bool[] enabled,
        int maxSlot,
        in TEvent value)
    {
        for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
        {
            if (actors[slotIndex] == null || states[slotIndex] != ActorSlotState.Alive)
            {
                continue;
            }

            if (_options.DisabledPolicy == ActorMailDisabledPolicy.Reject && !enabled[slotIndex])
            {
                continue;
            }

            ref EventMail<TEvent> mail = ref _mails[slotIndex];
            _ = PostQueuedFast(ref mail, slotIndex, in value);
        }
    }

    internal bool CanUseDefaultPostFastPath()
    {
        return _options.PostPolicy == ActorPostPolicy.Queued
               && _options.FullPolicy == ActorMailFullPolicy.Grow
               && _options.GrowFailurePolicy == ActorMailFullPolicy.RejectNew;
    }

    private bool CanUsePumpFastPath(in ActorMailPumpOptions options)
    {
        return options.MaxMailsPerActorPerPump <= 0
               && !_options.ReleaseWhenEmpty;
    }

    private bool CanUseQueuedFastPath(
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
    {
        return (postPolicy ?? _options.PostPolicy) == ActorPostPolicy.Queued
               && (fullPolicy ?? _options.FullPolicy) == ActorMailFullPolicy.Grow
               && _options.GrowFailurePolicy == ActorMailFullPolicy.RejectNew;
    }

    private PostResult PostQueuedFast(
        ref EventMail<TEvent> mail,
        int slotIndex,
        in TEvent value)
    {
        if (mail.BufferId == 0)
        {
            mail.BufferId = _bufferPool.Rent(_options.InitialCapacity);
            mail.Head = 0;
            mail.Count = 0;
            mail.Capacity = _bufferPool.GetCapacity(mail.BufferId);
        }

        if (mail.Count >= mail.Capacity && !TryGrowFast(ref mail))
        {
            return PostResult.Failure(
                ActorPostStatus.MailFullRejected,
                "Actor mail reached max capacity.",
                PostFailureKind.MailboxFull);
        }

        int tail = ActorMailCapacity.Wrap(mail.Head + mail.Count, mail.Capacity);
        mail.Count++;
        _bufferPool.Write(mail.BufferId, tail, in value);

        if (mail.Count == 1)
        {
            _dirtySlots.AddIfNotExists(slotIndex);
            NotifyBucketDirty();
        }

        return PostResult.Success;
    }

    private bool TryGrowFast(ref EventMail<TEvent> mail)
    {
        if (mail.Capacity >= _options.MaxCapacity)
        {
            return false;
        }

        int nextCapacity = ActorMailCapacity.NormalizePowerOfTwo(
            mail.Capacity * Math.Max(_options.GrowFactor, 2));
        if (nextCapacity > _options.MaxCapacity)
        {
            nextCapacity = _options.MaxCapacity;
        }

        if (nextCapacity <= mail.Capacity)
        {
            return false;
        }

        _bufferPool.Resize(mail.BufferId, mail.Head, mail.Count, nextCapacity);
        mail.Head = 0;
        mail.Capacity = nextCapacity;
        return true;
    }
}
