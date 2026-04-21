using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using LayerBase;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.Layers;
using IServiceCollection = LayerBase.DI.IServiceCollection;

namespace LayerBaseCompareBenchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, CompareBenchmarkConfig.Instance);
    }
}

public sealed class CompareBenchmarkConfig : ManualConfig
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
public abstract class CompareBenchmarkBase
{
    protected const int OneMillion = 1_000_000;
    protected const int HundredThousand = 100_000;
}

public static class CompareSink
{
    public static int IntValue;
}

public class PublishSingleSubscriberCompareBench : CompareBenchmarkBase
{
    private readonly EventNotifyDelegate<NotifyPayload> _direct = static (in NotifyPayload _) => { };
    private readonly CSharpNotifyPublisher _publisher = new();
    private System.IServiceProvider _provider = null!;
    private IPublisher<NotifyPayload> _messagePipePublisher = null!;
    private ISubscriber<NotifyPayload> _messagePipeSubscriber = null!;
    private IDisposable _messagePipeSubscription = null!;

    [GlobalSetup]
    public void Setup()
    {
        _publisher.Notify += static (in NotifyPayload _) => { };

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddMessagePipe();
        _provider = services.BuildServiceProvider();
        _messagePipePublisher = _provider.GetRequiredService<IPublisher<NotifyPayload>>();
        _messagePipeSubscriber = _provider.GetRequiredService<ISubscriber<NotifyPayload>>();
        _messagePipeSubscription = _messagePipeSubscriber.Subscribe(static _ => { });

        LayerHub.Reset();
        var layer = new CompareLayer();
        layer.RegisterService(new CompareNotifyManager());
        LayerHub.CreateLayers().Push(layer).Build();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _messagePipeSubscription.Dispose();
        ((IDisposable)_provider).Dispose();
        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "直接委托 (Notify, 1订阅) - 100万次")]
    [BenchmarkCategory("Compare.Notify", "Baseline")]
    public void DirectDelegate()
    {
        for (var i = 0; i < OneMillion; i++)
            _direct(in NotifyPayload.Instance);
    }

    [Benchmark(Description = "C# event (Notify, 1订阅) - 100万次")]
    [BenchmarkCategory("Compare.Notify", "CSharpEvent")]
    public void CSharpEvent()
    {
        for (var i = 0; i < OneMillion; i++)
            _publisher.Publish(in NotifyPayload.Instance);
    }

    [Benchmark(Description = "MessagePipe (Notify, 1订阅) - 100万次")]
    [BenchmarkCategory("Compare.Notify", "MessagePipe")]
    public void MessagePipe()
    {
        for (var i = 0; i < OneMillion; i++)
            _messagePipePublisher.Publish(NotifyPayload.Instance);
    }

    [Benchmark(Description = "LayerBase Notify (1层/1订阅) - 100万次")]
    [BenchmarkCategory("Compare.Notify", "LayerBase")]
    public void LayerBase()
    {
        for (var i = 0; i < OneMillion; i++)
            LayerHub.Send(NotifyPayload.Instance);
    }
}

public class PublishFanoutCompareBench : CompareBenchmarkBase
{
    [Params(1,4,8, 16)]
    public int SubscriberCount { get; set; }

    private readonly CSharpNotifyPublisher _publisher = new();
    private System.IServiceProvider _provider = null!;
    private IPublisher<NotifyPayload> _messagePipePublisher = null!;
    private ISubscriber<NotifyPayload> _messagePipeSubscriber = null!;
    private IDisposable[] _messagePipeSubscriptions = Array.Empty<IDisposable>();

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < SubscriberCount; i++)
            _publisher.Notify += static (in NotifyPayload _) => { };

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddMessagePipe();
        _provider = services.BuildServiceProvider();
        _messagePipePublisher = _provider.GetRequiredService<IPublisher<NotifyPayload>>();
        _messagePipeSubscriber = _provider.GetRequiredService<ISubscriber<NotifyPayload>>();
        _messagePipeSubscriptions = new IDisposable[SubscriberCount];
        for (var i = 0; i < SubscriberCount; i++)
            _messagePipeSubscriptions[i] = _messagePipeSubscriber.Subscribe(static _ => { });

        LayerHub.Reset();
        var layer = new CompareLayer();
        for (var i = 0; i < SubscriberCount; i++)
            layer.RegisterService(new CompareNotifyManager());
        LayerHub.CreateLayers().Push(layer).Build();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var d in _messagePipeSubscriptions) d.Dispose();
        ((IDisposable)_provider).Dispose();
        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "C# event Notify扇出 (N订阅) - 100万次")]
    [BenchmarkCategory("Compare.Notify", "CSharpEvent")]
    public void CSharpEvent()
    {
        for (var i = 0; i < OneMillion; i++)
            _publisher.Publish(in NotifyPayload.Instance);
    }

    [Benchmark(Description = "MessagePipe Notify扇出 (N订阅) - 100万次")]
    [BenchmarkCategory("Compare.Notify", "MessagePipe")]
    public void MessagePipe()
    {
        for (var i = 0; i < OneMillion; i++)
            _messagePipePublisher.Publish(NotifyPayload.Instance);
    }

    [Benchmark(Description = "LayerBase Notify扇出 (1层/N订阅) - 100万次")]
    [BenchmarkCategory("Compare.Notify", "LayerBase")]
    public void LayerBase()
    {
        for (var i = 0; i < OneMillion; i++)
            LayerHub.Send(NotifyPayload.Instance);
    }
}

public class RequestResponseCompareBench : CompareBenchmarkBase
{
    private readonly CompareRequest _request = new(123);
    private readonly CompareDirectBaseline _baseline = new();
    private System.IServiceProvider _provider = null!;
    private IRequestHandler<CompareRequest, CompareResponse> _messagePipeHandler = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var options = services.AddMessagePipe();
        options.AddRequestHandler<MessagePipeCompareHandler>();
        _provider = services.BuildServiceProvider();
        _messagePipeHandler = _provider.GetRequiredService<IRequestHandler<CompareRequest, CompareResponse>>();

        LayerHub.Reset();
        LayerHub.CreateLayers().Push(new CompareCallLayer()).Build();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        ((IDisposable)_provider).Dispose();
        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "直接LBTask基线 (Request/Response) - 10万次")]
    [BenchmarkCategory("Compare.Request", "Baseline")]
    public void DirectBaseline()
    {
        for (var i = 0; i < HundredThousand; i++)
            CompareSink.IntValue = _baseline.HandleAsync(_request).GetAwaiter().GetResult().Value;
    }

    [Benchmark(Description = "MessagePipe IRequestHandler - 10万次")]
    [BenchmarkCategory("Compare.Request", "MessagePipe")]
    public void MessagePipe()
    {
        for (var i = 0; i < HundredThousand; i++)
            CompareSink.IntValue = _messagePipeHandler.Invoke(_request).Value;
    }

    [Benchmark(Description = "LayerBase CallAsync - 10万次")]
    [BenchmarkCategory("Compare.Request", "LayerBase")]
    public void LayerBase()
    {
        for (var i = 0; i < HundredThousand; i++)
            CompareSink.IntValue = LayerHub.CallAsync<CompareCallLayer, CompareRequest, CompareResponse>(_request)
                .GetAwaiter().GetResult().Value;
    }
}

public class ManyNotifyFixedBatch32CompareBench : CompareBenchmarkBase
{
    [Params(2, 3)]
    public int SubscribersPerEvent { get; set; }

    private List<IDisposable> _messagePipeSubscriptions = null!;
    private ManyNotifyBatch32Publishers _publishers = null!;
    private System.IServiceProvider _provider = null!;

    [GlobalSetup]
    public void Setup()
    {
        _messagePipeSubscriptions = new List<IDisposable>(32 * SubscribersPerEvent);

        LayerHub.Reset();
        var layer = new CompareLayer();
        ManyNotifyFixedBatchRegistry.RegisterLayerBase32(layer, SubscribersPerEvent);
        LayerHub.CreateLayers().Push(layer).Build();

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddMessagePipe();
        _provider = services.BuildServiceProvider();
        _publishers = ManyNotifyFixedBatchRegistry.CreatePublishers32(_provider, SubscribersPerEvent, _messagePipeSubscriptions);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var subscription in _messagePipeSubscriptions)
            subscription.Dispose();

        ((IDisposable)_provider).Dispose();
        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "固定批次 Direct Notify (32事件/每事件2~3订阅)")]
    [BenchmarkCategory("Compare.ManyEventsFewNotifySubs", "Baseline", "Batch32")]
    public void DirectBaseline()
    {
        ManyNotifyFixedBatchRegistry.DispatchDirect32(SubscribersPerEvent);
    }

    [Benchmark(Description = "LayerBase SubscribeNotify 特性注册 (32事件/每事件2~3订阅)")]
    [BenchmarkCategory("Compare.ManyEventsFewNotifySubs", "LayerBase", "Batch32")]
    public void LayerBase()
    {
        ManyNotifyFixedBatchRegistry.DispatchLayerBase32();
    }

    [Benchmark(Description = "MessagePipe (32事件/每事件2~3订阅)")]
    [BenchmarkCategory("Compare.ManyEventsFewNotifySubs", "MessagePipe", "Batch32")]
    public void MessagePipe()
    {
        ManyNotifyFixedBatchRegistry.DispatchMessagePipe32(_publishers);
    }
}

public class ManyNotifyFixedBatch128CompareBench : CompareBenchmarkBase
{
    [Params(2, 3)]
    public int SubscribersPerEvent { get; set; }

    private List<IDisposable> _messagePipeSubscriptions = null!;
    private ManyNotifyBatch128Publishers _publishers = null!;
    private System.IServiceProvider _provider = null!;

    [GlobalSetup]
    public void Setup()
    {
        _messagePipeSubscriptions = new List<IDisposable>(128 * SubscribersPerEvent);

        LayerHub.Reset();
        var layer = new CompareLayer();
        ManyNotifyFixedBatchRegistry.RegisterLayerBase128(layer, SubscribersPerEvent);
        LayerHub.CreateLayers().Push(layer).Build();

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddMessagePipe();
        _provider = services.BuildServiceProvider();
        _publishers = ManyNotifyFixedBatchRegistry.CreatePublishers128(_provider, SubscribersPerEvent, _messagePipeSubscriptions);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var subscription in _messagePipeSubscriptions)
            subscription.Dispose();

        ((IDisposable)_provider).Dispose();
        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "固定批次 Direct Notify (128事件/每事件2~3订阅)")]
    [BenchmarkCategory("Compare.ManyEventsFewNotifySubs", "Baseline", "Batch128")]
    public void DirectBaseline()
    {
        ManyNotifyFixedBatchRegistry.DispatchDirect128(SubscribersPerEvent);
    }

    [Benchmark(Description = "LayerBase SubscribeNotify 特性注册 (128事件/每事件2~3订阅)")]
    [BenchmarkCategory("Compare.ManyEventsFewNotifySubs", "LayerBase", "Batch128")]
    public void LayerBase()
    {
        ManyNotifyFixedBatchRegistry.DispatchLayerBase128();
    }

    [Benchmark(Description = "MessagePipe (128事件/每事件2~3订阅)")]
    [BenchmarkCategory("Compare.ManyEventsFewNotifySubs", "MessagePipe", "Batch128")]
    public void MessagePipe()
    {
        ManyNotifyFixedBatchRegistry.DispatchMessagePipe128(_publishers);
    }
}

public class ManyNotifyFixedBatch256CompareBench : CompareBenchmarkBase
{
    [Params(2, 3)]
    public int SubscribersPerEvent { get; set; }

    private List<IDisposable> _messagePipeSubscriptions = null!;
    private ManyNotifyBatch256Publishers _publishers = null!;
    private System.IServiceProvider _provider = null!;

    [GlobalSetup]
    public void Setup()
    {
        _messagePipeSubscriptions = new List<IDisposable>(256 * SubscribersPerEvent);

        LayerHub.Reset();
        var layer = new CompareLayer();
        ManyNotifyFixedBatchRegistry.RegisterLayerBase256(layer, SubscribersPerEvent);
        LayerHub.CreateLayers().Push(layer).Build();

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddMessagePipe();
        _provider = services.BuildServiceProvider();
        _publishers = ManyNotifyFixedBatchRegistry.CreatePublishers256(_provider, SubscribersPerEvent, _messagePipeSubscriptions);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var subscription in _messagePipeSubscriptions)
            subscription.Dispose();

        ((IDisposable)_provider).Dispose();
        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "固定批次 Direct Notify (256事件/每事件2~3订阅)")]
    [BenchmarkCategory("Compare.ManyEventsFewNotifySubs", "Baseline", "Batch256")]
    public void DirectBaseline()
    {
        ManyNotifyFixedBatchRegistry.DispatchDirect256(SubscribersPerEvent);
    }

    [Benchmark(Description = "LayerBase SubscribeNotify 特性注册 (256事件/每事件2~3订阅)")]
    [BenchmarkCategory("Compare.ManyEventsFewNotifySubs", "LayerBase", "Batch256")]
    public void LayerBase()
    {
        ManyNotifyFixedBatchRegistry.DispatchLayerBase256();
    }

    [Benchmark(Description = "MessagePipe (256事件/每事件2~3订阅)")]
    [BenchmarkCategory("Compare.ManyEventsFewNotifySubs", "MessagePipe", "Batch256")]
    public void MessagePipe()
    {
        ManyNotifyFixedBatchRegistry.DispatchMessagePipe256(_publishers);
    }
}

public sealed class CSharpNotifyPublisher
{
    public event EventNotifyDelegate<NotifyPayload>? Notify;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Publish(in NotifyPayload payload)
    {
        Notify?.Invoke(in payload);
    }
}

public struct NotifyPayload
{
    public static readonly NotifyPayload Instance = default;
}

public struct CompareRequest
{
    public CompareRequest(int value) => Value = value;
    public int Value { get; set; }
}

public struct CompareResponse
{
    public CompareResponse(int value) => Value = value;
    public int Value { get; set; }
}

public sealed class CompareDirectBaseline
{
    public LBTask<CompareResponse> HandleAsync(CompareRequest request)
    {
        return LBTask<CompareResponse>.FromResult(new CompareResponse(request.Value + 1));
    }
}

public sealed class MessagePipeCompareHandler : IRequestHandler<CompareRequest, CompareResponse>
{
    public CompareResponse Invoke(CompareRequest request)
    {
        return new CompareResponse(request.Value + 1);
    }
}

public partial class CompareLayer : Layer { }
public partial class CompareCallLayer : Layer { }

[OwnerLayer(typeof(CompareCallLayer))]
public sealed class LayerBaseCompareHandler : ILayerCallHandler<CompareRequest, CompareResponse>
{
    public LBTask<CompareResponse> HandleAsync(CompareRequest request, CancellationToken cancellationToken = default)
    {
        return LBTask<CompareResponse>.FromResult(new CompareResponse(request.Value + 1));
    }
}

public partial class CompareNotifyManager : IService
{
    [SubscribeNotify]
    public void OnNotify(in NotifyPayload payload)
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {        
        services.AddSingleton(this);
    }
}
