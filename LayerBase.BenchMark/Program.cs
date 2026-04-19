using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;

namespace Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

[MemoryDiagnoser]
public abstract class EventBenchmarkBase
{
    protected const int OneMillion = 1_000_000;
    protected LayerRuntime _runtime = null!;
}

public class SingleLayer_Low_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var l = new BenchLayer();
        l.RegisterService(new BenchServiceModule(1));
        _runtime = LayerHub.CreateLayers().Push(l).Build();
    }

    [Benchmark(Description = "单层低压 (1层/1订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) _runtime.Send(new BenchEvent());
    }
}

public class SingleLayer_High_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var l = new BenchLayer();
        l.RegisterService(new BenchServiceModule(10));
        _runtime = LayerHub.CreateLayers().Push(l).Build();
    }

    [Benchmark(Description = "单层高压 (1层/10订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) _runtime.Send(new BenchEvent());
    }
}

public class MultiLayer_Low_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        for (var i = 0; i < 9; i++) builder.Push(new BenchLayer());
        var tail = new BenchLayer();
        tail.RegisterService(new BenchServiceModule(1));
        _runtime = builder.Push(tail).Build();
    }

    [Benchmark(Description = "多层低压 (10层/仅尾层) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) _runtime.Send(new BenchEvent());
    }
}

public class MultiLayer_Full_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        for (var i = 0; i < 10; i++)
        {
            var l = new BenchLayer();
            l.RegisterService(new BenchServiceModule(1));
            builder.Push(l);
        }

        _runtime = builder.Build();
    }

    [Benchmark(Description = "多层高压 (10层/全订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) _runtime.Send(new BenchEvent());
    }
}

public class Extreme_Empty_64_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        for (var i = 0; i < 64; i++) builder.Push(new BenchLayer());
        _runtime = builder.Build();
    }

    [Benchmark(Description = "极限空负载 (64层/0订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) _runtime.Send(new BenchEvent());
    }
}

public class Classic_1ms_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        for (var i = 0; i < 3; i++)
        {
            var l = new BenchLayer();
            l.RegisterService(new BenchServiceModule(1));
            builder.Push(l);
        }

        _runtime = builder.Build();
    }

    [Benchmark(Description = "经典 1ms 挑战 (3层全订阅) - 1万次")]
    public void Run()
    {
        for (var i = 0; i < 10_000; i++) _runtime.Send(new BenchEvent());
    }
}

public class Typical_Heavy_180_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        for (var i = 0; i < 5; i++)
        {
            var l = new BenchLayer();
            l.RegisterService(new BenchServiceModule(36));
            builder.Push(l);
        }

        _runtime = builder.Build();
    }

    [Benchmark(Description = "典型重负载 (180个订阅) - 1万次")]
    public void Run()
    {
        for (var i = 0; i < 10_000; i++) _runtime.Send(new BenchEvent());
    }
}

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

public partial class BenchManager : ILayerContext
{
    [Subscribe]
    public EventHandledState Handle(in BenchEvent e)
    {
        return EventHandledState.Continue;
    }
}

public class BenchLayer : Layer
{
}

public struct BenchEvent
{
}