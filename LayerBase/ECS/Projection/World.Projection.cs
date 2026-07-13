using LayerBase;
using LayerBase.Actor;
using LayerBase.Actor.RuntimeCommands;
using LayerBase.ECS.Projection;

namespace Arch.Core;

public partial class World
{
    private readonly ActiveProjectedActorList _activeProjectedActors = new();
    private ActorWorld? _scopeActors;
    private IProjectedActorLifecycleSink? _projectedActorLifecycleSink;

    internal LayerRuntime Runtime { get; private set; } = null!;

    internal void BindRuntime(
        LayerRuntime runtime)
    {
        Runtime = runtime;
        _projectedActorLifecycleSink = new LayerRuntimeProjectedActorLifecycleSink(runtime);
    }

    internal void BindScopeActors(ActorWorld actors)
    {
        BindScopeActors(actors, new ActorWorldProjectedActorLifecycleSink(actors));
    }

    internal void BindScopeActors(
        ActorWorld actors,
        IProjectedActorLifecycleSink lifecycleSink)
    {
        _scopeActors = actors ?? throw new ArgumentNullException(nameof(actors));
        _projectedActorLifecycleSink = lifecycleSink ?? throw new ArgumentNullException(nameof(lifecycleSink));
    }

    internal bool TryGetRuntime(out LayerRuntime? runtime)
    {
        runtime = Runtime;
        return runtime != null;
    }

    internal bool ShouldPrebindProjectedActorOnMark
    {
        get
        {
            return Runtime != null &&
                   Runtime.IsOwnerThreadForActorWorld &&
                   Runtime.EcsOptions.ExecutionMode == LayerBase.ECS.Runtime.EcsExecutionMode.Async;
        }
    }

    internal ActorWorld GetActorWorld()
    {
        if (_scopeActors != null)
        {
            return _scopeActors;
        }

        if (Runtime != null)
        {
            return Runtime.Actors;
        }

        throw new InvalidOperationException("ECS World is not bound to an ActorWorld.");
    }

    private IProjectedActorLifecycleSink GetProjectedActorLifecycleSink()
    {
        if (_projectedActorLifecycleSink != null)
        {
            return _projectedActorLifecycleSink;
        }

        _projectedActorLifecycleSink = new ActorWorldProjectedActorLifecycleSink(GetActorWorld());
        return _projectedActorLifecycleSink;
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
        _activeProjectedActors.Sweep(this, GetProjectedActorLifecycleSink(), maxCount);
    }

    internal ControlEnqueueResult TryReleaseProjectedActor(
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy)
    {
        return GetProjectedActorLifecycleSink().TryReleaseProjectedActor(actorId, releasePolicy);
    }

    internal ControlEnqueueResult TryEnableProjectedActor(ActorId actorId)
    {
        return GetProjectedActorLifecycleSink().TryEnableProjectedActor(actorId);
    }
}
