using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using LayerBase.Scope;

namespace Benchmarks.Errors;

/// <summary>
/// LayerExceptionHub 运输基准。
/// 测量 Report、Drain、空队列、多生产者、溢出的成本。
/// Category: 09.Exception.Transport
/// </summary>
[MemoryDiagnoser]
[Config(typeof(HubBenchConfig))]
[BenchmarkCategory("09.Exception.Transport")]
public class LayerExceptionHubBenchmarks
{
    private LayerExceptionHub? _hub;
    private LayerExceptionHub? _smallHub;
    private LayerExceptionHub? _bigHub;
    private LayerHubExceptionCallbacks? _detailedSink;
    private LayerHubExceptionCallbacks? _legacySink;
    private LayerHubExceptionCallbacks? _bothSink;
    private LayerExceptionRecord _record;
    private List<LayerExceptionRecord> _records = null!;

    private readonly Exception _ex = new InvalidOperationException("Bench");

    [Params(1, 64, 512)]
    public int RecordCount;

    [GlobalSetup]
    public void Setup()
    {
        _hub = new LayerExceptionHub(capacity: 4096);
        _smallHub = new LayerExceptionHub(capacity: 2);
        _bigHub = new LayerExceptionHub(capacity: RecordCount + 64);

        _detailedSink = new LayerHubExceptionCallbacks();
        _detailedSink.OnExceptionRecord += r => { };

        _legacySink = new LayerHubExceptionCallbacks();
        _legacySink.OnException += _ => { };

        _bothSink = new LayerHubExceptionCallbacks();
        _bothSink.OnExceptionRecord += r => { };
        _bothSink.OnException += _ => { };

        _record = new LayerExceptionRecord(
            _ex, 1, 3, LayerExceptionPhase.CallDispatch,
            LayerQueueKind.CallInbox, 7, ScopeTrace.Empty,
            Environment.CurrentManagedThreadId, 500, 0, 0);

        _records = new List<LayerExceptionRecord>(RecordCount);
        for (int i = 0; i < RecordCount; i++)
        {
            _records.Add(new LayerExceptionRecord(
                new Exception($"E{i}"), 1, -1, LayerExceptionPhase.Continuation,
                LayerQueueKind.None, i, ScopeTrace.Empty,
                Environment.CurrentManagedThreadId, i, 0, 0));
        }
    }

    // ── Report ──

    [Benchmark(Description = "Report 1 record")]
    [BenchmarkCategory("09.Exception.Transport")]
    public void ReportOne()
    {
        _hub!.Report(_record);
    }

    [Benchmark(Description = "Report N records")]
    [BenchmarkCategory("09.Exception.Transport")]
    public void ReportBatch()
    {
        var h = _bigHub!;
        for (int i = 0; i < RecordCount; i++)
            h.Report(_records[i]);
    }

    // ── Drain ──

    [Benchmark(Description = "空 Drain（无订阅者）")]
    [BenchmarkCategory("09.Exception.Transport")]
    public void EmptyDrain_NoSubscriber()
    {
        _hub!.DrainAndDispatch(new LayerHubExceptionCallbacks());
    }

    [Benchmark(Description = "Drain N records -> detailed subscriber")]
    [BenchmarkCategory("09.Exception.Transport")]
    public void Drain_DetailedSubscriber()
    {
        var h = _bigHub!;
        for (int i = 0; i < RecordCount; i++) h.Report(_records[i]);
        h.DrainAndDispatch(_detailedSink!);
    }

    [Benchmark(Description = "Drain N records -> legacy subscriber")]
    [BenchmarkCategory("09.Exception.Transport")]
    public void Drain_LegacySubscriber()
    {
        var h = _bigHub!;
        for (int i = 0; i < RecordCount; i++) h.Report(_records[i]);
        h.DrainAndDispatch(_legacySink!);
    }

    [Benchmark(Description = "Drain N records -> detailed + legacy")]
    [BenchmarkCategory("09.Exception.Transport")]
    public void Drain_BothSubscribers()
    {
        var h = _bigHub!;
        for (int i = 0; i < RecordCount; i++) h.Report(_records[i]);
        h.DrainAndDispatch(_bothSink!);
    }

    // ── 空队列 ──

    [Benchmark(Description = "空 Drain（空 Hub）")]
    [BenchmarkCategory("09.Exception.Transport")]
    public void EmptyDrain()
    {
        _hub!.DrainAndDispatch(_detailedSink!);
    }

    // ── 溢出 ──

    [Benchmark(Description = "Capacity=2 Report 5")]
    [BenchmarkCategory("09.Exception.Transport.Overflow")]
    public void Overflow_Capacity2_Report5()
    {
        var h = _smallHub!;
        for (int i = 0; i < 5; i++)
            h.Report(_record);
        h.DrainAndDispatch(_detailedSink!);
    }

    [Benchmark(Description = "Capacity=512 Report 1024")]
    [BenchmarkCategory("09.Exception.Transport.Overflow")]
    public void Overflow_Capacity512_Report1024()
    {
        var h = new LayerExceptionHub(capacity: 512);
        for (int i = 0; i < 1024; i++)
            h.Report(_record);
        h.DrainAndDispatch(_detailedSink!);
    }

    // ── 空转 ──

    [Benchmark(Description = "Runtime Pump 空 ExceptionHub (模拟)")]
    [BenchmarkCategory("09.Exception.Transport")]
    public void RuntimePump_EmptyExceptionHub()
    {
        for (int i = 0; i < 64; i++)
            _hub!.DrainAndDispatch(_detailedSink!);
    }
}

public sealed class HubBenchConfig : ManualConfig
{
    public HubBenchConfig()
    {
        AddJob(Job.ShortRun);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max);
    }
}
