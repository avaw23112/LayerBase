namespace LayerBase.Actor;

internal sealed class ActorDelayScheduler
{
    private readonly ActorTimeWheel _timeWheel;

    public bool HasPending => _timeWheel.HasPending;

    public ActorDelayScheduler(
        ActorWorld            world,
        ActorTimeWheelOptions options)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        _timeWheel = new ActorTimeWheel(options);
    }

    public DelayPostHandle Schedule(IActorDelayTask task, float delaySeconds)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        return _timeWheel.Schedule(this, task, delaySeconds);
    }

    public void Tick(float deltaTime)
    {
        _timeWheel.Tick(deltaTime);
    }

    public void Cancel(int taskId)
    {
        _timeWheel.Cancel(taskId);
    }

    public void Clear()
    {
        _timeWheel.Clear();
    }
}