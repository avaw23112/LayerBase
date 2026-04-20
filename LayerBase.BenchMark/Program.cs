using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Runtime.CompilerServices;
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
        var config = ManualConfig.Create(DefaultConfig.Instance);
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
// 02.Dispatch            -> 核心分发语义成本（扇出 / Handled / 路由）
// 03.Call                -> request/response 路径
// 04.Compare.CSharpEvent -> 与原生 C# event 的横向对照
// 05.PostPump            -> 队列投递与排空
// 06.AsyncParallel       -> 异步与并行调度
// 90.Scenario.Legacy     -> 原有业务场景压测，保留用于版本回归
public abstract class EventBenchmarkBase
{
    protected const int OneMillion = 1_000_000;
    protected const int TenThousand = 10_000;
    protected const int HundredThousand = 100_000;
}

public static class BenchmarkSink
{
    public static int IntValue;
}

public class DirectVsFrameworkDispatchBench : EventBenchmarkBase
{
    private readonly EventHandleDelegate<BenchEvent> _directSync = static (in BenchEvent _) => EventHandledState.Continue;
    private readonly EventNotifyDelegate<NotifyEvent> _directNotify = static (in NotifyEvent _) => { };

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

    [Benchmark(Description = "框架同步分发 (1层/1订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Compare.Direct")]
    public void LayerBaseSync()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
    }

    [Benchmark(Description = "框架Notify分发 (1层/1订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Notify", "Compare.Direct")]
    public void LayerBaseNotify()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(NotifyEvent.Instance);
    }
}

public class FanoutScalingBench : EventBenchmarkBase
{
    [Params(1, 4, 16, 64)]
    public int SubscriberCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        var layer = new BenchLayer();
        for (var i = 0; i < SubscriberCount; i++)
        {
            layer.Subscribe(static (in BenchEvent _) => EventHandledState.Continue);
            layer.SubscribeNotify(static (in NotifyEvent _) => { });
        }

        LayerHub.CreateLayers().Push(layer).Build();
    }

    [Benchmark(Baseline = true, Description = "同步扇出扩展 (1层/N订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Dispatch.Fanout")]
    public void StandardSync()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
    }

    [Benchmark(Description = "Notify扇出扩展 (1层/N订阅) - 100万次")]
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
            layer.Subscribe(static (in ContinueOnlyEvent _) => EventHandledState.Continue);

        layer.Subscribe(static (in FirstHandledEvent _) => EventHandledState.Handled);
        for (var i = 0; i < 31; i++)
            layer.Subscribe(static (in FirstHandledEvent _) => EventHandledState.Continue);

        for (var i = 0; i < 31; i++)
            layer.Subscribe(static (in LastHandledEvent _) => EventHandledState.Continue);
        layer.Subscribe(static (in LastHandledEvent _) => EventHandledState.Handled);

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
        _tailLayer.Subscribe(static (in RoutedEvent _) => EventHandledState.Continue);
        builder.Push(_tailLayer).Build();
    }

    [Benchmark(Baseline = true, Description = "定向本地分发 (目标层1订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Dispatch.Routing")]
    public void Local()
    {
        for (var i = 0; i < OneMillion; i++) _tailLayer.SendLocal(RoutedEvent.Instance);
    }

    [Benchmark(Description = "全局分发命中尾层 (4层/尾层1订阅) - 100万次")]
    [BenchmarkCategory("02.Dispatch", "Dispatch.Sync", "Dispatch.Routing")]
    public void GlobalTailHit()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(RoutedEvent.Instance);
    }
}

public class CallSubsystemBench : EventBenchmarkBase
{
    private readonly CallRequest _request = new(123);
    private readonly CallDirectBaseline _baseline = new();

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        LayerHub.CreateLayers().Push(new CallBenchLayer()).Build();
    }

    [Benchmark(Baseline = true, Description = "直接方法调用 (Call基线) - 10万次")]
    [BenchmarkCategory("03.Call", "Call", "Compare.Baseline")]
    public void DirectMethod()
    {
        for (var i = 0; i < HundredThousand; i++)
            BenchmarkSink.IntValue = _baseline.HandleAsync(_request).GetAwaiter().GetResult().Value;
    }

    [Benchmark(Description = "LayerHub.CallAsync (单层单处理器) - 10万次")]
    [BenchmarkCategory("03.Call", "Call", "Compare.Baseline")]
    public void LayerCall()
    {
        for (var i = 0; i < HundredThousand; i++)
            BenchmarkSink.IntValue =
                LayerHub.CallAsync<CallBenchLayer, CallRequest, CallResponse>(_request).GetAwaiter().GetResult().Value;
    }
}

public class CSharpEventSyncComparisonBench : EventBenchmarkBase
{
    [Params(1, 4, 16, 64)]
    public int SubscriberCount { get; set; }

    private CSharpEventPublisher _publisher = null!;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        _publisher = new CSharpEventPublisher();
        for (var i = 0; i < SubscriberCount; i++)
            _publisher.Sync += CSharpEventHandlers.SyncContinue;

        var layer = new BenchLayer();
        for (var i = 0; i < SubscriberCount; i++)
            layer.Subscribe<BenchEvent>(CSharpEventHandlers.SyncContinue);

        LayerHub.CreateLayers().Push(layer).Build();
    }

    [Benchmark(Baseline = true, Description = "C# event 同步扇出 (N订阅) - 100万次")]
    [BenchmarkCategory("04.Compare.CSharpEvent", "Dispatch.Sync", "Compare.CSharpEvent")]
    public void CSharpEvent()
    {
        for (var i = 0; i < OneMillion; i++)
            BenchmarkSink.IntValue = (int)_publisher.PublishSync(in BenchEvent.Instance);
    }

    [Benchmark(Description = "LayerBase 同步扇出 (1层/N订阅) - 100万次")]
    [BenchmarkCategory("04.Compare.CSharpEvent", "Dispatch.Sync", "Compare.CSharpEvent")]
    public void LayerBase()
    {
        for (var i = 0; i < OneMillion; i++)
            BenchmarkSink.IntValue = (int)LayerHub.Send(BenchEvent.Instance);
    }
}

public class CSharpEventNotifyComparisonBench : EventBenchmarkBase
{
    [Params(1, 4, 16, 64)]
    public int SubscriberCount { get; set; }

    private CSharpEventPublisher _publisher = null!;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        _publisher = new CSharpEventPublisher();
        for (var i = 0; i < SubscriberCount; i++)
            _publisher.Notify += CSharpEventHandlers.NotifyNoop;

        var layer = new BenchLayer();
        for (var i = 0; i < SubscriberCount; i++)
            layer.SubscribeNotify<NotifyEvent>(CSharpEventHandlers.NotifyNoop);

        LayerHub.CreateLayers().Push(layer).Build();
    }

    [Benchmark(Baseline = true, Description = "C# event Notify扇出 (N订阅) - 100万次")]
    [BenchmarkCategory("04.Compare.CSharpEvent", "Dispatch.Notify", "Compare.CSharpEvent")]
    public void CSharpEvent()
    {
        for (var i = 0; i < OneMillion; i++)
            _publisher.PublishNotify(in NotifyEvent.Instance);
    }

    [Benchmark(Description = "LayerBase Notify扇出 (1层/N订阅) - 100万次")]
    [BenchmarkCategory("04.Compare.CSharpEvent", "Dispatch.Notify", "Compare.CSharpEvent")]
    public void LayerBase()
    {
        for (var i = 0; i < OneMillion; i++)
            LayerHub.Send(NotifyEvent.Instance);
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
        _singleLayer.Subscribe(static (in PumpEvent _) => EventHandledState.Continue);

        LayerHub.CreateLayers().Push(_singleLayer).Build();
    }

    [Benchmark(Baseline = true, Description = "Post仅入队 (1层/1订阅) - 10万次")]
    [BenchmarkCategory("05.PostPump", "Dispatch.Queue", "PostPump")]
    public void PostOnly()
    {
        for (var i = 0; i < HundredThousand; i++)
            LayerHub.Post(PumpEvent.Instance);
    }

    [Benchmark(Description = "Post后立即Pump排空 (1层/1订阅) - 1万次")]
    [BenchmarkCategory("05.PostPump", "Dispatch.Queue", "PostPump")]
    public void PostThenPumpEach()
    {
        for (var i = 0; i < TenThousand; i++)
        {
            LayerHub.Post(PumpEvent.Instance);
            LayerHub.Pump(0.016f);
        }
    }

    [Benchmark(Description = "批量Post后单次Pump排空 (1层/1订阅) - 10万次")]
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

    [Benchmark(Description = "异步事件调度 (1层/1异步订阅) - 10万次")]
    [BenchmarkCategory("06.AsyncParallel", "Dispatch.Async", "Async")]
    public void AsyncDispatch()
    {
        for (var i = 0; i < HundredThousand; i++)
            LayerHub.Send(AsyncBenchEvent.Instance);
    }
}

public class ParallelDispatchBench : EventBenchmarkBase
{
    private readonly ParallelWorkloadConsumer _workloadConsumer = new();

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        LayerHub.InitializeJobScheduler(4);
        var layer = new BenchLayer();
        layer.SubscribeParallel(static (in ParallelBenchEvent _) => EventHandledState.Continue);
        layer.SubscribeParallel<ParallelWorkloadEvent>(_workloadConsumer.Handle);
        LayerHub.CreateLayers().Push(layer).Build();
    }

    [Benchmark(Baseline = true, Description = "并行事件入队与排空 (空工作, 1订阅) - 10万次")]
    [BenchmarkCategory("06.AsyncParallel", "Dispatch.Parallel", "Parallel")]
    public void ParallelDispatchNoop()
    {
        for (var i = 0; i < HundredThousand; i++)
            LayerHub.Send(ParallelBenchEvent.Instance);
    }

    [Benchmark(Description = "并行事件入队与排空 (固定CPU工作, 1订阅) - 10万次")]
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

    [Benchmark(Description = "单层低压 (1层/1订阅) - 100万次")]
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

    [Benchmark(Description = "单层低压 Notify (1层/1订阅) - 100万次")]
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

    [Benchmark(Baseline = true, Description = "单层低压标准同步 (1层/1订阅) - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Sync", "Scenario.Legacy")]
    public void StandardSync()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(BenchEvent.Instance);
    }

    [Benchmark(Description = "单层低压 Notify (1层/1订阅) - 100万次")]
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

    [Benchmark(Description = "单层高压 (1层/10订阅) - 100万次")]
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

    [Benchmark(Description = "多层低压 (10层/仅尾层) - 100万次")]
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

    [Benchmark(Description = "多层高压 (10层/全订阅) - 100万次")]
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

    [Benchmark(Description = "多层高压 Notify (10层/全订阅) - 100万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Notify", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < OneMillion; i++) LayerHub.Send(FullNotifyEvent.Instance);
    }
}

public partial class FullNotifyBenchManager : IService
{
    public void ConfigureServices(IServiceCollection s) => s.AddSingleton(this);

    [SubscribeNotify]
    public void Handle(in FullNotifyEvent e) { }
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

    [Benchmark(Description = "多层随机负载 (10层/5层订阅) - 100万次")]
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

    [Benchmark(Description = "极限空负载 (64层/0订阅) - 100万次")]
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

    [Benchmark(Description = "中重度负载 (180订阅) - 1万次")]
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

    [Benchmark(Description = "Notify中重度负载 (180订阅) - 1万次")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Notify", "Scenario.Legacy")]
    public void Run()
    {
        for (var i = 0; i < TenThousand; i++) LayerHub.Send(NotifyEvent.Instance);
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
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Sync", "Scenario.Legacy")]
    public void StandardSync()
    {
        for (var i = 0; i < HundredMillion; i++) LayerHub.Send(BenchEvent.Instance);
    }

    [Benchmark(Description = "Notify零分支订阅 (1亿次)")]
    [BenchmarkCategory("90.Scenario.Legacy", "Dispatch.Notify", "Scenario.Legacy")]
    public void NotifyPipeline()
    {
        for (var i = 0; i < HundredMillion; i++) LayerHub.Send(NotifyEvent.Instance);
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

public partial class AsyncBenchManager : IService
{
    public void ConfigureServices(IServiceCollection s) => s.AddSingleton(this);

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

public sealed class ParallelWorkloadConsumer
{
    private int _sink;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

public sealed class CSharpEventPublisher
{
    public event EventHandleDelegate<BenchEvent>? Sync;
    public event EventNotifyDelegate<NotifyEvent>? Notify;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandledState PublishSync(in BenchEvent value)
    {
        return Sync?.Invoke(in value) ?? EventHandledState.Continue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PublishNotify(in NotifyEvent value)
    {
        Notify?.Invoke(in value);
    }
}

public static class CSharpEventHandlers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventHandledState SyncContinue(in BenchEvent value)
    {
        return EventHandledState.Continue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotifyNoop(in NotifyEvent value)
    {
    }
}
