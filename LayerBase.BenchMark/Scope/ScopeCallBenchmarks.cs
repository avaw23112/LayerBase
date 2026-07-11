using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using LayerBase;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Scope;

namespace Benchmarks.Scope;

[MemoryDiagnoser]
[Config(typeof(ScopeCallBenchConfig))]
[BenchmarkCategory("08.Scope.Call")]
public class ScopeCallBenchmarks
{
    private LayerRuntime _runtime = null!;
    private ScopeRef<BenchCallScope> _scopeRef;
    private BenchCallService _service = null!;
    private int _nextValue;

    [Params(1, 8, 32, 128)]
    public int ServiceCount;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var layer = new BenchCallLayer(ServiceCount);
        _runtime = LayerHub.CreateLayers().Push(layer).Build();
        _scopeRef = _runtime.ScopeHost!.GetScopeRef<BenchCallScope>();
        _service = layer.Service;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "Raw ScopeRef.Call submit + drain")]
    [BenchmarkCategory("08.Scope.Call", "Submit")]
    public bool RawCallSubmit()
    {
        ScopePromise<BenchCallResult> promise;
        using var _ = ScopeExecution.Enter(_runtime.ScopeHost!.Scopes[0]);
        promise = _scopeRef.Call<BenchCallResult>(0, new BenchCall(++_nextValue));
        _runtime.ScopeHost!.Pump(0.016f);
        _runtime.ScopeHost!.Pump(0.016f);
        return promise.IsCompleted;
    }

    [Benchmark(Description = "Generated Call submit + dispatch + promise")]
    [BenchmarkCategory("08.Scope.Call", "Dispatch")]
    public int GeneratedCallRoundTrip()
    {
        using (ScopeExecution.Enter(_runtime.ScopeHost!.Scopes[0]))
        {
            _service.LastTask = _scopeRef.Call(new BenchCall(++_nextValue));
        }

        _runtime.ScopeHost!.Pump(0.016f);
        _runtime.ScopeHost!.Pump(0.016f);

        return _service.LastTask.GetAwaiter().GetResult().Value;
    }

    [Benchmark(Description = "Generated Call await continuation")]
    [BenchmarkCategory("08.Scope.Call", "Continuation")]
    public int GeneratedCallAwaitContinuation()
    {
        _service.RequestAwait(++_nextValue);
        _runtime.ScopeHost!.Pump(0.016f);
        _runtime.ScopeHost!.Pump(0.016f);
        return _service.LastAwaitedValue;
    }
}

[ScopeOptions]
public sealed partial class BenchCallScope
{
}

[ScopeCall<BenchCallScope, BenchCallResult>]
public readonly struct BenchCall
{
    public BenchCall(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

public readonly struct BenchCallResult
{
    public BenchCallResult(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

[Scope<BenchCallScope>]
public sealed partial class BenchCallService : IService, IUpdate
{
    private bool _awaitRequested;
    private int _awaitValue;

    public LBTask<BenchCallResult> LastTask;

    public int LastAwaitedValue { get; private set; }

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void RequestAwait(int value)
    {
        _awaitRequested = true;
        _awaitValue = value;
    }

    public void Update()
    {
        if (!_awaitRequested)
        {
            return;
        }

        _awaitRequested = false;
        AwaitGeneratedCall(_awaitValue).Forget();
    }

    private async LBTask AwaitGeneratedCall(int value)
    {
        BenchCallResult result = await Scope<BenchCallScope>().Call(new BenchCall(value));
        LastAwaitedValue = result.Value;
    }

    [ScopeCall]
    private async LBTask<BenchCallResult> OnBenchCall(BenchCall call)
    {
        await LBTask.CompletedTask;
        return new BenchCallResult(call.Value + 1);
    }
}

public sealed partial class BenchCallLayer : Layer
{
    public BenchCallLayer(int serviceCount)
    {
        Service = new BenchCallService();
        RegisterService(typeof(BenchCallService), Service);

        for (int i = 1; i < serviceCount; i++)
        {
            RegisterService(typeof(BenchCallPaddingService), new BenchCallPaddingService());
        }
    }

    public BenchCallService Service { get; }
}

public sealed class BenchCallPaddingService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}

public sealed class ScopeCallBenchConfig : ManualConfig
{
    public ScopeCallBenchConfig()
    {
        AddJob(Job.ShortRun);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max);
    }
}
