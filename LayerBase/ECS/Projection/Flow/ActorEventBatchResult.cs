using LayerBase.Actor;
using LayerBase.ECS.Runtime;

namespace LayerBase.ECS.Projection.Flow;

internal sealed class ActorEventBatchResult<TEvent> : IEcsResultItem, IDisposable
    where TEvent : struct
{
    private readonly ActorWorld _actorWorld;
    private ProjectionBatchBuffer<TEvent> _batch;

    public ActorEventBatchResult(
        string debugName,
        ProjectionBatchBuffer<TEvent> batch,
        ActorWorld actorWorld)
    {
        DebugName = debugName;
        _batch = batch;
        _actorWorld = actorWorld ?? throw new ArgumentNullException(nameof(actorWorld));
    }

    public string DebugName { get; }

    public void Apply(LayerRuntime runtime)
    {
        try
        {
            if (ReferenceEquals(runtime.Actors, _actorWorld) &&
                !runtime.IsOwnerThreadForActorWorld)
            {
                _batch.PostToRuntimeOwner(runtime);
                return;
            }

            _batch.PostTo(_actorWorld);
        }
        finally
        {
            _batch.Dispose();
        }
    }

    public void Dispose()
    {
        _batch.Dispose();
    }
}
