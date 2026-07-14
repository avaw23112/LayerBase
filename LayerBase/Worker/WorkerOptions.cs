namespace LayerBase.Worker;

public sealed class WorkerOptions
{
    public WorkerOptions(
        int stateCapacity = 4096,
        int jobQueueCapacity = 4096,
        int eventQueueCapacity = 4096)
    {
        StateCapacity = Math.Max(1, stateCapacity);
        JobQueueCapacity = Math.Max(1, jobQueueCapacity);
        EventQueueCapacity = Math.Max(1, eventQueueCapacity);
    }

    public int StateCapacity { get; }

    public int JobQueueCapacity { get; }

    public int EventQueueCapacity { get; }

    public static WorkerOptions Default { get; } = new();
}
