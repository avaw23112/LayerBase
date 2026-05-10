using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    internal EventMailPool<TEvent> GetOrCreateEventMailPoolCold<TEvent>()
        where TEvent : struct
    {
        if (!EventMailPoolRuntime<TEvent>.TryGetMailPool(this, out EventMailPool<TEvent>? pool)
            || pool == null)
        {
            ActorMailOptions options = ResolveMailOptions(LayerBase.Core.Event.EventTypeId<TEvent>.Id);
            pool = new EventMailPool<TEvent>(options);
            EventMailPoolRuntime<TEvent>.BindWorld(this, pool);
        }

        return pool;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool PostQueuedGrowFastNoResult<TEvent>(
        int slotIndex,
        in TEvent value,
        EventMail<TEvent>[] mails,
        DirtySlotList dirtySlots,
        int bucketIndex,
        EventMailPool<TEvent> pool)
        where TEvent : struct
    {
        ref EventMail<TEvent> mail = ref mails[slotIndex];

        if (mail.BufferId == 0)
        {
            mail.BufferId = pool.RentInitial();
            mail.Head = 0;
            mail.Tail = 0;
            mail.Count = 0;
            mail.Capacity = pool.GetCapacity(mail.BufferId);
        }

        if (mail.Count >= mail.Capacity)
        {
            if (!pool.TryGrow(ref mail))
            {
                return false;
            }
        }

        pool.Write(mail.BufferId, mail.Tail, in value);
        mail.Tail++;
        if (mail.Tail == mail.Capacity)
        {
            mail.Tail = 0;
        }

        mail.Count++;
        if (mail.Count == 1)
        {
            dirtySlots.Mark(slotIndex);
            _dirtyEventBuckets.Mark(bucketIndex);
        }

        return true;
    }

    internal void RegisterEventPostRow<TEvent>(
        int archetypeId,
        EventMail<TEvent>[] mails,
        EventMailPool<TEvent> pool,
        DirtySlotList dirtySlots,
        int bucketIndex,
        int[] generations)
        where TEvent : struct
    {
        EventPostRow<TEvent>[] rows = GetOrCreateRowsByArchetypeCold<TEvent>();
        if ((uint)archetypeId >= (uint)rows.Length)
        {
            int newSize = rows.Length == 0 ? 4 : rows.Length;
            while (newSize <= archetypeId)
            {
                newSize <<= 1;
            }

            Array.Resize(ref rows, newSize);
        }

        rows[archetypeId] = new EventPostRow<TEvent>(
            mails,
            pool,
            dirtySlots,
            bucketIndex,
            generations);

        EventPostRuntime<TEvent>.BindWorld(this, rows);
    }

    private EventPostRow<TEvent>[] GetOrCreateRowsByArchetypeCold<TEvent>()
        where TEvent : struct
    {
        if (EventPostRuntime<TEvent>.TryGetRows(this, out EventPostRow<TEvent>[]? rows)
            && rows != null)
        {
            return rows;
        }

        rows = new EventPostRow<TEvent>[Math.Max(_archetypes.Length, 4)];
        EventPostRuntime<TEvent>.BindWorld(this, rows);
        return rows;
    }
}
