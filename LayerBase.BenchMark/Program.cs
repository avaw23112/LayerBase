using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using LayerBase;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.Layers;

namespace Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, LayerBaseBenchmarkConfig.Instance);
    }
}

public sealed class LayerBaseBenchmarkConfig : ManualConfig
{
    public static readonly IConfig Instance = Create();

    private static IConfig Create()
    {
        var config = Create(DefaultConfig.Instance);
        config.AddJob(Job.ShortRun);
        config.AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);
        config.AddColumn(CategoriesColumn.Default, StatisticColumn.Min, StatisticColumn.Max, RankColumn.Arabic);
        config.AddExporter(MarkdownExporter.GitHub);
        config.Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
        config.SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
        return config;
    }
}

[MemoryDiagnoser]
// LayerBase benchmark suite
// 01.Baseline            -> 直接委托 / 最小壳成本
// 02.Dispatch            -> 核心分发语义成本（扇�?/ Handled / 路由�?
// 03.Call                -> request/response 路径
// 04.Compare.CSharpEvent -> 与原�?C# event 的横向对�?
// 05.PostPump            -> 队列投递与排空
// 06.AsyncParallel       -> 异步与并行调�?
// 90.Scenario.Legacy     -> 原有业务场景压测，保留用于版本回�?
public abstract class EventBenchmarkBase
{
    protected const int OneMillion = 200_000;
    protected const int TenThousand = 2_000;
    protected const int HundredThousand = 20_000;
}

public static class BenchmarkSink
{
    public static int IntValue;
}

public class DirectVsFrameworkDispatchBench : EventBenchmarkBase
{
    private readonly EventNotifyDelegate<NotifyEvent> _directNotify = static (in NotifyEvent _) => { };

    private readonly EventHandleDelegate<BenchEvent> _directSync = static (in BenchEvent _) =>
        EventHandledState.Continue;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var layer = new BenchLayer();
        layer.RegisterService(new BenchManager());
        layer.RegisterService(new NotifyBenchManager());
        LayerHub.CreateLayers().Push(layer).Build();
    }

    [Benchmark(Baseline = true, Description = "直接委托调用 (同步) - 100万次")]
    [BenchmarkCategory("01.Baseline", "Dispatch.Sync", "Compare.Direct")]
    public void DirectDelegate()
    {
        for (var i = 0; i < OneMillion; i++)
            BenchmarkSink.IntValue = (int)_directSync(in BenchEvent.Instance);
    }

    [Benchmark(Description = "直接委托调用 (Notify) - 100万次")]
    [BenchmarkCategory("01.Baseline", "Dispatch.Notify", "Compare.Direct")]
    public void DirectNotifyDelegate()
    {
        for (var i = 0; i < OneMillion; i++)
            _directNotify(in NotifyEvent.Instance);
    }

    [Benchmark(Description = "框架同步分发 (1�?1订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Compare.Direct")]
    public void LayerBaseSync()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
    }

    [Benchmark(Description = "框架Notify分发 (1�?1订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Notify", "Compare.Direct")]
    public void LayerBaseNotify()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(NotifyEvent.Instance);
    }
}

public class FanoutScalingBench : EventBenchmarkBase
{
    [Params(1, 4, 16)] public int SubscriberCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var layer = new BenchLayer();
        for (var i = 0; i < SubscriberCount; i++)
        {
            layer.RegisterService(new FanoutSyncManager());
            layer.RegisterService(new FanoutNotifyManager());
        }

        LayerHub.CreateLayers().Push(layer).Build();
    }

    [Benchmark(Baseline = true, Description = "同步扇出扩展 (1�?N订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Dispatch.Fanout")]
    public void StandardSync()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
    }

    [Benchmark(Description = "Notify扇出扩展 (1�?N订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Notify", "Dispatch.Fanout")]
    public void Notify()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(NotifyEvent.Instance);
    }
}

public class HandledSemanticsBench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var layer = new BenchLayer();

        for (var i = 0; i < 32; i++)
            layer.RegisterService(new ContinueOnlyManager());

        layer.RegisterService(new FirstHandledManager());
        for (var i = 0; i < 31; i++)
            layer.RegisterService(new FirstHandledContinueManager());

        for (var i = 0; i < 31; i++)
            layer.RegisterService(new LastHandledContinueManager());
        layer.RegisterService(new LastHandledManager());

        LayerHub.CreateLayers().Push(layer).Build();
    }

    [Benchmark(Baseline = true, Description = "全部Continue (32订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Dispatch.Handled")]
    public void AllContinue()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(ContinueOnlyEvent.Instance);
    }

    [Benchmark(Description = "首个Handled短路 (32订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Dispatch.Handled")]
    public void FirstHandled()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(FirstHandledEvent.Instance);
    }

    [Benchmark(Description = "末尾Handled短路 (32订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Dispatch.Handled")]
    public void LastHandled()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(LastHandledEvent.Instance);
    }
}

public class RoutingShapeBench : EventBenchmarkBase
{
    private BenchLayer _tailLayer = null!;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var builder = LayerHub.CreateLayers();
        for (var i = 0; i < 3; i++) builder.Push(new BenchLayer());

        _tailLayer = new BenchLayer();
        _tailLayer.RegisterService(new RoutedManager());
        builder.Push(_tailLayer).Build();
    }

    [Benchmark(Baseline = true, Description = "定向本地分发 (目标�?订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Dispatch.Routing")]
    public void Local()
    {
        for (var i = 0; i < OneMillion; i++) _tailLayer.SendLocal(RoutedEvent.Instance);
    }

    [Benchmark(Description = "全局分发命中尾层 (4�?尾层1订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Dispatch.Routing")]
    public void GlobalTailHit()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(RoutedEvent.Instance);
    }
}

public class CallSubsystemBench : EventBenchmarkBase
{
    // _baseline:
    // 1. 直接调用的基线对象�?
    // 2. 它不经过 LayerHub，不做层定位和路由命中�?
    // 3. 作用是提供一个“只保留最小业务处理”的对照组�?
    private readonly CallDirectBaseline _baseline = new();

    // _seed:
    // 1. 用来生成每次循环都不同的请求值�?
    // 2. 如果请求永远固定，比如一直是 new CallRequest(123)�?
    //    JIT（即时编译器，运行时�?C# 编译成机器码的组件）
    //    更容易把一些逻辑提前算掉，导�?baseline 看起来不真实地快�?
    private int _seed;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        LayerHub.CreateLayers().Push(new CallBenchLayer()).Build();

        // 初始化一个非零种子�?
        // 这里只是随便给一个固定初始值，不要求“随机质量”，
        // 只要求后续能稳定地产生“每次都不一样”的请求�?
        _seed = unchecked(0x12345678);
    }

    [Benchmark(Baseline = true, Description = "直接方法调用 (Call基线) - 10万次")]
    [BenchmarkCategory("03.Call", "Call", "Compare.Baseline")]
    public void DirectMethod()
    {
        // state:
        // 1. 把字段复制到局部变量里�?
        // 2. 这样循环里读写更直接，也更接近真实热路径�?
        var state = _seed;

        for (var i = 0; i < HundredThousand; i++)
        {
            // NextState(state):
            // 1. 生成下一个状态值�?
            // 2. 这是一个很便宜的伪随机推进函数�?
            // 3. “伪随机”指它不是密码学安全随机，只是为了让输入不断变化�?
            state = NextState(state);

            // request:
            // 1. 每次循环都构造不同的请求�?
            // 2. 这样可以减少“输入恒�?-> 输出恒定 -> 整段被过度优化”的概率�?
            var request = new CallRequest(state);

            // response:
            // 1. 通过 direct baseline 执行一次最小业务调用�?
            // 2. 返回值仍然走 LBTask + GetAwaiter().GetResult()�?
            //    这样�?LayerCall 在返回形态上尽量一致�?
            var response = DirectInvoke(_baseline, request);

            // Volatile.Write:
            // 1. 把结果真正写到一个外部可见的位置�?
            // 2. Volatile 的意思是“这个写入不能被轻易忽略或重排”�?
            // 3. 这样能降�?JIT 把整段计算当成无意义代码删掉的概率�?
            Volatile.Write(ref BenchmarkSink.IntValue, response.Value);
        }

        // 把最终状态写回字段，保证整个循环确实有副作用�?
        _seed = state;
    }

    [Benchmark(Description = "LayerHub.CallAsync (单层单处理器) - 10万次")]
    [BenchmarkCategory("03.Call", "Call", "Compare.Baseline")]
    public void LayerCall()
    {
        var state = _seed;

        for (var i = 0; i < HundredThousand; i++)
        {
            state = NextState(state);
            var request = new CallRequest(state);

            // 这里�?DirectMethod 的唯一区别�?
            // 1. DirectMethod 直接�?baseline�?
            // 2. LayerCall 通过 LayerHub 做层定位、路由命中、处理器调度�?
            var response = LayerInvoke(request);

            Volatile.Write(ref BenchmarkSink.IntValue, response.Value);
        }

        _seed = state;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CallResponse DirectInvoke(CallDirectBaseline baseline, CallRequest request)
    {
        // baseline:
        // 1. 基线对象实例�?
        // 2. 由它提供“最小业务处理”的直连调用�?
        //
        // request:
        // 1. 本次调用的请求对象�?
        //
        // NoInlining:
        // 1. 表示“不要内联”�?
        // 2. “内联”就是把函数体直接展开到调用处�?
        // 3. 禁止内联能减�?benchmark 被优化得过于理想化的概率�?
        return baseline.HandleAsync(request).GetAwaiter().GetResult();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CallResponse LayerInvoke(CallRequest request)
    {
        // request:
        // 1. 本次要发送给 LayerHub 的请求对象�?
        //
        // 逻辑说明�?
        // 1. 这里固定命中 CallBenchLayer�?
        // 2. �?LayerHub 完成层定位、请求类型匹配、处理器调度�?
        return LayerHub.CallAsync<CallBenchLayer, CallRequest, CallResponse>(request)
                       .GetAwaiter()
                       .GetResult();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NextState(int state)
    {
        // state:
        // 1. 上一次的状态值�?
        //
        // 返回�?
        // 1. 新的状态值�?
        //
        // 逻辑说明�?
        // 1. 这是一个线性同余生成器（LCG）的推进公式�?
        // 2. 线性同余生成器是一种非常简单、非常快的伪随机数生成方法�?
        // 3. 这里不追求“随机质量”，只追求“每次输入都不同且成本很低”�?
        return unchecked(state * 1664525 + 1013904223);
    }
}

public class PostPumpBench : EventBenchmarkBase
{
    private PumpDrivenLayer _singleLayer = null!;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();

        _singleLayer = new PumpDrivenLayer();
        _singleLayer.RegisterService(new PumpManager());

        LayerHub.CreateLayers().Push(_singleLayer).Build();
    }

    [Benchmark(Description = "Post后立即Pump排空 (1�?1订阅) - 1万次")]
    [BenchmarkCategory("05.PostPump", "Dispatch.Queue", "PostPump")]
    public void PostThenPumpEach()
    {
        for (var i = 0; i < TenThousand; i++)
        {
            LayerHub.Post(PumpEvent.Instance);
            LayerHub.Pump(0.016f);
        }
    }

    [Benchmark(Description = "批量Post后单次Pump排空 (1�?1订阅) - 10万次")]
    [BenchmarkCategory("05.PostPump", "Dispatch.Queue", "PostPump")]
    public void BatchPostThenPump()
    {
        for (var i = 0; i < HundredThousand; i++)
            LayerHub.Post(PumpEvent.Instance);

        LayerHub.Pump(0.016f);
    }
}

public class AsyncDispatchBench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var layer = new BenchLayer();
        layer.RegisterService(new AsyncBenchManager());
        LayerHub.CreateLayers().Push(layer).Build();
    }

    [Benchmark(Description = "异步事件调度 (1�?1异步订阅) - 10万次")]
    [BenchmarkCategory("06.AsyncParallel", "Dispatch.Async", "Async")]
    public void AsyncDispatch()
    {
        for (var i = 0; i < HundredThousand; i++)
            LayerHub.Send(AsyncBenchEvent.Instance);
    }
}

public class ParallelDispatchBench : EventBenchmarkBase
{
    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        LayerHub.InitializeJobScheduler(4);
        var layer = new BenchLayer();
        layer.RegisterService(new ParallelNoopManager());
        layer.RegisterService(new ParallelWorkloadManager());
        LayerHub.CreateLayers().Push(layer).Build();
    }

    [Benchmark(Baseline = true, Description = "并行事件入队与排�?(空工�? 1订阅) - 10万次")]
    [BenchmarkCategory("06.AsyncParallel", "Dispatch.Parallel", "Parallel")]
    public void ParallelDispatchNoop()
    {
        for (var i = 0; i < HundredThousand; i++)
            LayerHub.Send(ParallelBenchEvent.Instance);
    }

    [Benchmark(Description = "并行事件入队与排�?(固定CPU工作, 1订阅) - 10万次")]
    [BenchmarkCategory("06.AsyncParallel", "Dispatch.Parallel", "Parallel")]
    public void ParallelDispatchWithWorkload()
    {
        for (var i = 0; i < HundredThousand; i++)
            LayerHub.Send(ParallelWorkloadEvent.Instance);
    }
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

    [Benchmark(Description = "单层低压 (1�?1订阅) - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Sync", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
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

    [Benchmark(Description = "单层低压 Notify (1�?1订阅) - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Notify", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(NotifyEvent.Instance);
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

    [Benchmark(Baseline = true, Description = "单层低压标准同步 (1�?1订阅) - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Sync", "Scenario.Legacy")]
    public void StandardSync()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
    }

    [Benchmark(Description = "单层低压 Notify (1�?1订阅) - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Notify", "Scenario.Legacy")]
    public void NotifyRoute()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(NotifyEvent.Instance);
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

    [Benchmark(Description = "单层高压 (1�?10订阅) - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Sync", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
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

    [Benchmark(Description = "多层低压 (10�?仅尾�? - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Sync", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
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

    [Benchmark(Description = "多层高压 (10�?全订�? - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Sync", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
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

    [Benchmark(Description = "多层高压 Notify (10�?全订�? - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Notify", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(FullNotifyEvent.Instance);
    }
}

public partial class FullNotifyBenchManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [SubscribeNotify]
    public void Handle(in FullNotifyEvent e)
    {
    }
}

public struct FullNotifyEvent
{
    public static readonly FullNotifyEvent Instance = default;
}

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

    [Benchmark(Description = "多层随机负载 (10�?5层订�? - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Sync", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
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

    [Benchmark(Description = "极限空负�?(64�?0订阅) - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Empty", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
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
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Sync", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < TenThousand; i++) LayerHub.Send(BenchEvent.Instance);
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

    [Benchmark(Description = "中重度负�?(180订阅) - 1万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Sync", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < TenThousand; i++) LayerHub.Send(BenchEvent.Instance);
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

    [Benchmark(Description = "Notify中重度负�?(180订阅) - 1万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Notify", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < TenThousand; i++) LayerHub.Send(NotifyEvent.Instance);
    }
}

public partial class NotifyBenchManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [SubscribeNotify]
    public void Handle(in NotifyEvent e)
    {
    }
}

public partial class AsyncBenchManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [SubscribeAsync]
    public LBTask Handle(AsyncBenchEvent e)
    {
        return LBTask.CompletedTask;
    }
}

public struct NotifyEvent
{
    public static readonly NotifyEvent Instance = default;
}

public struct AsyncBenchEvent
{
    public static readonly AsyncBenchEvent Instance = default;
}

public struct ParallelBenchEvent
{
    public static readonly ParallelBenchEvent Instance = default;
}

public struct ParallelWorkloadEvent
{
    public static readonly ParallelWorkloadEvent Instance = default;
}

public struct PumpEvent
{
    public static readonly PumpEvent Instance = default;
}

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

public class PumpDrivenLayer : Layer
{
}

public struct BenchEvent
{
    public static readonly BenchEvent Instance = default;
}

public struct ContinueOnlyEvent
{
    public static readonly ContinueOnlyEvent Instance = default;
}

public struct FirstHandledEvent
{
    public static readonly FirstHandledEvent Instance = default;
}

public struct LastHandledEvent
{
    public static readonly LastHandledEvent Instance = default;
}

public struct RoutedEvent
{
    public static readonly RoutedEvent Instance = default;
}

public partial class CallBenchLayer : Layer
{
}

public struct CallRequest
{
    public CallRequest(int value)
    {
        Value = value;
    }

    public int Value { get; set; }
}

public struct CallResponse
{
    public CallResponse(int value)
    {
        Value = value;
    }

    public int Value { get; set; }
}

public sealed class CallDirectBaseline
{
    public LBTask<CallResponse> HandleAsync(CallRequest request)
    {
        return LBTask<CallResponse>.FromResult(new CallResponse(request.Value + 1));
    }
}

[OwnerLayer(typeof(CallBenchLayer))]
public sealed class CallBenchHandler : ILayerCallHandler<CallRequest, CallResponse>
{
    public LBTask<CallResponse> HandleAsync(CallRequest request, CancellationToken cancellationToken = default)
    {
        return LBTask<CallResponse>.FromResult(new CallResponse(request.Value + 1));
    }
}

public partial class FanoutSyncManager : IService
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

public partial class FanoutNotifyManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [SubscribeNotify]
    public void Handle(in NotifyEvent e)
    {
    }
}

public partial class ContinueOnlyManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [Subscribe]
    public EventHandledState Handle(in ContinueOnlyEvent e)
    {
        return EventHandledState.Continue;
    }
}

public partial class FirstHandledManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [Subscribe]
    public EventHandledState Handle(in FirstHandledEvent e)
    {
        return EventHandledState.Handled;
    }
}

public partial class FirstHandledContinueManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [Subscribe]
    public EventHandledState Handle(in FirstHandledEvent e)
    {
        return EventHandledState.Continue;
    }
}

public partial class LastHandledContinueManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [Subscribe]
    public EventHandledState Handle(in LastHandledEvent e)
    {
        return EventHandledState.Continue;
    }
}

public partial class LastHandledManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [Subscribe]
    public EventHandledState Handle(in LastHandledEvent e)
    {
        return EventHandledState.Handled;
    }
}

public partial class RoutedManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [Subscribe]
    public EventHandledState Handle(in RoutedEvent e)
    {
        return EventHandledState.Continue;
    }
}

public partial class PumpManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [Subscribe]
    public EventHandledState Handle(in PumpEvent e)
    {
        return EventHandledState.Continue;
    }
}

public partial class ParallelNoopManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [SubscribeParallel]
    public EventHandledState Handle(in ParallelBenchEvent e)
    {
        return EventHandledState.Continue;
    }
}

public partial class ParallelWorkloadManager : IService
{
    private int _sink;

    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [SubscribeParallel]
    public EventHandledState Handle(in ParallelWorkloadEvent value)
    {
        var acc = _sink;
        for (var i = 0; i < 16; i++)
            acc = (acc * 33) ^ (i + 17);
        _sink = acc;
        BenchmarkSink.IntValue = acc;
        return EventHandledState.Continue;
    }
}

