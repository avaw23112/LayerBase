using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

/// <summary>
/// 单个事件类型的全局事件流中心。
///
/// 作用：
/// 1. 管理该事件类型的所有邮件。
/// 2. 维护 slotIndex → handler 映射表。
/// 3. 维护 slotIndex → generation 映射表。
/// 4. 提供 Post 和 Pump 接口。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal sealed class EventStreamCenter<TEvent>
    where TEvent : struct
{
    private readonly EventStreamSegmentPool<TEvent> _segmentPool;
    private ActorEventHandler<TEvent>?[] _handlersBySlot;
    private int[] _aliveGenerations;
    private EventStreamSegment<TEvent>? _head;
    private EventStreamSegment<TEvent>? _tail;
    private int _count;

    /// <summary>
    /// 当前事件流是否为空。
    /// </summary>
    public bool IsEmpty => _count == 0;

    /// <summary>
    /// 当前事件流中待处理的邮件数量。
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// 构造 EventStreamCenter。
    /// </summary>
    /// <param name="options">
    /// EventStream 配置。
    /// </param>
    public EventStreamCenter(EventStreamOptions options)
    {
        _segmentPool = new EventStreamSegmentPool<TEvent>(
            options.SegmentCapacity,
            options.MaxRetainedSegments);
        _handlersBySlot = Array.Empty<ActorEventHandler<TEvent>?>();
        _aliveGenerations = Array.Empty<int>();
        _head = null;
        _tail = null;
        _count = 0;
    }

    /// <summary>
    /// 注册 Actor 的事件处理器。
    ///
    /// 作用：
    /// 在 Actor 创建时调用，将 handler 和 generation 写入 slot。
    /// </summary>
    /// <param name="slotIndex">
    /// Actor 的 slotIndex。
    /// </param>
    /// <param name="generation">
    /// Actor 的当前 generation。
    /// </param>
    /// <param name="handler">
    /// 绑定到该 Actor 实例的事件处理委托。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterHandler(
        int slotIndex,
        int generation,
        ActorEventHandler<TEvent> handler)
    {
        EnsureSlotCapacity(slotIndex);
        _handlersBySlot[slotIndex] = handler;
        _aliveGenerations[slotIndex] = generation;
    }

    /// <summary>
    /// 注销 Actor 的事件处理器。
    ///
    /// 作用：
    /// 在 Actor 销毁时调用，清除 handler 并将 generation 设为 -1。
    /// </summary>
    /// <param name="slotIndex">
    /// Actor 的 slotIndex。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnregisterHandler(int slotIndex)
    {
        if ((uint)slotIndex < (uint)_handlersBySlot.Length)
        {
            _handlersBySlot[slotIndex] = null;
            _aliveGenerations[slotIndex] = -1;
        }
    }

    /// <summary>
    /// 向事件流投递一封邮件。
    ///
    /// 作用：
    /// 将事件写入尾部 Segment。
    /// 如果尾部 Segment 已满，从池中租借新的 Segment。
    /// </summary>
    /// <param name="actorId">
    /// 目标 ActorId。
    /// </param>
    /// <param name="value">
    /// 事件值。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post(
        ActorId actorId,
        in TEvent value)
    {
        EventStreamSegment<TEvent> tail = EnsureWritableTail();

        ref EventStreamMail<TEvent> mail =
            ref tail.Items[tail.WriteIndex];

        mail.SlotIndex = actorId.SlotIndex;
        mail.Generation = actorId.Generation;
        mail.Value = value;

        tail.WriteIndex++;
        _count++;
    }

    /// <summary>
    /// Pump 当前事件流。
    ///
    /// 作用：
    /// 从头部 Segment 开始读取邮件并分发。
    /// 如果邮件的 generation 不匹配，跳过该邮件。
    /// 读空的 Segment 会被归还到池中。
    /// </summary>
    /// <param name="maxCount">
    /// 本次最多处理多少封邮件。
    /// 用于接入 RuntimeFrameBudget。
    /// </param>
    /// <returns>
    /// 实际处理数量。
    /// </returns>
    public int Pump(int maxCount)
    {
        int processed = 0;

        while (_head != null &&
               _count > 0 &&
               processed < maxCount)
        {
            EventStreamSegment<TEvent> head = _head;

            while (!head.IsEmpty &&
                   processed < maxCount)
            {
                ref EventStreamMail<TEvent> mail =
                    ref head.Items[head.ReadIndex];

                Dispatch(in mail);

                head.ReadIndex++;
                _count--;
                processed++;
            }

            if (head.IsEmpty)
            {
                ReleaseHeadSegment();
            }
        }

        return processed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Dispatch(in EventStreamMail<TEvent> mail)
    {
        int slotIndex = mail.SlotIndex;

        if ((uint)slotIndex >= (uint)_aliveGenerations.Length)
        {
            return;
        }

        if (_aliveGenerations[slotIndex] != mail.Generation)
        {
            return;
        }

        ActorEventHandler<TEvent>? handler = _handlersBySlot[slotIndex];
        handler?.Invoke(in mail.Value);
    }

    /// <summary>
    /// 直接分发事件给指定 Actor，不经过邮箱队列。
    ///
    /// 作用：
    /// 用于 DispatchNow 场景，需要立即执行事件处理。
    /// </summary>
    /// <param name="actorId">
    /// 目标 ActorId。
    /// </param>
    /// <param name="value">
    /// 事件值。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DispatchNow(
        ActorId actorId,
        in TEvent value)
    {
        int slotIndex = actorId.SlotIndex;

        if ((uint)slotIndex >= (uint)_aliveGenerations.Length)
        {
            return;
        }

        if (_aliveGenerations[slotIndex] != actorId.Generation)
        {
            return;
        }

        ActorEventHandler<TEvent>? handler = _handlersBySlot[slotIndex];
        handler?.Invoke(in value);
    }

    private void EnsureSlotCapacity(int slotIndex)
    {
        if ((uint)slotIndex < (uint)_handlersBySlot.Length)
        {
            return;
        }

        int oldLength = _handlersBySlot.Length;
        int newLength = oldLength == 0 ? 4 : oldLength;

        while (newLength <= slotIndex)
        {
            newLength *= 2;
        }

        Array.Resize(ref _handlersBySlot, newLength);
        Array.Resize(ref _aliveGenerations, newLength);
        Array.Fill(_aliveGenerations, -1, oldLength, newLength - oldLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EventStreamSegment<TEvent> EnsureWritableTail()
    {
        EventStreamSegment<TEvent>? tail = _tail;

        if (tail != null && !tail.IsFull)
        {
            return tail;
        }

        EventStreamSegment<TEvent> next = _segmentPool.Rent();

        if (_tail == null)
        {
            _head = next;
            _tail = next;
            return next;
        }

        _tail.Next = next;
        _tail = next;
        return next;
    }

    private void ReleaseHeadSegment()
    {
        EventStreamSegment<TEvent>? head = _head;
        if (head == null)
        {
            return;
        }

        _head = head.Next;
        if (_head == null)
        {
            _tail = null;
        }

        _segmentPool.Return(head);
    }
}
