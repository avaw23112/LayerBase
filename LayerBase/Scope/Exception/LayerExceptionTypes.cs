namespace LayerBase.Scope;

// ───────── Phase 定义 ─────────

/// <summary>
/// 标记异常发生在哪个阶段。
/// </summary>
public enum LayerExceptionPhase
{
    ServiceCreate,
    ServiceStart,
    ServiceStop,
    ServiceDispose,

    EventDispatch,
    PostScheduler,
    TimeScheduler,

    PostDispatch,
    CallDispatch,
    CallSetResult,
    CallSetException,

    Continuation,
    AwaiterRegister,

    EcsQuery,
    EcsCommandApply,
    EcsSystemTick,

    ActorEventFlush,
    ActorEventApply,

    QueueOverflow,
    WorkerLoop,
    RuntimeStart,
    RuntimeStop,
    RuntimeShutdown,
    ResourceUnbind,

    Unknown
}

/// <summary>
/// 标记哪个队列发生问题。
/// </summary>
public enum LayerQueueKind
{
    None,
    PostInbox,
    CallInbox,
    ContinuationQueue,
    ActorEventOutbox,
    ExceptionQueue,
    PostSchedulerQueue,
    TimeSchedulerQueue
}

// ───────── 异常策略 ─────────

/// <summary>
/// 异常处理策略。
/// </summary>
public enum LayerExceptionPolicy
{
    ReportAndContinue,
    StopScope,
    StopRuntime,
    RethrowOnMainScope,
    FailFast
}

// ───────── 异常选项 ─────────

/// <summary>
/// 按阶段配置的异常策略。
/// </summary>
public sealed class LayerExceptionOptions
{
    public LayerExceptionPolicy ServiceStartPolicy { get; set; } = LayerExceptionPolicy.StopScope;
    public LayerExceptionPolicy ServiceStopPolicy { get; set; } = LayerExceptionPolicy.ReportAndContinue;
    public LayerExceptionPolicy PostDispatchPolicy { get; set; } = LayerExceptionPolicy.ReportAndContinue;
    public LayerExceptionPolicy CallDispatchPolicy { get; set; } = LayerExceptionPolicy.ReportAndContinue;
    public LayerExceptionPolicy ContinuationPolicy { get; set; } = LayerExceptionPolicy.StopScope;
    public LayerExceptionPolicy TimeSchedulerPolicy { get; set; } = LayerExceptionPolicy.StopScope;
    public LayerExceptionPolicy EcsQueryPolicy { get; set; } = LayerExceptionPolicy.StopScope;
    public LayerExceptionPolicy ActorEventFlushPolicy { get; set; } = LayerExceptionPolicy.StopScope;
    public LayerExceptionPolicy QueueOverflowPolicy { get; set; } = LayerExceptionPolicy.StopScope;
    public LayerExceptionPolicy WorkerLoopPolicy { get; set; } = LayerExceptionPolicy.StopScope;

    public LayerExceptionPolicy GetPolicy(LayerExceptionPhase phase)
    {
        return phase switch
        {
            LayerExceptionPhase.ServiceStart => ServiceStartPolicy,
            LayerExceptionPhase.ServiceStop => ServiceStopPolicy,
            LayerExceptionPhase.PostDispatch => PostDispatchPolicy,
            LayerExceptionPhase.CallDispatch => CallDispatchPolicy,
            LayerExceptionPhase.Continuation => ContinuationPolicy,
            LayerExceptionPhase.TimeScheduler => TimeSchedulerPolicy,
            LayerExceptionPhase.EcsQuery => EcsQueryPolicy,
            LayerExceptionPhase.ActorEventFlush => ActorEventFlushPolicy,
            LayerExceptionPhase.QueueOverflow => QueueOverflowPolicy,
            LayerExceptionPhase.WorkerLoop => WorkerLoopPolicy,
            _ => LayerExceptionPolicy.ReportAndContinue
        };
    }
}
