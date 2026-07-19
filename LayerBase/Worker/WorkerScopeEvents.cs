using LayerBase.Core.Event;
using LayerBase.Scope;

namespace LayerBase.Worker;

internal enum WorkerExecutionCompletionKind : byte
{
    Succeeded = 0,
    Cancelled = 1,
    Faulted = 2
}

internal interface IWorkerExecutionResult
{
    Type EventType { get; }

    PostResult PostTo(
        PostScheduler scheduler,
        EventPostPolicy? policy);
}

internal sealed class WorkerExecutionResult<TEvent> : IWorkerExecutionResult
    where TEvent : struct
{
    private readonly TEvent _value;

    public WorkerExecutionResult(in TEvent value)
    {
        _value = value;
    }

    public Type EventType => typeof(TEvent);

    public PostResult PostTo(
        PostScheduler scheduler,
        EventPostPolicy? policy)
    {
        if (!policy.HasValue)
            return scheduler.TryPost(in _value);

        return policy.Value.Mode switch
        {
            PostDeliveryMode.Normal => scheduler.TryPost(in _value),
            PostDeliveryMode.Latest => scheduler.TryPostLatest(in _value),
            PostDeliveryMode.Coalesced => scheduler.TryPostCoalesced(in _value),
            PostDeliveryMode.DirtySignal => scheduler.TryPost(in _value),
            _ => PostResult.Failure()
        };
    }
}

internal readonly struct WorkerExecutionCompletedScopeEvent
{
    public WorkerExecutionCompletedScopeEvent(
        WorkerHandle handle,
        WorkerExecutionCompletionKind kind,
        IWorkerExecutionResult? result,
        WorkerEventJobOptions options,
        WorkerJobExceptionInfo error)
    {
        Handle = handle;
        Kind = kind;
        Result = result;
        Options = options;
        Error = error;
    }

    public WorkerHandle Handle { get; }

    public WorkerExecutionCompletionKind Kind { get; }

    public IWorkerExecutionResult? Result { get; }

    public WorkerEventJobOptions Options { get; }

    public WorkerJobExceptionInfo Error { get; }
}
