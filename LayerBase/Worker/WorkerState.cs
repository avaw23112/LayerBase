namespace LayerBase.Worker;

public enum WorkerState
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
