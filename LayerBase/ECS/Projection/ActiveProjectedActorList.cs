using System.Diagnostics;
using System.Runtime.InteropServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal sealed class ActiveProjectedActorList
{
    private ProjectedEntityRef[] _items = new ProjectedEntityRef[64];
    private int _count;

    public void Add(
        Entity entity,
        ref ProjectedActorMeta meta)
    {
        if (meta.ActiveListIndex >= 0)
        {
            return;
        }

        int index = _count;
        if ((uint)index >= (uint)_items.Length)
        {
            Array.Resize(ref _items, _items.Length << 1);
        }

        _items[index] = new ProjectedEntityRef(entity);
        _count = index + 1;
        meta.ActiveListIndex = index;
    }

    public void Sweep(
        World world,
        ActorWorld actorWorld)
    {
        long nowTicks = Stopwatch.GetTimestamp();

        for (int i = _count - 1; i >= 0; i--)
        {
            Entity entity = _items[i].Entity;
            if (!world.TryGetProjectionMeta(
                    entity,
                    out ProjectedActorMetaRef metaRef))
            {
                RemoveDeadAt(world, i);
                continue;
            }

            ref ProjectedActorMeta meta = ref metaRef.Value;
            if (!meta.ActorId.IsValid)
            {
                RemoveAt(world, i, ref meta);
                continue;
            }

            if (!actorWorld.TryGetPooledActor(
                    meta.ActorId,
                    out IPooledActor pooledActor))
            {
                meta.ClearActor();
                RemoveAt(world, i, ref meta);
                continue;
            }

            if (nowTicks < pooledActor.RecycleDeadlineTicks)
            {
                continue;
            }

            actorWorld.ReleaseProjectedActor(
                meta.ActorId,
                meta.ReleasePolicy);

            meta.ClearActor();
            RemoveAt(world, i, ref meta);
        }
    }

    private void RemoveAt(
        World world,
        int index,
        ref ProjectedActorMeta meta)
    {
        int lastIndex = _count - 1;
        ProjectedEntityRef moved = _items[lastIndex];
        _items[index] = moved;
        _items[lastIndex] = default;
        _count = lastIndex;
        meta.ActiveListIndex = -1;

        if (index != lastIndex
            && world.TryGetProjectionMeta(
                moved.Entity,
                out ProjectedActorMetaRef movedMetaRef))
        {
            movedMetaRef.Value.ActiveListIndex = index;
        }
    }

    private void RemoveDeadAt(
        World world,
        int index)
    {
        int lastIndex = _count - 1;
        ProjectedEntityRef moved = _items[lastIndex];
        _items[index] = moved;
        _items[lastIndex] = default;
        _count = lastIndex;

        if (index != lastIndex
            && world.TryGetProjectionMeta(
                moved.Entity,
                out ProjectedActorMetaRef movedMetaRef))
        {
            movedMetaRef.Value.ActiveListIndex = index;
        }
    }
}

internal readonly struct ProjectedEntityRef
{
    public readonly Entity Entity;

    public ProjectedEntityRef(
        Entity entity)
    {
        Entity = entity;
    }
}

internal readonly ref struct ProjectedActorMetaRef
{
    private readonly Span<ProjectedActorMeta> _span;

    public ref ProjectedActorMeta Value => ref _span[0];

    public ProjectedActorMetaRef(
        ref ProjectedActorMeta value)
    {
        _span = MemoryMarshal.CreateSpan(ref value, 1);
    }
}
