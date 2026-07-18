namespace LayerBase.Worker;

public readonly struct WorkerJobAccessor
{
    private readonly WorkerJobCoordinator? _coordinator;

    internal WorkerJobAccessor(WorkerJobCoordinator coordinator)
    {
        _coordinator = coordinator ??
            throw new ArgumentNullException(nameof(coordinator));
    }

    public WorkerHandle Run<TJob, TInput, TEvent>(
        in TJob job,
        in TInput input,
        WorkerEventJobOptions options = default,
        CancellationToken cancellationToken = default)
        where TJob : struct, IWorkerEventJob<TInput, TEvent>
        where TInput : struct
        where TEvent : struct
    {
        if (_coordinator == null)
        {
            throw new InvalidOperationException(
                "Worker job accessor is not initialized.");
        }

        return _coordinator.Run<TJob, TInput, TEvent>(
            in job,
            in input,
            options,
            cancellationToken);
    }
}
