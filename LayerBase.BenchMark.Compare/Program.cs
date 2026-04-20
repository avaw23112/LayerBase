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
        layer.SubscribeNotify<NotifyPayload>(static (in NotifyPayload _) => { });
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
    [Params(1, 4, 16, 64)]
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
            layer.SubscribeNotify<NotifyPayload>(static (in NotifyPayload _) => { });
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
