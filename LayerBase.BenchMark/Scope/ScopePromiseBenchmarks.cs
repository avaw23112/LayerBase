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
/// ScopePromise 单片基准：创建、完成、注册 continuation、GetResult。
/// Category: 08.Scope.Promise
/// </summary>
[MemoryDiagnoser]
[Config(typeof(PromiseBenchConfig))]
[BenchmarkCategory("08.Scope.Promise")]
public class ScopePromiseBenchmarks
{
    private ScopePromise<int>? _promise;
    private ScopePromise<int>? _completedPromise;
    private ScopeRuntime? _scope;
    private ScopeRuntime? _workerScope;

    [GlobalSetup]
    public void Setup()
    {
        _scope = new ScopeRuntime(
            new ScopeDescriptor(0, "Main", ScopeThreadingMode.Inline, ScopeClockMode.EngineDriven, 0, ScopeStopPolicy.Drain),
            Array.Empty<IService>());
        _workerScope = new ScopeRuntime(
            new ScopeDescriptor(1, "Worker", ScopeThreadingMode.Worker, ScopeClockMode.FixedRate, 60, ScopeStopPolicy.Drain),
            Array.Empty<IService>());
        _promise = new ScopePromise<int>(_scope);
        _completedPromise = new ScopePromise<int>(_scope);
        _completedPromise.SetResult(42);
    }

    [Benchmark(Baseline = true, Description = "空委托基线")]
    [BenchmarkCategory("08.Scope.Promise")]
    public void Baseline() { }

    [Benchmark(Description = "Create Promise")]
    [BenchmarkCategory("08.Scope.Promise")]
    public ScopePromise<int> CreatePromise()
    {
        return new ScopePromise<int>(null);
    }

    [Benchmark(Description = "IsCompleted (false)")]
    [BenchmarkCategory("08.Scope.Promise")]
    public void IsCompleted_False()
    {
        var c = _promise!.IsCompleted;
        Volatile.Write(ref BenchmarkSink.IntValue, c ? 1 : 0);
    }

    [Benchmark(Description = "IsCompleted (true)")]
    [BenchmarkCategory("08.Scope.Promise")]
    public void IsCompleted_True()
    {
        var c = _completedPromise!.IsCompleted;
        Volatile.Write(ref BenchmarkSink.IntValue, c ? 1 : 0);
    }

    [Benchmark(Description = "SetResult (无 continuation)")]
    [BenchmarkCategory("08.Scope.Promise")]
    public void SetResult_NoContinuation()
    {
        var p = new ScopePromise<int>(null);
        p.SetResult(42);
    }

    [Benchmark(Description = "SetResult (有 continuation)")]
    [BenchmarkCategory("08.Scope.Promise")]
    public void SetResult_WithContinuation()
    {
        var p = new ScopePromise<int>(_scope!);
        p.OnCompleted(static () => { });
        p.SetResult(42);
    }

    [Benchmark(Description = "SetException + continuation")]
    [BenchmarkCategory("08.Scope.Promise")]
    public void SetException_WithContinuation()
    {
        var p = new ScopePromise<int>(_scope!);
        p.OnCompleted(static () => { });
        p.SetException(new InvalidOperationException("bench"));
    }

    [Benchmark(Description = "GetResult (成功)")]
    [BenchmarkCategory("08.Scope.Promise")]
    public void GetResult_Success()
    {
        var v = _completedPromise!.GetResult();
        Volatile.Write(ref BenchmarkSink.IntValue, v);
    }

    [Benchmark(Description = "Register Continuation (先于 Complete)")]
    [BenchmarkCategory("08.Scope.Promise")]
    public void RegisterContinuation_BeforeComplete()
    {
        var p = new ScopePromise<int>(_scope!);
        p.OnCompleted(static () => { });
        p.SetResult(42);
    }

    [Benchmark(Description = "Register Continuation (Complete 之后)")]
    [BenchmarkCategory("08.Scope.Promise")]
    public void RegisterContinuation_AfterComplete()
    {
        var p = new ScopePromise<int>(_scope!);
        p.SetResult(42);
        p.OnCompleted(static () => { });
    }
}

public sealed class PromiseBenchConfig : ManualConfig
{
    public PromiseBenchConfig()
    {
        AddJob(Job.ShortRun);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max);
    }
}
