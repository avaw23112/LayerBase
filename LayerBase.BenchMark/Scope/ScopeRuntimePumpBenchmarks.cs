using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;

namespace Benchmarks.Scope;

[MemoryDiagnoser]
[Config(typeof(ScopeRuntimePumpBenchConfig))]
[BenchmarkCategory("08.Scope.Pump")]
public class ScopeRuntimePumpBenchmarks
{
    private LayerRuntime _legacyRuntime = null!;
    private ScopeRuntime[] _inlineScopes = null!;
    private ScopeRuntime[] _workerScopes = null!;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        _legacyRuntime = LayerHub.CreateLayers().Push(new EmptyPumpLayer()).Build();

        _inlineScopes =
        [
            CreateScope(101, "Inline1", ScopeThreadingMode.Inline, ScopeClockMode.EngineDriven),
            CreateScope(102, "Inline2", ScopeThreadingMode.Inline, ScopeClockMode.EngineDriven),
            CreateScope(103, "Inline3", ScopeThreadingMode.Inline, ScopeClockMode.EngineDriven),
            CreateScope(104, "Inline4", ScopeThreadingMode.Inline, ScopeClockMode.EngineDriven)
        ];

        _workerScopes =
        [
            CreateScope(201, "Worker1", ScopeThreadingMode.Worker, ScopeClockMode.FixedRate),
            CreateScope(202, "Worker2", ScopeThreadingMode.Worker, ScopeClockMode.FixedRate),
            CreateScope(203, "Worker3", ScopeThreadingMode.Worker, ScopeClockMode.FixedRate),
            CreateScope(204, "Worker4", ScopeThreadingMode.Worker, ScopeClockMode.FixedRate)
        ];

        foreach (ScopeRuntime scope in _inlineScopes)
        {
            scope.Start();
        }

        foreach (ScopeRuntime scope in _workerScopes)
        {
            scope.Start();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_workerScopes != null)
        {
            foreach (ScopeRuntime scope in _workerScopes)
            {
                scope.Dispose();
            }
        }

        if (_inlineScopes != null)
        {
            foreach (ScopeRuntime scope in _inlineScopes)
            {
                scope.Dispose();
            }
        }

        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "Legacy LayerRuntime empty Pump")]
    [BenchmarkCategory("08.Scope.Pump", "Legacy")]
    public void LegacyPump()
    {
        _legacyRuntime.Pump(0.016f);
    }

    [Benchmark(Description = "1 inline ScopeRuntime empty Pump")]
    [BenchmarkCategory("08.Scope.Pump", "Inline")]
    public void OneInlinePump()
    {
        _inlineScopes[0].Pump(0.016f);
    }

    [Benchmark(Description = "4 inline ScopeRuntime empty Pump")]
    [BenchmarkCategory("08.Scope.Pump", "Inline")]
    public void FourInlinePump()
    {
        _inlineScopes[0].Pump(0.016f);
        _inlineScopes[1].Pump(0.016f);
        _inlineScopes[2].Pump(0.016f);
        _inlineScopes[3].Pump(0.016f);
    }

    [Benchmark(Description = "1 worker ScopeRuntime external Pump")]
    [BenchmarkCategory("08.Scope.Pump", "Worker")]
    public void OneWorkerPump()
    {
        _workerScopes[0].Pump(0.016f);
    }

    [Benchmark(Description = "4 worker ScopeRuntime external Pump")]
    [BenchmarkCategory("08.Scope.Pump", "Worker")]
    public void FourWorkerPump()
    {
        _workerScopes[0].Pump(0.016f);
        _workerScopes[1].Pump(0.016f);
        _workerScopes[2].Pump(0.016f);
        _workerScopes[3].Pump(0.016f);
    }

    private static ScopeRuntime CreateScope(
        int scopeId,
        string name,
        ScopeThreadingMode threading,
        ScopeClockMode clock)
    {
        return new ScopeRuntime(
            new ScopeDescriptor(scopeId, name, threading, clock, 120, ScopeStopPolicy.Drain),
            Array.Empty<IService>());
    }
}

public sealed class EmptyPumpLayer : Layer
{
}

public sealed class ScopeRuntimePumpBenchConfig : ManualConfig
{
    public ScopeRuntimePumpBenchConfig()
    {
        AddJob(Job.ShortRun);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max);
    }
}
