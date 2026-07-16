using System;
using System.Diagnostics;
using LayerBase;
using LayerBase.Actor;
using LayerBase.ECS;
using LayerBase.ECS.Projection;

namespace Arch.Core;

public partial class World
{
    private readonly ActiveProjectedActorList _activeProjectedActors = new();
    private IProjectedActorCommandSink _projectedActorCommandSink = RejectingProjectedActorCommandSink.Instance;
    private ScopeEcsScheduler? _ecsScheduler;

    internal LayerRuntime Runtime { get; private set; } = null!;

    internal ScopeEcsScheduler EcsScheduler =>
        _ecsScheduler ?? throw new InvalidOperationException("World is not bound to a ScopeEcsScheduler.");

    internal IProjectedActorCommandSink ProjectedActorCommands => _projectedActorCommandSink;

    internal void BindRuntime(
        LayerRuntime runtime)
    {
        Runtime = runtime;
    }

    internal void BindEcsScheduler(
        ScopeEcsScheduler scheduler)
    {
        _ecsScheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    internal void BindProjectedActorCommandSink(
        IProjectedActorCommandSink commandSink)
    {
        _projectedActorCommandSink =
            commandSink ?? throw new ArgumentNullException(nameof(commandSink));
    }

    internal ref ProjectedActorMeta GetProjectionMeta(
        Entity entity)
    {
        ref EntityData data = ref EntityInfo.GetEntityData(entity.Id);
        ref Chunk chunk = ref data.Archetype.GetChunk(data.Slot.ChunkIndex);
        return ref chunk.ProjectionAt(data.Slot.Index);
    }

    internal bool TryGetProjectionMeta(
        Entity                    entity,
        out ProjectedActorMetaRef metaRef)
    {
        if (!EntityInfo.Has(entity.Id))
        {
            metaRef = default;
            return false;
        }

        ref EntityData data = ref EntityInfo.GetEntityData(entity.Id);
        if (data.Version != entity.Version)
        {
            metaRef = default;
            return false;
        }

        ref Chunk chunk = ref data.Archetype.GetChunk(data.Slot.ChunkIndex);
        metaRef = new ProjectedActorMetaRef(ref chunk.ProjectionAt(data.Slot.Index));
        return true;
    }

    internal void AddActiveProjectedActor(
        Entity                 entity,
        ref ProjectedActorMeta meta)
    {
        _activeProjectedActors.Add(entity, ref meta);
    }

    internal void SweepProjectedActors(int maxCount = 512)
    {
        var budget = new RuntimeFrameBudget(0, 0, 0);
        SweepProjectedActors(ref budget, maxCount);
    }

    internal int SweepProjectedActors(ref RuntimeFrameBudget budget, int maxSliceItems = 512)
    {
        if (!budget.CanContinue(Stopwatch.GetTimestamp()))
            return 0;

        int allowed = Math.Min(maxSliceItems, budget.RemainingWorkItems);
        if (allowed <= 0)
            return 0;

        int processed = _activeProjectedActors.Sweep(this, allowed, budget.DeadlineTicks);
        budget.Consume(processed);
        return processed;
    }

    internal void ApplyProjectedActorResult(
        in ProjectedActorScopeResult result)
    {
        if (!IsAlive(result.Entity))
        {
            return;
        }

        if (!TryGetProjectionMeta(result.Entity, out ProjectedActorMetaRef metaRef))
        {
            return;
        }

        ref ProjectedActorMeta meta = ref metaRef.Value;
        if (result.ActorTypeId >= 0 && meta.ActorTypeId != result.ActorTypeId)
        {
            return;
        }

        switch (result.Kind)
        {
            case ProjectedActorScopeCommandKind.Ensure:
                meta.EnsurePending = false;
                if (!result.Success || !result.ActorId.IsValid)
                {
                    return;
                }

                if (Has<ProjectedActorRef>(result.Entity))
                {
                    ref ProjectedActorRef actorRef = ref Get<ProjectedActorRef>(result.Entity);
                    ProjectedActorBindingUtility.Bind(
                        ref meta,
                        ref actorRef,
                        result.ActorId,
                        result.NowTicks);
                }
                else
                {
                    ProjectedActorBindingUtility.Bind(
                        this,
                        result.Entity,
                        ref meta,
                        result.ActorId,
                        result.NowTicks);
                }

                AddActiveProjectedActor(result.Entity, ref meta);
                return;

            case ProjectedActorScopeCommandKind.Release:
                if (meta.ActorId.Equals(result.ActorId))
                {
                    ProjectedActorBindingUtility.Clear(this, result.Entity, ref meta);
                    if (Has<ProjectedActorRef>(result.Entity))
                    {
                        ref ProjectedActorRef actorRef = ref Get<ProjectedActorRef>(result.Entity);
                        actorRef.ClearActor();
                    }

                    _activeProjectedActors.Remove(this, result.Entity, ref meta);
                    meta.State = ProjectedActorState.Released;
                }
                return;

            case ProjectedActorScopeCommandKind.Disable:
                if (meta.ActorId.Equals(result.ActorId))
                {
                    meta.State = result.Success
                        ? ProjectedActorState.Disabled
                        : ProjectedActorState.Active;
                }
                return;

            case ProjectedActorScopeCommandKind.Enable:
                meta.EnablePending = false;
                if (meta.ActorId.Equals(result.ActorId))
                {
                    if (result.Success)
                    {
                        meta.State = ProjectedActorState.Active;
                        if (Has<ProjectedActorRef>(result.Entity))
                        {
                            ref ProjectedActorRef actorRef = ref Get<ProjectedActorRef>(result.Entity);
                            actorRef.Bind(result.ActorId, result.NowTicks);
                        }
                    }
                    else
                    {
                        ProjectedActorBindingUtility.Clear(this, result.Entity, ref meta);
                        if (Has<ProjectedActorRef>(result.Entity))
                        {
                            ref ProjectedActorRef actorRef = ref Get<ProjectedActorRef>(result.Entity);
                            actorRef.ClearActor();
                        }

                        _activeProjectedActors.Remove(this, result.Entity, ref meta);
                    }
                }
                return;
        }
    }
}
