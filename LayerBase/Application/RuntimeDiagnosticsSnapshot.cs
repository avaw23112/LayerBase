using LayerBase.Scope;

namespace LayerBase;

public sealed class RuntimeDiagnosticsSnapshot
{
    public RuntimeDiagnosticsSnapshot(
        int runtimeId,
        int runtimeGeneration,
        RuntimeState state,
        long timestamp,
        ScopeDiagnosticsSnapshot[] scopes,
        MainActorDiagnosticsSnapshot mainActor,
        PayloadDiagnosticsSnapshot payloads)
    {
        RuntimeId = runtimeId;
        RuntimeGeneration = runtimeGeneration;
        State = state;
        Timestamp = timestamp;
        Scopes = scopes ?? Array.Empty<ScopeDiagnosticsSnapshot>();
        MainActor = mainActor;
        Payloads = payloads;
    }

    public int RuntimeId { get; }

    public int RuntimeGeneration { get; }

    public RuntimeState State { get; }

    public long Timestamp { get; }

    public ScopeDiagnosticsSnapshot[] Scopes { get; }

    public MainActorDiagnosticsSnapshot MainActor { get; }

    public PayloadDiagnosticsSnapshot Payloads { get; }
}

public readonly struct ScopeDiagnosticsSnapshot
{
    public ScopeDiagnosticsSnapshot(
        int scopeId,
        string scopeName,
        ScopeRuntimeState state,
        int ownerThreadId,
        long tickCount,
        long lastTickDurationTicks,
        long maxTickDurationTicks,
        int eventInboxCount,
        int eventInboxCapacity,
        long eventInboxAccepted,
        long eventInboxRejected,
        int eventInboxHighWatermark,
        int callInboxCount,
        int callInboxCapacity,
        long callInboxAccepted,
        long callInboxRejected,
        int callInboxHighWatermark,
        int postPending,
        int timerPending,
        int delayPending,
        int continuationPending,
        int workerJobsPending,
        int workerJobsRunning,
        EcsDiagnosticsSnapshot ecs,
        ToolDiagnosticsSnapshot tools,
        SnapDiagnosticsSnapshot snap,
        long faultCount,
        int completionInboxCount = 0,
        int faultInboxCount = 0,
        int faultInboxDropped = 0,
        int faultInboxMerged = 0,
        int faultInboxHighWatermark = 0)
    {
        ScopeId = scopeId;
        ScopeName = scopeName ?? string.Empty;
        State = state;
        OwnerThreadId = ownerThreadId;
        TickCount = tickCount;
        LastTickDurationTicks = lastTickDurationTicks;
        MaxTickDurationTicks = maxTickDurationTicks;
        EventInboxCount = eventInboxCount;
        EventInboxCapacity = eventInboxCapacity;
        EventInboxAccepted = eventInboxAccepted;
        EventInboxRejected = eventInboxRejected;
        EventInboxHighWatermark = eventInboxHighWatermark;
        CallInboxCount = callInboxCount;
        CallInboxCapacity = callInboxCapacity;
        CallInboxAccepted = callInboxAccepted;
        CallInboxRejected = callInboxRejected;
        CallInboxHighWatermark = callInboxHighWatermark;
        PostPending = postPending;
        TimerPending = timerPending;
        DelayPending = delayPending;
        ContinuationPending = continuationPending;
        WorkerJobsPending = workerJobsPending;
        WorkerJobsRunning = workerJobsRunning;
        Ecs = ecs;
        Tools = tools;
        Snap = snap;
        FaultCount = faultCount;
        CompletionInboxCount = completionInboxCount;
        FaultInboxCount = faultInboxCount;
        FaultInboxDropped = faultInboxDropped;
        FaultInboxMerged = faultInboxMerged;
        FaultInboxHighWatermark = faultInboxHighWatermark;
    }

    public int ScopeId { get; }
    public string ScopeName { get; }
    public ScopeRuntimeState State { get; }
    public int OwnerThreadId { get; }
    public long TickCount { get; }
    public long LastTickDurationTicks { get; }
    public long MaxTickDurationTicks { get; }
    public int EventInboxCount { get; }
    public int EventInboxCapacity { get; }
    public long EventInboxAccepted { get; }
    public long EventInboxRejected { get; }
    public int EventInboxHighWatermark { get; }
    public int CallInboxCount { get; }
    public int CallInboxCapacity { get; }
    public long CallInboxAccepted { get; }
    public long CallInboxRejected { get; }
    public int CallInboxHighWatermark { get; }
    public int PostPending { get; }
    public int TimerPending { get; }
    public int DelayPending { get; }
    public int ContinuationPending { get; }
    public int WorkerJobsPending { get; }
    public int WorkerJobsRunning { get; }
    public EcsDiagnosticsSnapshot Ecs { get; }
    public ToolDiagnosticsSnapshot Tools { get; }
    public SnapDiagnosticsSnapshot Snap { get; }
    public long FaultCount { get; }
    public int CompletionInboxCount { get; }
    public int FaultInboxCount { get; }
    public int FaultInboxDropped { get; }
    public int FaultInboxMerged { get; }
    public int FaultInboxHighWatermark { get; }
}

public readonly struct EcsDiagnosticsSnapshot
{
    public EcsDiagnosticsSnapshot(
        int entityCount,
        bool queryBatchEnabled,
        int lastQueryBatchCount,
        int lastQueryEntityCount,
        int commandBufferSize,
        long structuralPlaybackCount)
    {
        EntityCount = entityCount;
        QueryBatchEnabled = queryBatchEnabled;
        LastQueryBatchCount = lastQueryBatchCount;
        LastQueryEntityCount = lastQueryEntityCount;
        CommandBufferSize = commandBufferSize;
        StructuralPlaybackCount = structuralPlaybackCount;
    }

    public int EntityCount { get; }
    public bool QueryBatchEnabled { get; }
    public int LastQueryBatchCount { get; }
    public int LastQueryEntityCount { get; }
    public int CommandBufferSize { get; }
    public long StructuralPlaybackCount { get; }
}

public readonly struct ToolDiagnosticsSnapshot
{
    public ToolDiagnosticsSnapshot(int registeredCount, int cachedCount, int createdCount, int createFailureCount)
    {
        RegisteredCount = registeredCount;
        CachedCount = cachedCount;
        CreatedCount = createdCount;
        CreateFailureCount = createFailureCount;
    }

    public int RegisteredCount { get; }
    public int CachedCount { get; }
    public int CreatedCount { get; }
    public int CreateFailureCount { get; }
}

public readonly struct MainActorDiagnosticsSnapshot
{
    public MainActorDiagnosticsSnapshot(
        MainActorRuntimeState state,
        int actorCount,
        int pendingMailCount,
        int pendingCallCount,
        int pendingLifecycleCount,
        int pendingDestroyCount,
        long pumpCount,
        long lastPumpDurationTicks,
        long faultCount)
    {
        State = state;
        ActorCount = actorCount;
        PendingMailCount = pendingMailCount;
        PendingCallCount = pendingCallCount;
        PendingLifecycleCount = pendingLifecycleCount;
        PendingDestroyCount = pendingDestroyCount;
        PumpCount = pumpCount;
        LastPumpDurationTicks = lastPumpDurationTicks;
        FaultCount = faultCount;
    }

    public MainActorRuntimeState State { get; }
    public int ActorCount { get; }
    public int PendingMailCount { get; }
    public int PendingCallCount { get; }
    public int PendingLifecycleCount { get; }
    public int PendingDestroyCount { get; }
    public long PumpCount { get; }
    public long LastPumpDurationTicks { get; }
    public long FaultCount { get; }
}

public enum MainActorRuntimeState : byte
{
    Created = 0,
    Running = 1,
    Stopping = 2,
    Stopped = 3,
    Disposed = 4
}

public readonly struct PayloadDiagnosticsSnapshot
{
    public PayloadDiagnosticsSnapshot(long rented, long returned, long outstanding, long peakOutstanding)
    {
        Rented = rented;
        Returned = returned;
        Outstanding = outstanding;
        PeakOutstanding = peakOutstanding;
    }

    public long Rented { get; }
    public long Returned { get; }
    public long Outstanding { get; }
    public long PeakOutstanding { get; }

    public static PayloadDiagnosticsSnapshot Sum(
        PayloadDiagnosticsSnapshot left,
        PayloadDiagnosticsSnapshot right)
    {
        return new PayloadDiagnosticsSnapshot(
            left.Rented + right.Rented,
            left.Returned + right.Returned,
            left.Outstanding + right.Outstanding,
            left.PeakOutstanding + right.PeakOutstanding);
    }
}

public readonly struct SnapDiagnosticsSnapshot
{
    public SnapDiagnosticsSnapshot(
        ScopeSafePointState state,
        int nodeCount,
        long serializeCount,
        long deserializeCount,
        long failureCount,
        long lastDurationTicks = 0,
        long lastBytes = 0)
    {
        State = state;
        NodeCount = nodeCount;
        SerializeCount = serializeCount;
        DeserializeCount = deserializeCount;
        FailureCount = failureCount;
        LastDurationTicks = lastDurationTicks;
        LastBytes = lastBytes;
    }

    public ScopeSafePointState State { get; }
    public int NodeCount { get; }
    public long SerializeCount { get; }
    public long DeserializeCount { get; }
    public long FailureCount { get; }
    public long LastDurationTicks { get; }
    public long LastBytes { get; }
}
