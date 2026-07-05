namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    internal void PrepareRuntimeBuild()
    {
        if (_state == ActorWorldState.Disposed)
        {
            throw new ObjectDisposedException(nameof(ActorWorld));
        }

        _state = ActorWorldState.Building;
    }

    internal void CompleteRuntimeBuild()
    {
        if (_state == ActorWorldState.Disposed)
        {
            throw new ObjectDisposedException(nameof(ActorWorld));
        }

        _state = ActorWorldState.Running;
    }

    internal void RuntimeStop()
    {
        if (_state == ActorWorldState.Disposed)
        {
            return;
        }

        _state = ActorWorldState.Stopping;
        DelayScheduler.Clear();
    }

    public void Dispose()
    {
        if (_state == ActorWorldState.Disposed)
        {
            return;
        }

        _state = ActorWorldState.Disposed;
        DelayScheduler.Clear();
        _queryCacheByDescriptor.Clear();
        _callBucketsByRouteId = Array.Empty<IActorCallBucket>();

        foreach (Action unbind in _eventStreamUnbinders)
        {
            unbind();
        }

        _eventStreamUnbinders.Clear();
        _eventStreamRuntimes.Clear();
        GlobalEventMailPools.Clear();
        ActorWorldRuntimeIndexAllocator.Return(RuntimeIndex);
    }

    public bool IsEnable(ActorId actorId)
    {
        if (!CanUseWorldFast())
        {
            return false;
        }

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId].IsEnable(actorId);
    }

    public bool SetEnable(ActorId actorId, bool enable)
    {
        if (!CanUseWorldFast())
        {
            return false;
        }

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId].SetEnable(actorId, enable);
    }
}
