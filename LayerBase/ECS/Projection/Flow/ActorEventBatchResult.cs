using LayerBase.Actor;
using LayerBase.ECS.Runtime;

namespace LayerBase.ECS.Projection.Flow;

internal sealed class ActorEventBatchResult<TEvent> : IEcsResultItem, IDisposable
    where TEvent : struct
{
    private ProjectionBatchBuffer<TEvent> _batch;

    public ActorEventBatchResult(string debugName, ProjectionBatchBuffer<TEvent> batch)
    {
        DebugName = debugName;
        _batch = batch;
    }

    public string DebugName { get; }

    public void Apply(LayerRuntime runtime)
    {
        try
        {
            ActorWorld actorWorld = runtime.Actors;
            _batch.PostTo(actorWorld);
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
