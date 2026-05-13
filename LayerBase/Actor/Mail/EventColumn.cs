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
    private readonly int _bucketIndex;
    private readonly ActorEventPostPlan<TEvent> _plan;
    private EventMail<TEvent>[] _mails;

    internal EventMail<TEvent>[] Mails => _mails;
    internal EventMailPool<TEvent> Pool => _mailPool;
    internal DirtySlotList DirtySlots => _dirtySlots;
    internal int BucketIndex => _bucketIndex;
    internal ActorMailOptions Options => _options;

    public EventColumn(
        ActorWorld                            world,
        TypedActorStorage<TActor>             owner,
        ActorBehaviourInvoker<TActor, TEvent> invoker,
        EventMailPool<TEvent>                 mailPool,
        ActorMailOptions                      options,
        int                                   bucketIndex,
        int                                   initialSlotCapacity,
        ActorEventPostPlan<TEvent>            plan)
    {
        _world = world;
        _owner = owner;
        _invoker = invoker;
        _options = options;
        _bucketIndex = bucketIndex;
        _mails = new EventMail<TEvent>[Math.Max(initialSlotCapacity, 1)];
        _plan = plan;
        _mailPool = mailPool;
        _dirtySlots = new DirtySlotList(initialSlotCapacity);
        _writeMode = ResolveWriteMode(options);
    }


    public override ActorColumnPumpResult PumpOne(
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
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

            if (!_owner.CanPumpSlot(slotIndex))
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

            if (!_owner.CanPumpSlot(slotIndex))
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
        TActor    actor,
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
        RefreshPostRowBinding();
    }

    public override void RefreshPostRowBinding()
    {
        if (_plan.RouteCode == ActorPostRouteCode.Disabled)
        {
            return;
        }

        _world.RegisterEventPostRow(
            archetypeId: _owner.ArchetypeId,
            mails: _mails,
            dirtySlots: _dirtySlots,
            bucketIndex: _bucketIndex,
            plan: _plan);
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

    /// <summary>
    /// 批量 Pump 当前 Column。
    ///
    /// 参数说明：
    /// budget：当前帧预算。
    /// options：邮箱 Pump 配置。
    /// stats：Pump 统计构建器。
    /// maxEvents：本次最多处理多少事件。
    ///
    /// 作用：
    /// 如果当前配置适合批量快路径，则处理一个事件。
    /// 注意：为了保持跨 Column 的公平性，每次调用只处理一个事件。
    /// 批量处理的收益来自减少外层调度器的调用次数。
    /// </summary>
    public override ActorPumpManyResult PumpMany(
        ref RuntimeFrameBudget    budget,
        in  ActorMailPumpOptions  options,
        ActorMailPumpStatsBuilder stats,
        int                       maxEvents)
    {
        // 如果当前配置不适合批量快路径，则回退默认实现。
        // 这样可以保证复杂限流、释放空邮箱等场景不被破坏。
        if (!CanUsePumpManyFast(options))
        {
            return base.PumpMany(
                budget: ref budget,
                options: in options,
                stats: stats,
                maxEvents: maxEvents);
        }

        // 为了保持跨 Column 的公平性，每次调用只处理一个事件。
        // 批量处理的收益来自减少外层调度器的调用次数。
        if (maxEvents <= 0 || !budget.HasRemainingEventBudget())
        {
            return ActorPumpManyResult.NoWork();
        }

        int processed = 0;
        int batchLimit = Math.Min(maxEvents,options.MaxEventCountPerPump);
        while (processed < batchLimit &&
               budget.HasRemainingEventBudget() &&
               _dirtySlots.TryPeek(out int slotIndex))
        {
            ref EventMail<TEvent> mail = ref _mails[slotIndex];

            // 从当前 slot 的邮箱中取出一个事件。
            // 如果没有事件，说明 dirty 标记已经过期，直接移除。
            if (!EventMailReader.TryDequeue(ref mail, _mailPool, out TEvent value))
            {
                _dirtySlots.Pop();
                continue;
            }

            // 检查当前 slot 是否仍然可 Pump。
            // 这里会过滤 PendingDestroy、Destroying、空 Actor 等情况。
            if (!_owner.CanPumpSlot(slotIndex))
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

            // 调用 ActorBehaviour invoker。
            // _invoker 通常由生成器或 Actor 元数据构建。
            _invoker(actor, in value);

            // 消耗一个事件预算。
            budget.ConsumeEvent();
            processed++;

            // 当前邮箱清空后移除 dirty slot。
            // 如果还有事件，则移动到队尾，保留基本公平性。
            if (mail.Count == 0)
            {
                _dirtySlots.Pop();
            }
            else
            {
                _dirtySlots.MoveHeadToTail();
            }

        }
        return processed > 0
            ? ActorPumpManyResult.ProcessedBatch(processed)
            : ActorPumpManyResult.NoWork();
    }

    /// <summary>
    /// 判断当前 Column 是否可以使用批量 Pump 快路径。
    ///
    /// 参数说明：
    /// options：Actor 邮箱 Pump 配置。
    ///
    /// 返回值：
    /// true 表示可以连续处理多个事件。
    /// false 表示必须回退 PumpOne。
    /// </summary>
    private bool CanUsePumpManyFast(in ActorMailPumpOptions options)
    {
        return options.MaxMailsPerActorPerPump <= 0
               && options.MaxMailsPerBucketPerPump <= 0
               && !_options.ReleaseWhenEmpty;
    }

    private bool CanUsePumpFastPath(in ActorMailPumpOptions options)
    {
        return options.MaxMailsPerActorPerPump <= 0
               && !_options.ReleaseWhenEmpty;
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