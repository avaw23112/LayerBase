namespace LayerBase.Actor;

internal sealed class ActorLifecycleScheduler
{
    private readonly ActorWorld _world;
    private readonly ActorLifecycleFreeList<IUpdate> _updates = new();
    private readonly ActorLifecycleFreeList<ILateUpdate> _lateUpdates = new();
    private readonly ActorLifecycleFreeList<IFixedUpdate> _fixedUpdates = new();

    public ActorLifecycleScheduler(ActorWorld world)
    {
        _world = world;
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

  
    public void PumpFixedUpdate(
        float                  fixedDeltaTime,
        ref RuntimeFrameBudget budget)
    {
        // fixedDeltaTime 参数表示固定逻辑步长。
        // budget 参数表示当前帧剩余预算。
        var state = new LifecycleFrameState(
            world: _world,
            deltaTime: fixedDeltaTime);

        _fixedUpdates.PumpBudgeted(
            state: ref state,
            budget: ref budget,
            invoker: static (
                IFixedUpdate instance,
                float        deltaTime) =>
            {
                // instance 参数表示具体 IFixedUpdate Actor。
                // deltaTime 参数表示固定逻辑步长。
                instance.FixedUpdate(deltaTime);
            });
    }

    public void PumpUpdate(
        float                  deltaTime,
        ref RuntimeFrameBudget budget)
    {
        // deltaTime 参数表示当前帧间隔。
        // budget 参数表示当前帧剩余预算。
        var state = new LifecycleFrameState(
            world: _world,
            deltaTime: deltaTime);

        _updates.PumpBudgeted(
            state: ref state,
            budget: ref budget,
            invoker: static (
                IUpdate instance,
                float   deltaTime) =>
            {
                // instance 参数表示具体 IUpdate Actor。
                // deltaTime 参数表示当前帧间隔。
                instance.Update(deltaTime);
            });
    }

    public void PumpLateUpdate(
        float                  deltaTime,
        ref RuntimeFrameBudget budget)
    {
        // deltaTime 参数表示当前帧间隔。
        // budget 参数表示当前帧剩余预算。
        var state = new LifecycleFrameState(
            world: _world,
            deltaTime: deltaTime);

        _lateUpdates.PumpBudgeted(
            state: ref state,
            budget: ref budget,
            invoker: static (
                ILateUpdate instance,
                float       deltaTime) =>
            {
                // instance 参数表示具体 ILateUpdate Actor。
                // deltaTime 参数表示当前帧间隔。
                instance.LateUpdate(deltaTime);
            });
    }
}
