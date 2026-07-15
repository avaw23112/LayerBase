using System.Diagnostics;
using System.Runtime.InteropServices;
using Arch.Core;

namespace LayerBase.ECS.Projection;

internal sealed class ActiveProjectedActorList
{
    private ProjectedEntityRef[] _items = new ProjectedEntityRef[64];
    private int _count;
    private int _sweepCursor;

    public void Add(
        Entity entity,
        ref ProjectedActorMeta meta)
    {
        if (meta.ActiveListIndex >= 0)
            return;

        int index = _count;
        if ((uint)index >= (uint)_items.Length)
            Array.Resize(ref _items, _items.Length << 1);

        _items[index] = new ProjectedEntityRef(entity);
        _count = index + 1;
        meta.ActiveListIndex = index;
    }

    public void Sweep(
        World world,
        int maxCount = 512)
    {
        if (_count == 0 || maxCount <= 0)
            return;

        long nowTicks = Stopwatch.GetTimestamp();
        int inspected = 0;

        for (int i = 0; i < _count && inspected < maxCount;)
        {
            int index = (_sweepCursor + i) % _count;
            inspected++;

            Entity entity = _items[index].Entity;

            if (!world.TryGetProjectionMeta(entity, out ProjectedActorMetaRef metaRef))
            {
                RemoveDeadAt(world, index);
                continue;
            }

            ref ProjectedActorMeta meta = ref metaRef.Value;
            if (!meta.ActorId.IsValid)
            {
                RemoveAt(world, index, ref meta);
                continue;
            }

            if (!world.Has<ProjectedActorRef>(entity))
            {
                ProjectedActorBindingUtility.Clear(world, entity, ref meta);
                RemoveAt(world, index, ref meta);
                continue;
            }

            ref ProjectedActorRef actorRef = ref world.Get<ProjectedActorRef>(entity);
            if (nowTicks < actorRef.ExpireAtTicks)
            {
                i++;
                continue;
            }

            if (!world.ProjectedActorCommands.Exists(meta.ActorId))
            {
                ProjectedActorBindingUtility.Clear(world, entity, ref meta);
                actorRef.ClearActor();
                RemoveAt(world, index, ref meta);
                continue;
            }

            RetireProjectedActor(world, entity, ref meta, ref actorRef, nowTicks);
        }

        _sweepCursor = _count == 0
            ? 0
            : (_sweepCursor + inspected) % _count;
    }

    private void RetireProjectedActor(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef actorRef,
        long nowTicks)
    {
        switch (meta.RetirePolicy)
        {
            case ProjectedActorRetirePolicy.Disable:
                if (world.ProjectedActorCommands.Disable(
                        entity,
                        meta.ActorTypeId,
                        meta.ActorId,
                        nowTicks))
                {
                    meta.State = world.ProjectedActorCommands.CompletesSynchronously
                        ? ProjectedActorState.Disabled
                        : ProjectedActorState.DisablePending;
                    actorRef.ExpireAtTicks = long.MaxValue;
                }
                return;

            case ProjectedActorRetirePolicy.ReturnToPool:
                if (!world.ProjectedActorCommands.Release(
                        entity,
                        meta.ActorTypeId,
                        meta.ActorId,
                        ProjectedActorReleasePolicy.ReturnToPool,
                        nowTicks))
                    return;
                break;

            case ProjectedActorRetirePolicy.DestroyImmediately:
                if (!world.ProjectedActorCommands.Release(
                        entity,
                        meta.ActorTypeId,
                        meta.ActorId,
                        ProjectedActorReleasePolicy.DestroyImmediately,
                        nowTicks))
                    return;
                break;

            case ProjectedActorRetirePolicy.DetachAndLetActorFinish:
                if (!world.ProjectedActorCommands.Release(
                        entity,
                        meta.ActorTypeId,
                        meta.ActorId,
                        ProjectedActorReleasePolicy.DetachAndLetActorFinish,
                        nowTicks))
                    return;
                break;
        }

        if (!world.ProjectedActorCommands.CompletesSynchronously)
        {
            meta.State = ProjectedActorState.ReleasePending;
            actorRef.ExpireAtTicks = long.MaxValue;
            return;
        }

        ProjectedActorBindingUtility.Clear(world, entity, ref meta);
        actorRef.ClearActor();
        RemoveAt(world, meta.ActiveListIndex, ref meta);
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

        if (index != lastIndex &&
            world.TryGetProjectionMeta(moved.Entity, out ProjectedActorMetaRef movedMetaRef))
        {
            movedMetaRef.Value.ActiveListIndex = index;
        }
    }

    public void Remove(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta)
    {
        if (meta.ActiveListIndex < 0)
            return;

        RemoveAt(world, meta.ActiveListIndex, ref meta);
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

        if (index != lastIndex &&
            world.TryGetProjectionMeta(moved.Entity, out ProjectedActorMetaRef movedMetaRef))
        {
            movedMetaRef.Value.ActiveListIndex = index;
        }
    }
}

internal readonly struct ProjectedEntityRef
{
    public readonly Entity Entity;

    public ProjectedEntityRef(Entity entity)
    {
        Entity = entity;
    }
}

internal readonly ref struct ProjectedActorMetaRef
{
    private readonly Span<ProjectedActorMeta> _span;

    public ref ProjectedActorMeta Value => ref _span[0];

    public ProjectedActorMetaRef(ref ProjectedActorMeta value)
    {
        _span = MemoryMarshal.CreateSpan(ref value, 1);
    }
}
