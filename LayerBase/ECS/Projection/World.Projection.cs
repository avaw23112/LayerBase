using LayerBase;
using LayerBase.ECS.Projection;

namespace Arch.Core;

public partial class World
{
    private readonly ActiveProjectedActorList _activeProjectedActors = new();

    internal LayerRuntime Runtime { get; private set; } = null!;

    internal void BindRuntime(
        LayerRuntime runtime)
    {
        Runtime = runtime;
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

    internal void SweepProjectedActors()
    {
        _activeProjectedActors.Sweep(this, Runtime.Actors);
    }
}