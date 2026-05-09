using LayerBase.Async;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

internal sealed class ActorCallColumn<TActor, TRequest, TResponse> :
    ActorCallColumnRuntime
    where TActor : class, IActor
    where TRequest : struct
    where TResponse : struct
{
    private readonly TypedActorStorage<TActor> _owner;
    private readonly ActorCallInvoker<TActor, TRequest, TResponse> _invoker;
    private readonly RingQueueBuffer<ActorCallMail<TRequest, TResponse>> _bufferPool;
    private readonly DirtySlotList _dirtySlots;
    private readonly ActorMailOptions _options;
    private EventMail<ActorCallMail<TRequest, TResponse>>[] _mails;

    public ActorCallColumn(
        TypedActorStorage<TActor> owner,
        ActorCallInvoker<TActor, TRequest, TResponse> invoker,
        ActorMailOptions options,
        int initialSlotCapacity)
    {
        _owner = owner;
        _invoker = invoker;
        _options = options;
        _mails = new EventMail<ActorCallMail<TRequest, TResponse>>[Math.Max(initialSlotCapacity, 1)];
        _bufferPool = new RingQueueBuffer<ActorCallMail<TRequest, TResponse>>();
        _dirtySlots = new DirtySlotList(initialSlotCapacity);
    }

    public PostResult Post(
        int slotIndex,
        in ActorCallMail<TRequest, TResponse> mail)
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
        ref EventMail<ActorCallMail<TRequest, TResponse>> queuedMail = ref _mails[slotIndex];
        int previousCount = queuedMail.Count;
        PostResult result = EventMailWriter.Enqueue(
            ref queuedMail,
            in mail,
            _bufferPool,
            _dirtySlots,
            slotIndex,
            _options,
            ActorPostPolicy.Queued,
            null);

        if (previousCount == 0 && result.IsSuccess && result.CountsAsPending && queuedMail.Count > 0)
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
            ref EventMail<ActorCallMail<TRequest, TResponse>> mail = ref _mails[slotIndex];
            if (!EventMailReader.TryDequeue(ref mail, _bufferPool, out ActorCallMail<TRequest, TResponse> value))
            {
                _dirtySlots.Pop();
                EventMailReader.ReleaseIfEmpty(ref mail, _bufferPool, _options);
                continue;
            }

            if (!_owner.IsAliveSlot(slotIndex))
            {
                value.Source.SetException(new ActorCallException(ActorCallFailureKind.ActorNotFound));
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
                value.Source.SetException(new ActorCallException(ActorCallFailureKind.ActorNotFound));
                _dirtySlots.Pop();
                EventMailReader.ReleaseIfEmpty(ref mail, _bufferPool, _options);
                continue;
            }

            Dispatch(actor, in value);
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

    public override bool HasPendingWork()
    {
        return _dirtySlots.Count > 0;
    }

    public ActorColumnPumpResult PumpOneFast(ref RuntimeFrameBudget budget)
    {
        while (_dirtySlots.TryPeek(out int slotIndex))
        {
            ref EventMail<ActorCallMail<TRequest, TResponse>> mail = ref _mails[slotIndex];
            if (!EventMailReader.TryDequeue(ref mail, _bufferPool, out ActorCallMail<TRequest, TResponse> value))
            {
                _dirtySlots.Pop();
                continue;
            }

            if (!_owner.IsAliveSlot(slotIndex))
            {
                value.Source.SetException(new ActorCallException(ActorCallFailureKind.ActorNotFound));
                _dirtySlots.Pop();
                continue;
            }

            TActor? actor = _owner.Actors[slotIndex];
            if (actor == null)
            {
                value.Source.SetException(new ActorCallException(ActorCallFailureKind.ActorNotFound));
                _dirtySlots.Pop();
                continue;
            }

            Dispatch(actor, in value);
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

    public void Dispatch(
        TActor actor,
        in ActorCallMail<TRequest, TResponse> mail)
    {
        if (mail.CancellationToken.IsCancellationRequested)
        {
            mail.Source.SetCanceled(mail.CancellationToken);
            return;
        }

        try
        {
            LBTask<TResponse> task = _invoker(actor, in mail.Request, mail.CancellationToken);
            ActorCallTaskBridge.Forward(task, mail.Source);
        }
        catch (Exception exception)
        {
            mail.Source.SetException(exception);
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

        ref EventMail<ActorCallMail<TRequest, TResponse>> mail = ref _mails[slotIndex];
        while (EventMailReader.TryDequeue(ref mail, _bufferPool, out ActorCallMail<TRequest, TResponse> value))
        {
            value.Source.SetException(new ActorCallException(ActorCallFailureKind.PendingDestroy));
        }

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

    private bool CanUsePumpFastPath(in ActorMailPumpOptions options)
    {
        return options.MaxMailsPerActorPerPump <= 0
               && !_options.ReleaseWhenEmpty;
    }
}
