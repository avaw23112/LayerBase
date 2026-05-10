using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

internal sealed class EventColumn<TActor, TEvent> :
    ActorEventColumnRuntime
    where TActor : class, IActor
    where TEvent : struct
{
    private readonly ActorWorld _world;
    private readonly TypedActorStorage<TActor> _owner;
    private readonly ActorBehaviourInvoker<TActor, TEvent> _invoker;
    private readonly EventMailPool<TEvent> _mailPool;
    private readonly DirtySlotList _dirtySlots;
    private readonly ActorMailOptions _options;
    private readonly ActorMailWriteMode _writeMode;
    private readonly ActorSlotFlags _postRejectMask;
    private readonly bool _rejectDisabled;
    private readonly int _bucketIndex;
    private readonly BehaviourType _behaviourType;
    private EventMail<TEvent>[] _mails;

    internal EventMail<TEvent>[] Mails => _mails;
    internal DirtySlotList DirtySlots => _dirtySlots;
    internal int BucketIndex => _bucketIndex;
    internal BehaviourType BehaviourType => _behaviourType;
    internal ActorMailOptions Options => _options;

    public EventColumn(
        ActorWorld world,
        TypedActorStorage<TActor> owner,
        ActorBehaviourInvoker<TActor, TEvent> invoker,
        EventMailPool<TEvent> mailPool,
        ActorMailOptions options,
        BehaviourType behaviourType,
        int bucketIndex,
        int initialSlotCapacity)
    {
        _world = world;
        _owner = owner;
        _invoker = invoker;
        _options = options;
        _behaviourType = behaviourType;
        _bucketIndex = bucketIndex;
        _mails = new EventMail<TEvent>[Math.Max(initialSlotCapacity, 1)];
        _mailPool = mailPool;
        _dirtySlots = new DirtySlotList(initialSlotCapacity);
        _writeMode = ResolveWriteMode(options);
        _postRejectMask = ActorSlotFlags.PendingDestroy | ActorSlotFlags.Destroying;
        _rejectDisabled = options.DisabledPolicy == ActorMailDisabledPolicy.Reject;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PostResult Post(
        int slotIndex,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
    {
        if (postPolicy == null &&
            fullPolicy == null &&
            _writeMode == ActorMailWriteMode.QueuedGrow)
        {
            return PostQueuedGrowFast(slotIndex, in value);
        }

        return PostGeneral(slotIndex, in value, postPolicy, fullPolicy);
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
            if (!EventMailReader.TryDequeue(ref mail, _mailPool, out TEvent value))
            {
                _dirtySlots.Pop();
                EventMailReader.ReleaseIfEmpty(ref mail, _mailPool, _options);
                continue;
            }

            if (!_owner.IsAliveSlot(slotIndex))
            {
                _dirtySlots.Pop();
                EventMailReader.ReleaseIfEmpty(ref mail, _mailPool, _options);
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
                EventMailReader.ReleaseIfEmpty(ref mail, _mailPool, _options);
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
                EventMailReader.ReleaseIfEmpty(ref mail, _mailPool, _options);
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
            if (!EventMailReader.TryDequeue(ref mail, _mailPool, out TEvent value))
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        _world.InvalidateAllFastCaches<TEvent>();
    }

    public override void ClearMail(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)_mails.Length)
        {
            return;
        }

        ref EventMail<TEvent> mail = ref _mails[slotIndex];
        EventMailReader.ForceRelease(ref mail, _mailPool);
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
            if (!_owner.CanPostFast(slotIndex, _postRejectMask, _rejectDisabled))
            {
                continue;
            }

            _ = _world.PostQueuedGrowFastNoResult(
                slotIndex,
                in value,
                _mails,
                _dirtySlots,
                _bucketIndex,
                _mailPool);
        }
    }

    internal bool CanUseDefaultPostFastPath()
    {
        return _writeMode == ActorMailWriteMode.QueuedGrow;
    }

    internal bool SupportsFastCacheBinding()
    {
        return _behaviourType != BehaviourType.Cold
               && _writeMode == ActorMailWriteMode.QueuedGrow;
    }

    internal PostResult PostQueuedFast(int slotIndex, in TEvent value)
    {
        return PostQueuedGrowFast(slotIndex, in value);
    }

    internal bool PostQueuedFastNoResult(int slotIndex, in TEvent value)
    {
        return _world.PostQueuedGrowFastNoResult(
            slotIndex,
            in value,
            _mails,
            _dirtySlots,
            _bucketIndex,
            _mailPool);
    }

    private bool CanUsePumpFastPath(in ActorMailPumpOptions options)
    {
        return options.MaxMailsPerActorPerPump <= 0
               && !_options.ReleaseWhenEmpty;
    }

    private PostResult PostGeneral(
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
        int previousCount = mail.Count;
        PostResult result = EventMailWriter.Enqueue(
            ref mail,
            in value,
            _mailPool,
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult PostQueuedGrowFast(int slotIndex, in TEvent value)
    {
        if (!_owner.CanPostFast(slotIndex, _postRejectMask, _rejectDisabled))
        {
            return PostGeneral(slotIndex, in value, postPolicy: null, fullPolicy: null);
        }

        EnsureSlotCapacity(slotIndex);
        ref EventMail<TEvent> mail = ref _mails[slotIndex];
        EnsureMailAllocatedFast(ref mail);

        if (mail.Count >= mail.Capacity)
        {
            if (!TryGrowQueuedFast(ref mail))
            {
                return PostResult.Failure(
                    ActorPostStatus.MailFullRejected,
                    "Actor mail reached max capacity.",
                    PostFailureKind.MailboxFull);
            }
        }

        EnqueueFast(ref mail, in value, slotIndex);
        return PostResult.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureMailAllocatedFast(ref EventMail<TEvent> mail)
    {
        if (mail.BufferId != 0)
        {
            return;
        }

        mail.BufferId = _mailPool.Rent(_options.InitialCapacity);
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 0;
        mail.Capacity = _mailPool.GetCapacity(mail.BufferId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnqueueFast(
        ref EventMail<TEvent> mail,
        in TEvent value,
        int slotIndex)
    {
        _mailPool.Write(mail.BufferId, mail.Tail, in value);
        mail.Tail++;
        if (mail.Tail == mail.Capacity)
        {
            mail.Tail = 0;
        }

        mail.Count++;
        if (mail.Count == 1)
        {
            _dirtySlots.Mark(slotIndex);
            NotifyBucketDirty();
        }
    }

    private bool TryGrowQueuedFast(ref EventMail<TEvent> mail)
    {
        if (mail.Capacity >= _options.MaxCapacity)
        {
            return false;
        }

        int growFactor = Math.Max(_options.GrowFactor, 2);
        int nextCapacity = mail.Capacity * growFactor;
        if (nextCapacity <= mail.Capacity)
        {
            nextCapacity = mail.Capacity + 1;
        }

        nextCapacity = Math.Min(nextCapacity, _options.MaxCapacity);
        if (nextCapacity <= mail.Capacity)
        {
            return false;
        }

        _mailPool.Resize(mail.BufferId, mail.Head, mail.Count, nextCapacity);
        mail.Head = 0;
        mail.Tail = mail.Count;
        mail.Capacity = nextCapacity;
        return true;
    }

    private static ActorMailWriteMode ResolveWriteMode(in ActorMailOptions options)
    {
        if (options.PostPolicy == ActorPostPolicy.Queued &&
            options.FullPolicy == ActorMailFullPolicy.Grow &&
            options.GrowFailurePolicy == ActorMailFullPolicy.RejectNew)
        {
            return ActorMailWriteMode.QueuedGrow;
        }

        if (options.PostPolicy == ActorPostPolicy.Latest)
        {
            return ActorMailWriteMode.Latest;
        }

        if (options.PostPolicy == ActorPostPolicy.Dirty)
        {
            return ActorMailWriteMode.Dirty;
        }

        if (options.PostPolicy == ActorPostPolicy.Coalesced)
        {
            return ActorMailWriteMode.Coalesced;
        }

        return ActorMailWriteMode.General;
    }
}
