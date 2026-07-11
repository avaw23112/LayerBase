using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using LayerBase.Core.DataStruct;

namespace Benchmarks.Scope;

/// <summary>
/// 有限环形队列单片基准。
/// Category: 08.Scope.Queue
/// 测量 LocalRingQueue（无锁）与 LockedBoundedRingQueue（带锁）的
/// 入队、出队、竞争、空满检查成本。
/// struct 消息尺寸从 4B (int) 到 64B (LargeMsg)。
/// </summary>
[MemoryDiagnoser]
[Config(typeof(QueueBenchConfig))]
[BenchmarkCategory("08.Scope.Queue")]
public class ScopeQueueBenchmarks
{
    [Params(64, 1024, 16384)]
    public int Capacity;

    [Params(1, 64, 1024)]
    public int BatchSize;

    private LocalRingQueue<int> _local = null!;
    private LockedBoundedRingQueue<int> _locked = null!;
    private LockedBoundedRingQueue<LargeMsg> _lockedLarge = null!;
    private ConcurrentQueue<int> _concurrent = null!;
    private int[] _items = null!;

    [GlobalSetup]
    public void Setup()
    {
        _items = new int[BatchSize];
        for (int i = 0; i < BatchSize; i++) _items[i] = i;
        _local = new LocalRingQueue<int>(Capacity);
        _locked = new LockedBoundedRingQueue<int>(Capacity);
        _lockedLarge = new LockedBoundedRingQueue<LargeMsg>(Capacity);
        _concurrent = new ConcurrentQueue<int>();
    }

    // ── LocalRingQueue（无锁）──

    [Benchmark(Description = "Local 入队 int x BatchSize")]
    [BenchmarkCategory("08.Scope.Queue")]
    public void Local_EnqueueOnly()
    {
        var q = _local;
        for (int i = 0; i < BatchSize; i++) q.TryEnqueue(i);
    }

    [Benchmark(Description = "Local 往返 int x BatchSize")]
    [BenchmarkCategory("08.Scope.Queue")]
    public void Local_RoundTrip()
    {
        var q = _local;
        for (int i = 0; i < BatchSize; i++) { q.TryEnqueue(i); q.TryDequeue(out _); }
    }

    // ── LockedBoundedRingQueue（单线程）──

    [Benchmark(Description = "Locked 入队 int x BatchSize")]
    [BenchmarkCategory("08.Scope.Queue")]
    public void Locked_EnqueueOnly()
    {
        var q = _locked;
        for (int i = 0; i < BatchSize; i++) q.TryEnqueue(i);
    }

    [Benchmark(Description = "Locked 往返 int x BatchSize")]
    [BenchmarkCategory("08.Scope.Queue")]
    public void Locked_RoundTrip()
    {
        var q = _locked;
        for (int i = 0; i < BatchSize; i++) { q.TryEnqueue(i); q.TryDequeue(out _); }
    }

    [Benchmark(Description = "Locked 空出队")]
    [BenchmarkCategory("08.Scope.Queue")]
    public void Locked_EmptyDequeue()
    {
        _locked.TryDequeue(out _);
    }

    [Benchmark(Description = "Locked 满入队失败")]
    [BenchmarkCategory("08.Scope.Queue")]
    public void Locked_FullEnqueueFailure()
    {
        var q = _locked;
        for (int i = 0; i < Capacity; i++) q.TryEnqueue(i);
        q.TryEnqueue(-1);
    }

    [Benchmark(Description = "Locked Count 读取")]
    [BenchmarkCategory("08.Scope.Queue")]
    public void Locked_CountRead()
    {
        var c = _locked.Count;
        Volatile.Write(ref BenchmarkSink.IntValue, c);
    }

    // ── 大 struct 复制成本 ──

    [Benchmark(Description = "Locked LargeMsg 往返 x BatchSize")]
    [BenchmarkCategory("08.Scope.Queue")]
    public void Locked_LargeMsg_RoundTrip()
    {
        var q = _lockedLarge;
        for (int i = 0; i < BatchSize; i++)
        {
            q.TryEnqueue(new LargeMsg(i, i + 1, i + 2, i + 3, _items));
            q.TryDequeue(out _);
        }
    }

    // ── ConcurrentQueue 对照 ──

    [Benchmark(Description = "ConcurrentQueue 往返 int x BatchSize (对照)")]
    [BenchmarkCategory("08.Scope.Queue")]
    public void Concurrent_RoundTrip()
    {
        var q = _concurrent;
        for (int i = 0; i < BatchSize; i++) { q.Enqueue(i); q.TryDequeue(out _); }
    }
}

// 模拟 PostMessage 大小 (object payload)
public readonly struct LargeMsg
{
    public readonly long A, B, C, D;
    public readonly object Ref;
    public LargeMsg(long a, long b, long c, long d, object r) { A = a; B = b; C = c; D = d; Ref = r; }
}

public sealed class QueueBenchConfig : ManualConfig
{
    public QueueBenchConfig()
    {
        AddJob(Job.ShortRun);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max, RankColumn.Arabic);
    }
}
