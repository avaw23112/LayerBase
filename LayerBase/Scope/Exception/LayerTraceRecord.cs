using System.Runtime.ExceptionServices;

namespace LayerBase.Scope;

// ───────── ScopeTrace ─────────

/// <summary>
/// 跨 Scope 通讯追踪信息。
/// </summary>
public readonly struct ScopeTrace
{
    public static readonly ScopeTrace Empty = new(0, 0, -1, -1, 0);

    public readonly long TraceId;
    public readonly long ParentTraceId;
    public readonly int SourceScopeId;
    public readonly int TargetScopeId;
    public readonly long SourceTick;

    public ScopeTrace(long traceId, long parentTraceId, int sourceScopeId, int targetScopeId, long sourceTick)
    {
        TraceId = traceId;
        ParentTraceId = parentTraceId;
        SourceScopeId = sourceScopeId;
        TargetScopeId = targetScopeId;
        SourceTick = sourceTick;
    }
}

// ───────── Trace Factory ─────────

/// <summary>
/// 全局递增 TraceId 生成器，由 LayerRuntime 持有。
/// </summary>
public sealed class ScopeTraceFactory
{
    private long _nextTraceId;

    public long NextTraceId()
    {
        return Interlocked.Increment(ref _nextTraceId);
    }
}

// ───────── LayerExceptionRecord ─────────

/// <summary>
/// 异常完整上下文记录。
/// </summary>
public readonly struct LayerExceptionRecord
{
    public readonly Exception Exception;
    public readonly ExceptionDispatchInfo DispatchInfo;
    public readonly int ScopeId;
    public readonly int ServiceId;
    public readonly LayerExceptionPhase Phase;
    public readonly LayerQueueKind QueueKind;
    public readonly int MessageId;
    public readonly ScopeTrace Trace;
    public readonly int ThreadId;
    public readonly long Tick;
    public readonly int QueueCapacity;
    public readonly int QueueCount;
    public readonly int LayerIndex;
    public readonly string? Source;
    public readonly string? EventName;

    public LayerExceptionRecord(
        Exception exception,
        int scopeId,
        int serviceId,
        LayerExceptionPhase phase,
        LayerQueueKind queueKind,
        int messageId,
        ScopeTrace trace,
        int threadId,
        long tick,
        int queueCapacity,
        int queueCount,
        int layerIndex = -1,
        string? source = null,
        string? eventName = null)
    {
        Exception = exception;
        DispatchInfo = ExceptionDispatchInfo.Capture(exception);
        ScopeId = scopeId;
        ServiceId = serviceId;
        Phase = phase;
        QueueKind = queueKind;
        MessageId = messageId;
        Trace = trace;
        ThreadId = threadId;
        Tick = tick;
        QueueCapacity = queueCapacity;
        QueueCount = queueCount;
        LayerIndex = layerIndex;
        Source = source;
        EventName = eventName;
    }
}

// ───────── Queue Overflow Exception ─────────

/// <summary>
/// 队列满异常。必须进入异常通道，不能静默丢。
/// </summary>
public sealed class LayerBaseQueueOverflowException : Exception
{
    public int ScopeId { get; }
    public LayerQueueKind QueueKind { get; }
    public int Capacity { get; }
    public int Count { get; }

    public LayerBaseQueueOverflowException(
        int scopeId,
        LayerQueueKind queueKind,
        int capacity,
        int count)
        : base($"LayerBase queue overflow. Scope={scopeId}, Queue={queueKind}, Count={count}, Capacity={capacity}.")
    {
        ScopeId = scopeId;
        QueueKind = queueKind;
        Capacity = capacity;
        Count = count;
    }
}

// ───────── LayerContinuation（替代裸 Action）─────────

/// <summary>
/// 带调试上下文的异步延续。
/// </summary>
public readonly struct LayerContinuation
{
    public readonly Action Action;
    public readonly int ServiceId;
    public readonly int TaskId;
    public readonly ScopeTrace Trace;

    public LayerContinuation(Action action, int serviceId, int taskId, ScopeTrace trace)
    {
        Action = action;
        ServiceId = serviceId;
        TaskId = taskId;
        Trace = trace;
    }
}
