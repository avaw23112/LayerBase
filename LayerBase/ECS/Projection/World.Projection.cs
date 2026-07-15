using System;
using LayerBase;
using LayerBase.ECS.Projection;

namespace Arch.Core;

public partial class World
{
    private readonly ActiveProjectedActorList _activeProjectedActors = new();
    private IProjectedActorCommandSink _projectedActorCommandSink = RejectingProjectedActorCommandSink.Instance;

    internal LayerRuntime Runtime { get; private set; } = null!;

    internal IProjectedActorCommandSink ProjectedActorCommands => _projectedActorCommandSink;

    internal void BindRuntime(
        LayerRuntime runtime)
    {
        Runtime = runtime;
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
        _activeProjectedActors.Sweep(this, maxCount);
    }

    internal void ApplyProjectedActorResult(
        in ProjectedActorScopeResult result)
    {
        if (!result.Success || !IsAlive(result.Entity))
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
                if (!result.ActorId.IsValid)
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
                    ProjectedActorBindingUtility.Clear(this, result.Entity, ref meta);
                return;

            case ProjectedActorScopeCommandKind.Disable:
                if (meta.ActorId.Equals(result.ActorId))
                    meta.State = ProjectedActorState.Disabled;
                return;

            case ProjectedActorScopeCommandKind.Enable:
                if (meta.ActorId.Equals(result.ActorId))
                    meta.State = ProjectedActorState.Active;
                return;
        }
    }
}
