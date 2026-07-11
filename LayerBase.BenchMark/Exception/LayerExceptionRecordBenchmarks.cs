using System.Runtime.ExceptionServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using LayerBase.Scope;

namespace Benchmarks.Errors;

/// <summary>
/// LayerExceptionRecord 单片基准。
/// 把 Exception 创建 / DispatchInfo.Capture / Record 构造成本分开。
/// Category: 09.Exception.Record
/// </summary>
[MemoryDiagnoser]
[Config(typeof(RecordBenchConfig))]
[BenchmarkCategory("09.Exception.Record")]
public class LayerExceptionRecordBenchmarks
{
    private readonly Exception _existing = new InvalidOperationException("Benchmark");

    [Benchmark(Baseline = true, Description = "空基线")]
    [BenchmarkCategory("09.Exception.Record")]
    public void Baseline() { }

    [Benchmark(Description = "创建 Exception")]
    [BenchmarkCategory("09.Exception.Record")]
    public Exception CreateNewException()
    {
        return new InvalidOperationException("Benchmark");
    }

    [Benchmark(Description = "ExceptionDispatchInfo.Capture")]
    [BenchmarkCategory("09.Exception.Record")]
    public ExceptionDispatchInfo CaptureExceptionDispatchInfo()
    {
        return ExceptionDispatchInfo.Capture(_existing);
    }

    [Benchmark(Description = "创建 LayerExceptionRecord")]
    [BenchmarkCategory("09.Exception.Record")]
    public LayerExceptionRecord CreateLayerExceptionRecord()
    {
        return new LayerExceptionRecord(
            exception: _existing,
            scopeId: 1,
            serviceId: 3,
            phase: LayerExceptionPhase.CallDispatch,
            queueKind: LayerQueueKind.CallInbox,
            messageId: 7,
            trace: new ScopeTrace(1001, 1000, 0, 1, 500),
            threadId: Environment.CurrentManagedThreadId,
            tick: 500,
            queueCapacity: 0,
            queueCount: 0);
    }

    [Benchmark(Description = "创建 QueueOverflowException")]
    [BenchmarkCategory("09.Exception.Record")]
    public LayerBaseQueueOverflowException CreateQueueOverflowException()
    {
        return new LayerBaseQueueOverflowException(
            scopeId: 1,
            queueKind: LayerQueueKind.CallInbox,
            capacity: 1024,
            count: 1024);
    }

    /// <summary>
    /// 完整路径：新建 Exception + Record + DispatchInfo.Capture
    /// </summary>
    [Benchmark(Description = "创建 Exception + Record (完整路径)")]
    [BenchmarkCategory("09.Exception.Record")]
    public LayerExceptionRecord FullExceptionPipeline()
    {
        var ex = new InvalidOperationException("Full pipeline");
        return new LayerExceptionRecord(
            exception: ex,
            scopeId: 1,
            serviceId: 3,
            phase: LayerExceptionPhase.CallDispatch,
            queueKind: LayerQueueKind.CallInbox,
            messageId: 7,
            trace: new ScopeTrace(1001, 1000, 0, 1, 500),
            threadId: Environment.CurrentManagedThreadId,
            tick: 500,
            queueCapacity: 0,
            queueCount: 0);
    }
}

public sealed class RecordBenchConfig : ManualConfig
{
    public RecordBenchConfig()
    {
        AddJob(Job.ShortRun);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max);
    }
}
