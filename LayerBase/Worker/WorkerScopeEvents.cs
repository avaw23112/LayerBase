using LayerBase.Core.Event;
using LayerBase.Scope;

namespace LayerBase.Worker;

internal static class WorkerScopeEventRouteIds
{
    public const int Result = -301;
    public const int Failure = -302;
}

internal interface IWorkerEventJobResult
{
    Type EventType { get; }

    PostResult PostTo(PostScheduler scheduler);
}

internal sealed class WorkerEventJobResult<TEvent> : IWorkerEventJobResult
    where TEvent : struct
{
    private readonly TEvent _value;

    public WorkerEventJobResult(TEvent value)
    {
        _value = value;
    }

    public Type EventType => typeof(TEvent);

    public PostResult PostTo(PostScheduler scheduler)
    {
        return scheduler.TryPost(_value);
    }
}

internal readonly struct WorkerEventJobResultScopeEvent
{
    public WorkerEventJobResultScopeEvent(WorkerHandle handle, IWorkerEventJobResult result)
    {
        Handle = handle;
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    public WorkerHandle Handle { get; }

    public IWorkerEventJobResult Result { get; }
}

internal readonly struct WorkerEventJobResultScopeEvent<TEvent>
    where TEvent : struct
{
    public WorkerEventJobResultScopeEvent(WorkerHandle handle, TEvent value, EventPostPolicy? postPolicy)
    {
        Handle = handle;
        Value = value;
        PostPolicy = postPolicy;
    }

    public WorkerHandle Handle { get; }

    public TEvent Value { get; }

    public EventPostPolicy? PostPolicy { get; }
}

internal readonly struct WorkerEventJobFailedScopeEvent
{
    public WorkerEventJobFailedScopeEvent(
        WorkerHandle handle,
        WorkerJobFailureKind kind,
        WorkerJobExceptionInfo error)
    {
        Handle = handle;
        Kind = kind;
        Error = error;
    }

    public WorkerHandle Handle { get; }

    public WorkerJobFailureKind Kind { get; }

    public WorkerJobExceptionInfo Error { get; }
}

internal static class WorkerScopeEventDispatcher
{
    public static bool TryDispatch(
        int routeId,
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage,
        PostScheduler? scheduler)
    {
        if (routeId == WorkerScopeEventRouteIds.Result)
        {
            DispatchResult(runtimeId, payload, payloadStorage, scheduler);
            return true;
        }

        if (routeId == WorkerScopeEventRouteIds.Failure)
        {
            DispatchFailure(runtimeId, payload, payloadStorage, scheduler);
            return true;
        }

        return false;
    }

    private static void DispatchResult(
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage,
        PostScheduler? scheduler)
    {
        if (scheduler == null)
            return;

        if (!payloadStorage.TryGet<WorkerEventJobResultScopeEvent>(
                runtimeId,
                payload,
                out var resultEvent))
        {
            return;
        }

        _ = resultEvent.Result.PostTo(scheduler);
    }

    private static void DispatchFailure(
        int runtimeId,
        PayloadHandle payload,
        EventPayloadStorage payloadStorage,
        PostScheduler? scheduler)
    {
        if (scheduler == null)
            return;

        if (!payloadStorage.TryGet<WorkerEventJobFailedScopeEvent>(
                runtimeId,
                payload,
                out var failedEvent))
        {
            return;
        }

        _ = scheduler.TryPost(new WorkerJobFailedEvent(
            failedEvent.Handle,
            failedEvent.Kind,
            failedEvent.Error));
    }
}
