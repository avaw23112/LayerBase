using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;

namespace Benchmarks.Scope;

/// <summary>
/// Scope Post 全链路基准：从 Post → ScopeHost.Pump → Drain → Dispatch。
/// Category: 08.Scope.Post
/// </summary>
[MemoryDiagnoser]
[Config(typeof(PostBenchConfig))]
[BenchmarkCategory("08.Scope.Post")]
public class ScopePostBenchmarks
{
    private LayerRuntime _runtime = null!;
    private ScopeRef<BenchPostScope> _scopeRef;
    private LayerRuntime _multiRuntime = null!;
    private ScopeRef<BenchPostScope> _multiRef;
    private BenchPostEvent _event;

    [Params(1, 32, 256)]
    public int MessageCount;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        _event = new BenchPostEvent { Value = 42 };

        var singleBuilder = LayerHub.CreateLayers();
        singleBuilder.Push(new PostBenchLayer());
        _runtime = singleBuilder.Build();
        _scopeRef = _runtime.ScopeHost!.GetScopeRef<BenchPostScope>();

        LayerHub.Reset();
        var multiBuilder = LayerHub.CreateLayers();
        multiBuilder.Push(new PostBenchLayer());
        _multiRuntime = multiBuilder.Build();
        _multiRef = _multiRuntime.ScopeHost!.GetScopeRef<BenchPostScope>();
    }

    [Benchmark(Baseline = true, Description = "直接 Queue TryEnqueue 基线")]
    [BenchmarkCategory("08.Scope.Post")]
    public void RawQueueEnqueueBaseline()
    {
        var q = _runtime.ScopeHost!;
        for (int i = 0; i < MessageCount; i++)
            _scopeRef.TryPost(0, _event);
    }

    [Benchmark(Description = "Post + ScopeHost.Pump")]
    [BenchmarkCategory("08.Scope.Post")]
    public void PostAndPump()
    {
        var sr = _scopeRef;
        for (int i = 0; i < MessageCount; i++)
            sr.TryPost(0, _event);
        _runtime.Pump(0.016f);
    }

    [Benchmark(Description = "Post + Pump + 全 Dispatch")]
    [BenchmarkCategory("08.Scope.Post")]
    public void PostAndPumpFullDispatch()
    {
        var sr = _scopeRef;
        for (int i = 0; i < MessageCount; i++)
            sr.TryPost(0, _event);
        _runtime.ScopeHost!.Pump(0.016f);
    }
}

// ── Scope 定义 ──

[ScopeOptions]
public sealed partial class BenchPostScope { }

[ScopeEvent<BenchPostScope>]
public readonly struct BenchPostEvent
{
    public int Value { get; init; }
}

// ── Service ──

[Scope<BenchPostScope>]
public sealed partial class BenchPostService : IService
{
    public int Total;

    public void ConfigureServices(IServiceCollection services) { }

    [ScopeEvent]
    private void OnBenchPostEvent(BenchPostEvent message)
    {
        Total += message.Value;
    }
}

// ── Layer ──

public partial class PostBenchLayer : Layer
{
    public PostBenchLayer()
    {
        RegisterService(typeof(BenchPostService), new BenchPostService());
    }
}

public sealed class PostBenchConfig : ManualConfig
{
    public PostBenchConfig()
    {
        AddJob(Job.ShortRun);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max);
    }
}
