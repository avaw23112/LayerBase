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

        _disposeRequested = true;
        _state = ActorWorldState.Stopping;
        DelayScheduler.Clear();

        MarkAllActorsPendingDestroy();
        DrainCompletionInbox();
        SweepPendingDestroy();
        TryFinalizeDeferredDispose();
    }

    private void TryFinalizeDeferredDispose()
    {
        if (!_disposeRequested
            || _state == ActorWorldState.Disposed
            || CountActiveOperations() > 0
            || _pendingDestroyCount > 0
            || CountCompletionInbox() > 0)
        {
            return;
        }

        FinalizeDispose();
    }

    private void FinalizeDispose()
    {
        if (_state == ActorWorldState.Disposed)
        {
            return;
        }

        _state = ActorWorldState.Disposed;
        _queryCacheByDescriptor.Clear();
        _callBucketsByRouteId = Array.Empty<IActorCallBucket>();

        foreach (Action unbind in _eventStreamUnbinders)
        {
            unbind();
        }

        _eventStreamUnbinders.Clear();
        _eventStreamRuntimes.Clear();
        GlobalEventMailPools.Clear();
        if (!_runtimeIndexReturned)
        {
            _runtimeIndexReturned = true;
            ActorWorldRuntimeIndexAllocator.Return(RuntimeIndex);
        }
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
