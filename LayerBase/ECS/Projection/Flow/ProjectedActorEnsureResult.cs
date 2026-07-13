using System.Diagnostics;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.ECS.Runtime;

namespace LayerBase.ECS.Projection.Flow;

internal sealed class ProjectedActorEnsureResult : IEcsResultItem
{
    private readonly World _world;
    private readonly ActorWorld _actorWorld;
    private readonly Entity _entity;
    private readonly int _actorTypeId;

    public ProjectedActorEnsureResult(
        string debugName,
        World world,
        ActorWorld actorWorld,
        Entity entity,
        int actorTypeId)
    {
        DebugName = debugName;
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _actorWorld = actorWorld ?? throw new ArgumentNullException(nameof(actorWorld));
        _entity = entity;
        _actorTypeId = actorTypeId;
    }

    public string DebugName { get; }

    public void Apply(LayerRuntime runtime)
    {
        if (!runtime.IsOwnerThreadForActorWorld ||
            !ReferenceEquals(runtime.Actors, _actorWorld) ||
            !_world.TryGetProjectionMeta(_entity, out ProjectedActorMetaRef metaRef) ||
            !_world.Has<ProjectedActorRef>(_entity))
        {
            return;
        }

        ref ProjectedActorMeta meta = ref metaRef.Value;
        if (meta.ActorId.IsValid || meta.ActorTypeId != _actorTypeId)
        {
            return;
        }

        ProjectedActorHandle handle =
            ProjectedActorTypeRegistry.CreateActorByTypeId(_actorWorld, _actorTypeId);
        if (!handle.IsValid)
        {
            return;
        }

        long nowTicks = Stopwatch.GetTimestamp();
        ref ProjectedActorRef actorRef = ref _world.Get<ProjectedActorRef>(_entity);
        ProjectedActorBindingUtility.Bind(ref meta, ref actorRef, handle.ActorId, nowTicks);
        _world.AddActiveProjectedActor(_entity, ref meta);
    }
}
