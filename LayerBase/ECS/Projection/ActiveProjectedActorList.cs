using System.Diagnostics;
using System.Runtime.InteropServices;
using Arch.Core;
using LayerBase.Actor.RuntimeCommands;

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
    /// world 参数作用：
    /// 当前 ECS World。
    ///
    /// actorWorld 参数作用：
    /// 当前 ActorWorld，用于执行 Disable / ReturnToPool 等生命周期操作。
    ///
    /// maxCount 参数作用：
    /// 单帧最多检查的 ProjectedActor 数量。
    /// 注意：这里限制的是检查数量，不是退场数量。
    /// </summary>
    public void Sweep(
        World                        world,
        IProjectedActorLifecycleSink lifecycleSink,
        int                          maxCount = 512)
    {
        if (_count == 0 || maxCount <= 0)
        {
            return;
        }

        long nowTicks = Stopwatch.GetTimestamp();
        int inspected = 0;

        for (int i = 0; i < _count && inspected < maxCount;)
        {
            int index = (_sweepCursor + i) % _count;
            inspected++;

            Entity entity = _items[index].Entity;

            if (!world.TryGetProjectionMeta(
                    entity,
                    out ProjectedActorMetaRef metaRef))
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

            // 读取 ProjectedActorRef 以获取 ExpireAtTicks
            ref ProjectedActorRef actorRef = ref world.Get<ProjectedActorRef>(entity);

            // 使用 ExpireAtTicks 判断是否到期
            if (nowTicks < actorRef.ExpireAtTicks)
            {
                i++;
                continue;
            }

            // 到期处理 - 需要获取 pooledActor 以调用生命周期方法
            RetireProjectedActor(world, lifecycleSink, entity, ref meta, ref actorRef);
        }

        if (_count == 0)
        {
            _sweepCursor = 0;
            return;
        }

        _sweepCursor = (_sweepCursor + inspected) % _count;
    }

    /// <summary>
    /// 根据 RetirePolicy 处理到期的 ProjectedActor。
    /// </summary>
    private void RetireProjectedActor(
        World                        world,
        IProjectedActorLifecycleSink lifecycleSink,
        Entity                       entity,
        ref ProjectedActorMeta       meta,
        ref ProjectedActorRef        actorRef)
    {
        switch (meta.RetirePolicy)
        {
            case ProjectedActorRetirePolicy.Disable:
                ControlEnqueueResult disableResult = lifecycleSink.TryDisableProjectedActor(meta.ActorId);
                if (disableResult == ControlEnqueueResult.Closed)
                {
                    return;
                }

                if (disableResult == ControlEnqueueResult.Failed)
                {
                    ProjectedActorBindingUtility.Clear(world, entity, ref meta);
                    actorRef.ClearActor();
                    RemoveAt(world, meta.ActiveListIndex, ref meta);
                    return;
                }

                meta.State = ProjectedActorState.DisablePending;

                // ExpireAtTicks 参数作用：
                // Disable 后不再让该 Actor 持续命中到期判断。
                // 下一次 Touch 时 RefreshProjectedActorInterest 会先 OnEnable，再刷新 ExpireAtTicks。
                actorRef.ExpireAtTicks = long.MaxValue;

                return;

            case ProjectedActorRetirePolicy.ReturnToPool:
                if (lifecycleSink.TryReleaseProjectedActor(
                    meta.ActorId,
                    ProjectedActorReleasePolicy.ReturnToPool) == ControlEnqueueResult.Closed)
                {
                    return;
                }

                ProjectedActorBindingUtility.Clear(world, entity, ref meta);
                actorRef.ClearActor();
                RemoveAt(world, meta.ActiveListIndex, ref meta);
                return;

            case ProjectedActorRetirePolicy.DestroyImmediately:
                if (lifecycleSink.TryReleaseProjectedActor(
                    meta.ActorId,
                    ProjectedActorReleasePolicy.DestroyImmediately) == ControlEnqueueResult.Closed)
                {
                    return;
                }

                ProjectedActorBindingUtility.Clear(world, entity, ref meta);
                actorRef.ClearActor();
                RemoveAt(world, meta.ActiveListIndex, ref meta);
                return;

            case ProjectedActorRetirePolicy.DetachAndLetActorFinish:
                if (lifecycleSink.TryReleaseProjectedActor(
                    meta.ActorId,
                    ProjectedActorReleasePolicy.DetachAndLetActorFinish) == ControlEnqueueResult.Closed)
                {
                    return;
                }

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
