namespace LayerBase.Actor;

internal sealed class ActorLifecycleScheduler
{
    private readonly ActorWorld _world;
    private readonly ActorLifecycleFreeList<IUpdate> _updates = new();
    private readonly ActorLifecycleFreeList<ILateUpdate> _lateUpdates = new();
    private readonly ActorLifecycleFreeList<IFixedUpdate> _fixedUpdates = new();

    /// <summary>
    /// 生命周期 Pump 时间检查间隔。
    /// 每处理多少个生命周期条目后检查一次时间预算。
    /// 默认值 64 表示每处理 64 个条目后检查一次时间预算。
    /// 值 1 表示每个条目都检查（旧行为）。
    /// </summary>
    public int TimeCheckInterval { get; set; } = 64;

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
            },
            timeCheckInterval: TimeCheckInterval);
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
            },
            timeCheckInterval: TimeCheckInterval);
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
            },
            timeCheckInterval: TimeCheckInterval);
    }
}