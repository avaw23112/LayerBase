namespace LayerBase.Actor;

internal sealed class ActorLifecycleScheduler
{
    private readonly ActorWorld _world;
    private readonly ActorLifecycleFreeList<IStart> _starts = new();
    private readonly ActorLifecycleFreeList<IUpdate> _updates = new();
    private readonly ActorLifecycleFreeList<ILateUpdate> _lateUpdates = new();
    private readonly ActorLifecycleFreeList<IFixedUpdate> _fixedUpdates = new();

    public ActorLifecycleScheduler(ActorWorld world)
    {
        _world = world;
    }

    public ActorLifecycleHandle AddStart(ActorId actorId, IStart start)
    {
        return _starts.Add(actorId, start);
    }

    public ActorLifecycleHandle AddUpdate(ActorId actorId, IUpdate update)
    {
        return _updates.Add(actorId, update);
    }

    public ActorLifecycleHandle AddLateUpdate(ActorId actorId, ILateUpdate lateUpdate)
    {
        return _lateUpdates.Add(actorId, lateUpdate);
    }

    public ActorLifecycleHandle AddFixedUpdate(ActorId actorId, IFixedUpdate fixedUpdate)
    {
        return _fixedUpdates.Add(actorId, fixedUpdate);
    }

    public void RemoveStart(ActorLifecycleHandle handle)
    {
        _starts.Remove(handle);
    }

    public void RemoveUpdate(ActorLifecycleHandle handle)
    {
        _updates.Remove(handle);
    }

    public void RemoveLateUpdate(ActorLifecycleHandle handle)
    {
        _lateUpdates.Remove(handle);
    }

    public void RemoveFixedUpdate(ActorLifecycleHandle handle)
    {
        _fixedUpdates.Remove(handle);
    }

    public void PumpStart()
    {
        var state = new LifecycleFrameState(_world, 0f);

        _starts.ForEachRemoveIf(
            ref state,
            static (in ActorLifecycleEntry<IStart> entry, ref LifecycleFrameState frameState) =>
            {
                if (!frameState.World.IsAlive(entry.ActorId))
                {
                    return true;
                }

                entry.Instance.Start();
                return true;
            });
    }

    public void PumpFixedUpdate(float fixedDeltaTime)
    {
        var state = new LifecycleFrameState(_world, fixedDeltaTime);
        _fixedUpdates.ForEach(
            ref state,
            static (in ActorLifecycleEntry<IFixedUpdate> entry, ref LifecycleFrameState frameState) =>
            {
                if (!frameState.World.IsAlive(entry.ActorId) || !frameState.World.IsEnable(entry.ActorId))
                {
                    return;
                }

                entry.Instance.FixedUpdate(frameState.DeltaTime);
            });
    }

    public void PumpUpdate(float deltaTime)
    {
        var state = new LifecycleFrameState(_world, deltaTime);
        _updates.ForEach(
            ref state,
            static (in ActorLifecycleEntry<IUpdate> entry, ref LifecycleFrameState frameState) =>
            {
                if (!frameState.World.IsAlive(entry.ActorId) || !frameState.World.IsEnable(entry.ActorId))
                {
                    return;
                }

                entry.Instance.Update(frameState.DeltaTime);
            });
    }

    public void PumpLateUpdate(float deltaTime)
    {
        var state = new LifecycleFrameState(_world, deltaTime);
        _lateUpdates.ForEach(
            ref state,
            static (in ActorLifecycleEntry<ILateUpdate> entry, ref LifecycleFrameState frameState) =>
            {
                if (!frameState.World.IsAlive(entry.ActorId) || !frameState.World.IsEnable(entry.ActorId))
                {
                    return;
                }

                entry.Instance.LateUpdate(frameState.DeltaTime);
            });
    }
}
