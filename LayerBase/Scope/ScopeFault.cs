using System.Diagnostics;

namespace LayerBase.Scope;

public enum ScopeFaultPhase
{
    Activate,
    ServiceInitialize,
    ServiceUpdate,
    ServiceStop,
    ServiceDispose,
    Snapshot,
    ContextInitialize,
    ContextUpdate,
    ContextDispose,
    EventDispatch,
    CallDispatch,
    CallResponseApply,
    SynchronizationContext,
    Continuation,
    Timer,
    Delay,
    PostScheduler,
    EcsSubmit,
    EcsExecute,
    EcsResultApply,
    ActorEventEncode,
    ActorEventApply,
    WorkerLoop,
    QueueAdmission,
    ResourceUnbind,
    Shutdown,
    Unknown
}

public enum ScopeFaultPolicy
{
    ReportAndContinue = 0,
    StopScope = 1,
    StopRuntime = 2
}

public enum ScopeFaultAction
{
    Continue = 0,
    StopScope = 1,
    StopRuntime = 2
}

public readonly struct ScopeFaultContext
{
    public ScopeFaultContext(ScopeFaultRecord record)
    {
        Record = record;
    }

    public ScopeFaultRecord Record { get; }
}

public interface IScopeFaultPolicy
{
    ScopeFaultAction OnFault(in ScopeFaultContext context);
}

public readonly struct ScopeFaultRecord
{
    public ScopeFaultRecord(
        int runtimeId,
        int runtimeGeneration,
        int sourceScopeId,
        ScopeFaultPhase phase,
        Exception exception,
        int routeId = 0,
        int serviceSlot = -1,
        int contextSlot = -1,
        long timestamp = 0)
    {
        RuntimeId = runtimeId;
        RuntimeGeneration = runtimeGeneration;
        SourceScopeId = sourceScopeId;
        Phase = phase;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        RouteId = routeId;
        ServiceSlot = serviceSlot;
        ContextSlot = contextSlot;
        Timestamp = timestamp == 0 ? Stopwatch.GetTimestamp() : timestamp;
    }

    public int RuntimeId { get; }

    public int RuntimeGeneration { get; }

    public int SourceScopeId { get; }

    public ScopeFaultPhase Phase { get; }

    public Exception Exception { get; }

    public int RouteId { get; }

    public int ServiceSlot { get; }

    public int ContextSlot { get; }

    public long Timestamp { get; }
}

public readonly struct ScopeFaultInfo
{
    public ScopeFaultInfo(ScopeFaultRecord record)
    {
        Record = record;
    }

    public ScopeFaultRecord Record { get; }
}

internal readonly struct ScopeFaultEvent
{
    public ScopeFaultEvent(ScopeFaultRecord record)
    {
        Record = record;
    }

    public ScopeFaultRecord Record { get; }
}

internal static class ScopeFaultRouteIds
{
    public const int FaultEvent = -201;
}
