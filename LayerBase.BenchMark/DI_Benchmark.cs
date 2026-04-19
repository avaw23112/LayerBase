using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;

namespace Benchmarks.DI;

[MemoryDiagnoser]
public class DI_Benchmark
{
    private const int OneMillion = 1_000_000;
    private LayerRuntime _scopedRuntime = null!;
    private LayerRuntime _singletonRuntime = null!;

    [GlobalSetup]
    public void Setup()
    {
        // --- Scoped 场景 ---
        var scopedLayer = new BenchLayer();
        scopedLayer.RegisterService(new BenchServiceModule(10));
        _scopedRuntime = LayerHub.CreateLayers().Push(scopedLayer).Build();

        // --- Singleton 场景 ---
        var singletonLayer = new BenchLayer();
        var singletonModule = new SingletonBenchServiceModule(10);
        singletonLayer.RegisterService(singletonModule);
        _singletonRuntime = LayerHub.CreateLayers().Push(singletonLayer).Build();
    }

    [Benchmark(Description = "DI-Scoped: 10个动态解析的订阅者")]
    public void ScopedDI_Resolution()
    {
        for (var i = 0; i < OneMillion; i++) _scopedRuntime.Send(new BenchEvent());
    }

    [Benchmark(Description = "DI-Singleton: 10个预创建实例的订阅者")]
    public void SingletonInstance_Resolution()
    {
        for (var i = 0; i < OneMillion; i++) _singletonRuntime.Send(new BenchEvent());
    }
}

// --- 辅助类 ---

public class BenchServiceModule : IService
{
    private readonly int _count;
    public BenchServiceModule(int count) => _count = count;

    public void ConfigureServices(IServiceCollection services)
    {
        for (int i = 0; i < _count; i++)
            services.AddScoped<BenchManager, BenchManager>();
    }
}

public class SingletonBenchServiceModule : IService
{
    private readonly List<BenchManager> _instances;
    public SingletonBenchServiceModule(int count)
    {
        _instances = new List<BenchManager>(count);
        for (int i = 0; i < count; i++)
        {
            _instances.Add(new BenchManager());
        }
    }

    public void ConfigureServices(IServiceCollection services)
    {
        foreach (var instance in _instances)
        {
            services.AddSingleton(instance);
        }
    }
}

public partial class BenchManager : ILayerContext
{
    [Subscribe]
    public EventHandledState Handle(in BenchEvent e)
    {
        return EventHandledState.Continue;
    }
}

public class BenchLayer : Layer { }
public struct BenchEvent { }