namespace LayerBase.Worker;

public readonly struct WorkerJobAccessor
{
    private readonly WorkerJobScheduler? _scheduler;
    private readonly WorkerJobOrigin _origin;

    internal WorkerJobAccessor(WorkerJobScheduler scheduler, WorkerJobOrigin origin)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _origin = origin;
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
        if (_scheduler == null)
            throw new InvalidOperationException("Worker job accessor is not initialized.");
        if (!_origin.CanSubmit)
            return WorkerHandle.Invalid;

        return _scheduler.Run<TJob, TInput, TEvent>(
            in job,
            in input,
            in _origin,
            options,
            cancellationToken);
    }
}
