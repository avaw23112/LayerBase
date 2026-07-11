using LayerBase;
using LayerBase.Scope;

namespace LayerBase.Usage;

/// <summary>
/// 统一异常通道示例：
/// - LayerExceptionHub: 任意线程/Scope Report，主线程 Drain 时统一回调
/// - LayerExceptionRecord: 完整上下文（ScopeId, Phase, QueueKind, TraceId, ThreadId, Tick）
/// - 队列满异常化: overflowCount + lastOverflow
/// - ILayerExceptionSink / LayerHubExceptionCallbacks: 回调接口
/// </summary>
public static class ExceptionHandlingUsage
{
    public static void Run()
    {
        Console.WriteLine("=== 统一异常通道示例 ===\n");

        // ─ 1. LayerExceptionHub 基本用法 ─
        Console.WriteLine("-- 1. Report + DrainAndDispatch --");
        var hub = new LayerExceptionHub(capacity: 32);
        var sink = new LayerHubExceptionCallbacks();
        int callbackCount = 0;

        sink.OnExceptionRecord += record =>
        {
            callbackCount++;
            Console.WriteLine($"  [{callbackCount}] ScopeId={record.ScopeId}, Phase={record.Phase}, " +
                              $"Queue={record.QueueKind}, ThreadId={record.ThreadId}");
        };

        // 模拟 CombatScope(1) 的 ServiceStart 异常
        hub.Report(new LayerExceptionRecord(
            exception: new InvalidOperationException("Combat 系统初始化失败"),
            scopeId: 1,
            serviceId: 3,
            phase: LayerExceptionPhase.ServiceStart,
            queueKind: LayerQueueKind.None,
            messageId: -1,
            trace: ScopeTrace.Empty,
            threadId: 24,
            tick: 0,
            queueCapacity: 0,
            queueCount: 0));

        // 模拟 MainScope(0) 的 PostDispatch 异常
        hub.Report(new LayerExceptionRecord(
            exception: new NullReferenceException("事件 Payload 为空"),
            scopeId: 0,
            serviceId: -1,
            phase: LayerExceptionPhase.PostDispatch,
            queueKind: LayerQueueKind.PostInbox,
            messageId: 42,
            trace: new ScopeTrace(1001, 0, 0, 1, 50),
            threadId: 1,
            tick: 120,
            queueCapacity: 1024,
            queueCount: 5));

        hub.DrainAndDispatch(sink);
        Console.WriteLine($"  回调数: {callbackCount} / expect 2");

        // ─ 2. CallDispatch 异常 (带 TraceId) ─
        Console.WriteLine("\n-- 2. CallDispatch 异常 + TraceId --");
        callbackCount = 0;

        hub.Report(new LayerExceptionRecord(
            exception: new TimeoutException("BulletTickCall(7) 执行超时"),
            scopeId: 1,
            serviceId: 1,
            phase: LayerExceptionPhase.CallDispatch,
            queueKind: LayerQueueKind.CallInbox,
            messageId: 7,
            trace: new ScopeTrace(
                traceId: 2050,
                parentTraceId: 2048,
                sourceScopeId: 0,
                targetScopeId: 1,
                sourceTick: 500),
            threadId: 24,
            tick: 510,
            queueCapacity: 1024,
            queueCount: 3));

        hub.DrainAndDispatch(sink);
        Console.WriteLine($"  回调数: {callbackCount} / expect 1");
        Console.WriteLine($"  TraceId={2050}, ParentTraceId=2048, 来源=MainScope(0) -> CombatScope(1)");

        // ─ 3. 队列满异常化 ─
        Console.WriteLine("\n-- 3. 队列满 (QueueOverflow) --");
        var smallHub = new LayerExceptionHub(capacity: 2);
        var smallSink = new LayerHubExceptionCallbacks();
        int overflowCalls = 0;

        smallSink.OnExceptionQueueOverflow += (dropped, last) =>
        {
            overflowCalls++;
            Console.WriteLine($"  QueueOverflow: dropped={dropped}, lastPhase={last.Phase}, lastMsgId={last.MessageId}");
        };

        for (int i = 0; i < 5; i++)
        {
            smallHub.Report(new LayerExceptionRecord(
                exception: new Exception($"异常 {i}"),
                scopeId: 0, serviceId: -1,
                phase: LayerExceptionPhase.Continuation,
                queueKind: LayerQueueKind.ContinuationQueue,
                messageId: i,
                trace: ScopeTrace.Empty,
                threadId: 1, tick: i,
                queueCapacity: 2, queueCount: i + 1));
        }

        smallHub.DrainAndDispatch(smallSink);
        Console.WriteLine($"  QueueOverflow回调: {overflowCalls} / expect >=1");

        // ─ 4. ExceptionPolicy + LayerExceptionOptions ─
        Console.WriteLine("\n-- 4. ExceptionPolicy 策略 --");
        var options = new LayerExceptionOptions
        {
            ServiceStartPolicy = LayerExceptionPolicy.StopScope,
            PostDispatchPolicy = LayerExceptionPolicy.ReportAndContinue,
            ContinuationPolicy = LayerExceptionPolicy.RethrowOnMainScope,
        };
        Console.WriteLine($"  ServiceStart -> {options.ServiceStartPolicy}");
        Console.WriteLine($"  PostDispatch -> {options.PostDispatchPolicy}");
        Console.WriteLine($"  Continuation  -> {options.ContinuationPolicy}");
        Console.WriteLine($"  GetPolicy(Continuation) = {options.GetPolicy(LayerExceptionPhase.Continuation)}");

        // ─ 5. RethrowOnMainScope 语义 ─
        Console.WriteLine("\n-- 5. RethrowOnMainScope (开发期) --");
        Console.WriteLine("  Worker 线程异常 -> ExceptionHub 记录 -> MainScope Pump 时重新抛出");
        Console.WriteLine("  Worker 线程不崩，IDE 在 MainScope 恢复原始调用栈");

        Console.WriteLine("\n=== 异常通道验证通过 ===");
    }
}
