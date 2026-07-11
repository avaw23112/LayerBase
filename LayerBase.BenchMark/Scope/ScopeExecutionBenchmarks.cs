using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Scope;

namespace Benchmarks.Scope;

/// <summary>
/// ScopeExecution 上下文基准：Enter/Exit、Current 读取、嵌套、AsyncLocal 传播。
/// Category: 08.Scope.ExecutionContext
/// </summary>
[MemoryDiagnoser]
[Config(typeof(ExecBenchConfig))]
[BenchmarkCategory("08.Scope.ExecutionContext")]
public class ScopeExecutionBenchmarks
{
    private ScopeRuntime? _scope;
    private ScopeRuntime? _scopeA;
    private ScopeRuntime? _scopeB;

    [GlobalSetup]
    public void Setup()
    {
        var desc = new ScopeDescriptor(0, "Main", ScopeThreadingMode.Inline, ScopeClockMode.EngineDriven, 0, ScopeStopPolicy.Drain);
        _scope = new ScopeRuntime(desc, Array.Empty<IService>());
        _scopeA = new ScopeRuntime(new ScopeDescriptor(1, "A", ScopeThreadingMode.Inline, ScopeClockMode.EngineDriven, 0, ScopeStopPolicy.Drain), Array.Empty<IService>());
        _scopeB = new ScopeRuntime(new ScopeDescriptor(2, "B", ScopeThreadingMode.Inline, ScopeClockMode.EngineDriven, 0, ScopeStopPolicy.Drain), Array.Empty<IService>());
    }

    [Benchmark(Baseline = true, Description = "空委托基线")]
    [BenchmarkCategory("08.Scope.ExecutionContext")]
    public void DirectDelegateBaseline()
    {
        action();
    }

    [Benchmark(Description = "ScopeExecution.Current 读取")]
    [BenchmarkCategory("08.Scope.ExecutionContext")]
    public void ScopeExecution_CurrentRead()
    {
        using (ScopeExecution.Enter(_scope!))
        {
            var frame = ScopeExecution.Current;
            Volatile.Write(ref BenchmarkSink.IntValue, frame.ScopeId);
        }
    }

    [Benchmark(Description = "ScopeExecution Enter/Exit")]
    [BenchmarkCategory("08.Scope.ExecutionContext")]
    public void ScopeExecution_EnterExit()
    {
        using (ScopeExecution.Enter(_scope!))
        {
            action();
        }
    }

    [Benchmark(Description = "ScopeExecution 嵌套 Depth=4")]
    [BenchmarkCategory("08.Scope.ExecutionContext")]
    public void ScopeExecution_NestedEnterExit_Depth4()
    {
        using (ScopeExecution.Enter(_scope!))
        using (ScopeExecution.Enter(_scopeA!))
        using (ScopeExecution.Enter(_scopeB!))
        using (ScopeExecution.Enter(_scope!))
        {
            action();
        }
    }

    [Benchmark(Description = "ScopeExecution Enter + SyncCtx 安装")]
    [BenchmarkCategory("08.Scope.ExecutionContext")]
    public void ScopeExecution_Enter_WithSynchronizationContext()
    {
        var ctx = LayerBaseSynchronizationContext.Install();
        using (ctx.EnterScope())
        using (ScopeExecution.Enter(_scope!))
        {
            action();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void action() { }
}

public sealed class ExecBenchConfig : ManualConfig
{
    public ExecBenchConfig()
    {
        AddJob(Job.ShortRun);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max, RankColumn.Arabic);
    }
}
