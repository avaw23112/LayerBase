using System.Diagnostics;
using System.Runtime.InteropServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal sealed class ActiveProjectedActorList
{
    private ProjectedEntityRef[] _items = new ProjectedEntityRef[64];
    private int _count;
    private int _sweepCursor;

    public void Add(
        Entity                 entity,
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

    /// <summary>
    /// Sweep 预算化版本。
    ///
    /// 参数说明：
    /// world：ECS World。
    /// actorWorld：ActorWorld。
    /// maxCount：单帧最多处理的 Actor 数量。
    ///
    /// 行为：
    /// 1. 单帧最多处理 maxCount 个。
    /// 2. 多帧轮转处理所有 active projected actor。
    /// 3. 不使用 Dictionary。
    /// 4. Disable 不清 ActorId。
    /// 5. 到期判断使用 ProjectedActorRef.ExpireAtTicks，不再依赖 IPooledActor。
    /// </summary>
    public void Sweep(
        World      world,
        ActorWorld actorWorld,
        int        maxCount = 512)
    {
        if (_count == 0)
        {
            return;
        }

        long nowTicks = Stopwatch.GetTimestamp();
        int processed = 0;

        for (int i = 0; i < _count && processed < maxCount; i++)
        {
            int index = (_sweepCursor + i) % _count;
            Entity entity = _items[index].Entity;

            if (!world.TryGetProjectionMeta(
                    entity,
                    out ProjectedActorMetaRef metaRef))
            {
                RemoveDeadAt(world, index);
                processed++;
                continue;
            }

            ref ProjectedActorMeta meta = ref metaRef.Value;
            if (!meta.ActorId.IsValid)
            {
                RemoveAt(world, index, ref meta);
                processed++;
                continue;
            }

            // 读取 ProjectedActorRef 以获取 ExpireAtTicks
            ref ProjectedActorRef actorRef = ref world.Get<ProjectedActorRef>(entity);

            // 使用 ExpireAtTicks 判断是否到期
            if (nowTicks < actorRef.ExpireAtTicks)
            {
                continue;
            }

            // 到期处理 - 需要获取 pooledActor 以调用生命周期方法
            if (!actorWorld.TryGetPooledActor(
                    meta.ActorId,
                    out IPooledActor pooledActor))
            {
                ProjectedActorBindingUtility.Clear(world, entity, ref meta);
                actorRef.ClearActor();
                RemoveAt(world, index, ref meta);
                processed++;
                continue;
            }

            RetireProjectedActor(world, actorWorld, entity, ref meta, ref actorRef, pooledActor);
            processed++;
        }

        _sweepCursor = (_sweepCursor + processed) % Math.Max(1, _count);
    }

    /// <summary>
    /// 根据 RetirePolicy 处理到期的 ProjectedActor。
    /// </summary>
    private void RetireProjectedActor(
        World                  world,
        ActorWorld             actorWorld,
        Entity                 entity,
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef  actorRef,
        IPooledActor           pooledActor)
    {
        switch (meta.RetirePolicy)
        {
            case ProjectedActorRetirePolicy.Disable:
                actorWorld.DisableProjectedActor(meta.ActorId);
                meta.State = ProjectedActorState.Disabled;
                return;

            case ProjectedActorRetirePolicy.ReturnToPool:
                actorWorld.ReleaseProjectedActor(
                    meta.ActorId,
                    ProjectedActorReleasePolicy.ReturnToPool);

                ProjectedActorBindingUtility.Clear(world, entity, ref meta);
                actorRef.ClearActor();
                RemoveAt(world, meta.ActiveListIndex, ref meta);
                return;

            case ProjectedActorRetirePolicy.DestroyImmediately:
                actorWorld.ReleaseProjectedActor(
                    meta.ActorId,
                    ProjectedActorReleasePolicy.DestroyImmediately);

                ProjectedActorBindingUtility.Clear(world, entity, ref meta);
                actorRef.ClearActor();
                RemoveAt(world, meta.ActiveListIndex, ref meta);
                return;

            case ProjectedActorRetirePolicy.DetachAndLetActorFinish:
                actorWorld.ReleaseProjectedActor(
                    meta.ActorId,
                    ProjectedActorReleasePolicy.DetachAndLetActorFinish);

                ProjectedActorBindingUtility.Clear(world, entity, ref meta);
                actorRef.ClearActor();
                RemoveAt(world, meta.ActiveListIndex, ref meta);
                return;
        }
    }

    private void RemoveAt(
        World                  world,
        int                    index,
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
        int   index)
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
