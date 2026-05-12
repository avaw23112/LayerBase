namespace LayerBase.Actor;

public readonly struct DelayPostHandle
{
    private readonly ActorDelayScheduler? _scheduler;
    private readonly int _taskId;

    internal DelayPostHandle(ActorDelayScheduler scheduler, int taskId)
    {
        _scheduler = scheduler;
        _taskId = taskId;
    }

    public bool IsValid => _scheduler != null && _taskId != 0;

    public void Cancel()
    {
        _scheduler?.Cancel(_taskId);
    }
}