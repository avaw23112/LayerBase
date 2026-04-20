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
// 🚀 移除 HideColumns，确保输出最完整的原始报表
public abstract class EventBenchmarkBase
{
    protected const int OneMillion = 1_000_000;
    protected const int TenThousand = 130_000;
}

public class SingleLayer_Low_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var l = new BenchLayer();
        l.RegisterService(new BenchManager());
        LayerHub.CreateLayers().Push(l).Build();
    }

    [Benchmark(Description = "单层低压 (1层/1订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent());
    }
}

public class SingleLayer_Low_Notify_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var l = new BenchLayer();
        l.RegisterService(new NotifyBenchManager());
        LayerHub.CreateLayers().Push(l).Build();
    }

    [Benchmark(Description = "单层低压 Notify (1层/1订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(new NotifyEvent());
    }
}

public class SingleLayer_Low_Comparison_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var l = new BenchLayer();
        l.RegisterService(new BenchManager());
        l.RegisterService(new NotifyBenchManager());
        LayerHub.CreateLayers().Push(l).Build();
    }

    [Benchmark(Baseline = true, Description = "单层低压标准同步 (1层/1订阅) - 100万次")]
    public void StandardSync()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent());
    }

    [Benchmark(Description = "单层低压 Notify (1层/1订阅) - 100万次")]
    public void NotifyRoute()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(new NotifyEvent());
    }
}

public class SingleLayer_High_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var l = new BenchLayer();
        for (var i = 0; i < 10; i++) l.RegisterService(new BenchManager());
        LayerHub.CreateLayers().Push(l).Build();
    }

    [Benchmark(Description = "单层高压 (1层/10订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent());
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
        tail.RegisterService(new BenchManager());
        builder.Push(tail).Build();
    }

    [Benchmark(Description = "多层低压 (10层/仅尾层) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent());
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
            l.RegisterService(new BenchManager());
            builder.Push(l);
        }

        builder.Build();
    }

    [Benchmark(Description = "多层高压 (10层/全订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent());
    }
}

public class MultiLayer_Full_Notify_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        for (var i = 0; i < 10; i++)
        {
            var l = new BenchLayer();
            l.RegisterService(new FullNotifyBenchManager());
            builder.Push(l);
        }

        builder.Build();
    }

    [Benchmark(Description = "多层高压 Notify (10层/全订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(new FullNotifyEvent());
    }
}

public partial class FullNotifyBenchManager : IService
{
    public void ConfigureServices(IServiceCollection s) => s.AddSingleton(this);

    [SubscribeNotify]
    public void Handle(in FullNotifyEvent e) { }
}

public struct FullNotifyEvent { }

public class MultiLayer_Random_Bench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        for (var i = 0; i < 10; i++)
        {
            var l = new BenchLayer();
            if (i % 2 == 0) l.RegisterService(new BenchManager());
            builder.Push(l);
        }

        builder.Build();
    }

    [Benchmark(Description = "多层随机负载 (10层/5层订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent());
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
        builder.Build();
    }

    [Benchmark(Description = "极限空负载 (64层/0订阅) - 100万次")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent());
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
            l.RegisterService(new BenchManager());
            builder.Push(l);
        }

        builder.Build();
    }

    [Benchmark(Description = "经典 1ms 挑战 (3层全订阅) - 1万次")]
    public void Run()
    {
        for (var i = 0; i < TenThousand; i++) LayerHub.Send(new BenchEvent());
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
            var count = i == 0 ? 100 : 20;
            for (var j = 0; j < count; j++) l.RegisterService(new BenchManager());
            builder.Push(l);
        }

        builder.Build();
    }

    [Benchmark(Description = "中重度负载 (180订阅) - 1万次")]
    public void Run()
    {
        for (var i = 0; i < TenThousand; i++) LayerHub.Send(new BenchEvent());
    }
}
public class Typical_Heavy_180_Bench_Notify : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        for (var i = 0; i < 5; i++)
        {
            var l = new BenchLayer();
            var count = i == 0 ? 100 : 20;
            for (var j = 0; j < count; j++) l.RegisterService(new NotifyBenchManager());
            builder.Push(l);
        }

        builder.Build();
    }

    [Benchmark(Description = "Notify中重度负载 (180订阅) - 1万次")]
    public void Run()
    {
        for (var i = 0; i < TenThousand; i++) LayerHub.Send(new NotifyEvent());
    }
}
public class NotifyComparisonBench : EventBenchmarkBase
{
    private const int HundredMillion = 100_000_000;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        var l = new BenchLayer();
        l.RegisterService(new StandardBenchManager());
        l.RegisterService(new NotifyBenchManager());
        builder.Push(l).Build();
    }

    [Benchmark(Description = "标准同步订阅 (1亿次)")]
    public void StandardSync()
    {
        for (var i = 0; i < HundredMillion; i++) LayerHub.Send(new BenchEvent());
    }

    [Benchmark(Description = "Notify零分支订阅 (1亿次)")]
    public void NotifyPipeline()
    {
        for (var i = 0; i < HundredMillion; i++) LayerHub.Send(new NotifyEvent());
    }
}

public partial class StandardBenchManager : IService
{
    public void ConfigureServices(IServiceCollection s) => s.AddSingleton(this);

    [Subscribe]
    public EventHandledState Handle(in BenchEvent e) => EventHandledState.Continue;
}

public partial class NotifyBenchManager : IService
{
    public void ConfigureServices(IServiceCollection s) => s.AddSingleton(this);

    [SubscribeNotify]
    public void Handle(in NotifyEvent e) { }
}

public struct NotifyEvent { }

public partial class BenchManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

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
