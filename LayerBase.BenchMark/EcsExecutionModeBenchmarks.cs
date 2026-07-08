using System.Runtime.CompilerServices;
using System.Diagnostics;
using Arch.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using LayerBase;
using LayerBase.Actor;
using LayerBase.Core;
using LayerBase.ECS;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Flow;
using LayerBase.ECS.Runtime.Queues;
using LayerBase.ECS.Runtime.Submission;
using LayerBase.ECS.Runtime;
using LayerBase.Layers;

namespace Benchmarks;

[MemoryDiagnoser]
[CategoriesColumn]
[RankColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EcsSpscBatchBenchmarks
{
    private SpscRing<EcsSubmissionBatch> _ring = null!;
    private SpscRing<EcsResultBatch> _resultRing = null!;
    private EcsSubmissionBatch _batch = null!;
    private EcsSubmissionBatch _recordBatch = null!;
    private NoopEcsWorkItem _workItem = null!;
    private SmallArenaJob _arenaJob;
    private EcsResultBatch _resultBatch = null!;
    private EcsResultBatch _recordResultBatch = null!;
    private NoopEcsResultItem _resultItem = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ring = new SpscRing<EcsSubmissionBatch>(1024);
        _resultRing = new SpscRing<EcsResultBatch>(1024);
        _batch = new EcsSubmissionBatch(1);
        _recordBatch = new EcsSubmissionBatch(1024);
        _workItem = new NoopEcsWorkItem();
        _arenaJob = new SmallArenaJob(16);
        _resultBatch = new EcsResultBatch(1);
        _recordResultBatch = new EcsResultBatch(1024);
        _resultItem = new NoopEcsResultItem();
    }

    [Benchmark(OperationsPerInvoke = 1024, Description = "Raw SPSC Batch RoundTrip - 1024")]
    [BenchmarkCategory("07.ECS.SPSC", "RawSpscBatchEnqueue")]
    public void RawSpscBatchRoundTrip_1024()
    {
        for (int i = 0; i < 1024; i++)
        {
            _ring.TryEnqueue(_batch);
            _ring.TryDequeue(out _);
        }
    }

    [Benchmark(OperationsPerInvoke = 1024, Description = "SubmissionBatch Record WorkItem - 1024")]
    [BenchmarkCategory("07.ECS.SPSC", "SubmissionBatchRecord")]
    public void SubmissionBatch_RecordWorkItem_1024()
    {
        for (int i = 0; i < 1024; i++)
        {
            _recordBatch.Add(_workItem);
        }

        _recordBatch.Clear();
    }

    [Benchmark(OperationsPerInvoke = 1024, Description = "SubmissionBatch Record WorkRecord - 1024")]
    [BenchmarkCategory("07.ECS.SPSC", "SubmissionBatchRecord")]
    public void SubmissionBatch_RecordWorkRecord_1024()
    {
        for (int i = 0; i < 1024; i++)
        {
            int jobOffset = _recordBatch.JobArena.Store(in _arenaJob);
            var record = new EcsWorkRecord(0, null!, null, jobOffset);
            _recordBatch.AddRecord(in record);
        }

        _recordBatch.Clear();
    }

    [Benchmark(OperationsPerInvoke = 1024, Description = "Raw SPSC ResultBatch RoundTrip - 1024")]
    [BenchmarkCategory("07.ECS.SPSC", "RawSpscResultBatchEnqueue")]
    public void RawSpscResultBatchRoundTrip_1024()
    {
        for (int i = 0; i < 1024; i++)
        {
            _resultRing.TryEnqueue(_resultBatch);
            _resultRing.TryDequeue(out _);
        }
    }

    [Benchmark(OperationsPerInvoke = 1024, Description = "ResultBatch Record ResultItem - 1024")]
    [BenchmarkCategory("07.ECS.SPSC", "ResultBatchRecord")]
    public void ResultBatch_RecordResultItem_1024()
    {
        for (int i = 0; i < 1024; i++)
        {
            _recordResultBatch.Add(_resultItem);
        }

        _recordResultBatch.Clear(disposeItems: false);
    }

    private sealed class NoopEcsWorkItem : IEcsWorkItem
    {
        public string DebugName => "Noop";

        public void Execute(World world, EcsResultQueue results)
        {
        }
    }

    private sealed class NoopEcsResultItem : IEcsResultItem
    {
        public string DebugName => "Noop";

        public void Apply(LayerRuntime runtime)
        {
        }
    }

    private readonly struct SmallArenaJob
    {
        private readonly int _workIterations;

        public SmallArenaJob(int workIterations)
        {
            _workIterations = workIterations;
        }
    }
}

[MemoryDiagnoser]
[CategoriesColumn]
[RankColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EcsExecutionModeBenchmarks
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);
    private const int SubmitOperations = 1024;

    private LayerRuntime _syncRuntime = null!;
    private LayerRuntime _asyncRuntime = null!;
    private AsyncEcsScheduler _asyncScheduler = null!;
    private NoopEcsWorkItem _noopWorkItem = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int EntityCount { get; set; }

    [Params(0, 8, 32, 128, 512)]
    public int WorkIterations { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        _syncRuntime = EcsBenchmarkWorldFactory.CreateRuntime(EcsExecutionMode.Sync);
        _asyncRuntime = EcsBenchmarkWorldFactory.CreateRuntime(EcsExecutionMode.Async);
        _asyncScheduler = (AsyncEcsScheduler)_asyncRuntime.EcsWorkScheduler;
        _noopWorkItem = new NoopEcsWorkItem();
        EcsBenchmarkWorldFactory.PopulatePlainQueryWorld(_syncRuntime, EntityCount);
        EcsBenchmarkWorldFactory.PopulatePlainQueryWorld(_asyncRuntime, EntityCount);
        EnsureAsyncSubmitCapacity();

        _asyncScheduler.Schedule(_noopWorkItem);
        long fence = _asyncScheduler.FlushSubmissionsForTest();
        _asyncRuntime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "Sync PlainQuery Execute")]
    [BenchmarkCategory("07.ECS.ExecutionMode", "PlainQuery", "Sync")]
    public void Sync_PlainQuery_Execute()
    {
        var job = new MoveWithWorkJob(WorkIterations);
        _syncRuntime.EcsWorld
                    .Query<BenchPosition, BenchVelocity>()
                    .ForEach(ref job);
    }

    [IterationSetup(Target = nameof(Async_PlainQuery_SubmitOnly))]
    public void PrepareAsyncPlainSubmitOnly()
    {
        EnsureAsyncSubmitCapacity();
    }

    [Benchmark(OperationsPerInvoke = SubmitOperations, Description = "Async PlainQuery SubmitOnly")]
    [BenchmarkCategory("07.ECS.ExecutionMode", "PlainQuery", "AsyncSubmit")]
    public void Async_PlainQuery_SubmitOnly()
    {
        for (int i = 0; i < SubmitOperations; i++)
        {
            var job = new MoveWithWorkJob(0);
            _asyncRuntime.EcsWorld
                         .Query<BenchPosition, BenchVelocity>()
                         .ForEach(ref job);
        }
    }

    [IterationCleanup(Target = nameof(Async_PlainQuery_SubmitOnly))]
    public void CleanupAsyncPlainSubmitOnly()
    {
        long fence = _asyncRuntime.FlushEcsSubmissionsForTest();
        _asyncRuntime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    [IterationSetup(Target = nameof(Async_PlainQuery_EndToEnd))]
    public void PrepareAsyncPlainEndToEnd()
    {
        _asyncScheduler.Schedule(_noopWorkItem);
        long fence = _asyncScheduler.FlushSubmissionsForTest();
        _asyncRuntime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    [Benchmark(Description = "Async PlainQuery WarmWorker EndToEnd")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.ExecutionMode", "PlainQuery", "AsyncEndToEnd")]
    public void Async_PlainQuery_EndToEnd()
    {
        var job = new MoveWithWorkJob(WorkIterations);
        _asyncRuntime.EcsWorld
                     .Query<BenchPosition, BenchVelocity>()
                     .ForEach(ref job);

        long fence = _asyncRuntime.FlushEcsSubmissionsForTest();
        _asyncRuntime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    private void EnsureAsyncSubmitCapacity()
    {
        _asyncScheduler.EnsureCurrentSubmissionCapacityForTest(
            SubmitOperations,
            SubmitOperations * Unsafe.SizeOf<MoveWithWorkJob>());
    }

    private sealed class NoopEcsWorkItem : IEcsWorkItem
    {
        public string DebugName => "Noop";

        public void Execute(World world, EcsResultQueue results)
        {
        }
    }
}

[MemoryDiagnoser]
[CategoriesColumn]
[RankColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EcsAsyncSubmitBoundaryBenchmarks
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    private SpscRing<EcsSubmissionBatch> _rawRing = null!;
    private EcsSubmissionBatch _batch = null!;
    private EcsSubmissionBatch _recordBatch = null!;
    private EcsWorkRecord _record;
    private MoveWithWorkJob _job;
    private LayerRuntime _runtime = null!;
    private AsyncEcsScheduler _scheduler = null!;
    private NoopEcsWorkItem _noopWorkItem = null!;
    private WakeProbeEcsWorkItem _wakeProbe = null!;
    private long _completedFence;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        _rawRing = new SpscRing<EcsSubmissionBatch>(1024);
        _batch = new EcsSubmissionBatch(1);
        _recordBatch = new EcsSubmissionBatch(1024);
        _record = new EcsWorkRecord(0, null!, null, 0);
        _job = new MoveWithWorkJob(0);
        _runtime = EcsBenchmarkWorldFactory.CreateRuntime(EcsExecutionMode.Async);
        EcsBenchmarkWorldFactory.PopulatePlainQueryWorld(_runtime, 1_000);
        _scheduler = (AsyncEcsScheduler)_runtime.EcsWorkScheduler;
        _noopWorkItem = new NoopEcsWorkItem();
        _wakeProbe = new WakeProbeEcsWorkItem();
        _completedFence = _runtime.FlushEcsSubmissionsForTest();

        var warmupJob = new MoveWithWorkJob(0);
        _runtime.EcsWorld
                .Query<BenchPosition, BenchVelocity>()
                .ForEach(ref warmupJob);
        _runtime.WaitEcsIdleForTest(IdleTimeout);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        LayerHub.Reset();
    }

    [IterationSetup(Target = nameof(RawSpscBatchEnqueueBenchmark))]
    public void ResetRawSpscBatchEnqueue()
    {
        _rawRing = new SpscRing<EcsSubmissionBatch>(1024);
    }

    [Benchmark(Description = "RawSpscBatchEnqueueBenchmark")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "RawSpsc", "SubmitOnly")]
    public void RawSpscBatchEnqueueBenchmark()
    {
        _rawRing.TryEnqueue(_batch);
    }

    [Benchmark(OperationsPerInvoke = 1024, Description = "SubmissionBatchAddRecordBenchmark")]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "Record", "SubmitOnly")]
    public void SubmissionBatchAddRecordBenchmark()
    {
        for (int i = 0; i < 1024; i++)
        {
            _recordBatch.AddRecord(in _record);
        }

        _recordBatch.Clear();
    }

    [Benchmark(OperationsPerInvoke = 1024, Description = "JobArenaStoreBenchmark")]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "Record", "SubmitOnly")]
    public void JobArenaStoreBenchmark()
    {
        for (int i = 0; i < 1024; i++)
        {
            _recordBatch.JobArena.Store(in _job);
        }

        _recordBatch.Clear();
    }

    [Benchmark(OperationsPerInvoke = 1024, Description = "RecordPlainQueryOnlyBenchmark")]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "Record", "SubmitOnly")]
    public void RecordPlainQueryOnlyBenchmark()
    {
        for (int i = 0; i < 1024; i++)
        {
            int jobOffset = _recordBatch.JobArena.Store(in _job);
            var record = new EcsWorkRecord(0, null!, null, jobOffset);
            _recordBatch.AddRecord(in record);
        }

        _recordBatch.Clear();
    }

    [IterationSetup(Target = nameof(FlushSubmissionsOnlyBenchmark))]
    public void PrepareFlushSubmissionsOnly()
    {
        _scheduler.Schedule(_noopWorkItem);
    }

    [Benchmark(Description = "FlushSubmissionsOnlyBenchmark")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "Flush", "Signal")]
    public void FlushSubmissionsOnlyBenchmark()
    {
        _scheduler.FlushSubmissions();
    }

    [IterationCleanup(Target = nameof(FlushSubmissionsOnlyBenchmark))]
    public void CleanupFlushSubmissionsOnly()
    {
        _runtime.WaitEcsIdleForTest(IdleTimeout);
    }

    [Benchmark(Description = "QueryFlowAsyncForEachBenchmark")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "QueryFlow", "SubmitOnly")]
    public void QueryFlowAsyncForEachBenchmark()
    {
        var job = new MoveWithWorkJob(0);
        _runtime.EcsWorld
                .Query<BenchPosition, BenchVelocity>()
                .ForEach(ref job);
    }

    [IterationCleanup(Target = nameof(QueryFlowAsyncForEachBenchmark))]
    public void CleanupQueryFlowAsyncForEach()
    {
        _runtime.WaitEcsIdleForTest(IdleTimeout);
    }

    [Benchmark(Description = "SignalOnlyBenchmark")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "Signal")]
    public void SignalOnlyBenchmark()
    {
        _scheduler.SignalForTest();
    }

    [IterationSetup(Target = nameof(SignalOnlyBenchmark))]
    public void ParkWorkerForSignalOnly()
    {
        _scheduler.WaitWorkerParkedForTest(IdleTimeout);
    }

    [Benchmark(Description = "FenceWaitAlreadyCompletedBenchmark")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "Fence", "Wait")]
    public void FenceWaitAlreadyCompletedBenchmark()
    {
        _runtime.WaitEcsFenceForTest(_completedFence, IdleTimeout);
    }

    [Benchmark(Description = "FenceWaitWorkerBusyBenchmark")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "Fence", "Wait", "Signal")]
    public void FenceWaitWorkerBusyBenchmark()
    {
        _scheduler.Schedule(_noopWorkItem);
        long fence = _scheduler.FlushSubmissionsForTest();
        _runtime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    [IterationSetup(Target = nameof(WarmWorkerEndToEndBenchmark))]
    public void PrepareWarmWorkerEndToEnd()
    {
        _scheduler.Schedule(_noopWorkItem);
        long fence = _scheduler.FlushSubmissionsForTest();
        _runtime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    [Benchmark(Description = "WarmWorker EndToEnd")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "Fence", "EndToEnd", "WarmWorker")]
    public void WarmWorkerEndToEndBenchmark()
    {
        var job = new MoveWithWorkJob(0);
        _runtime.EcsWorld
                .Query<BenchPosition, BenchVelocity>()
                .ForEach(ref job);

        long fence = _runtime.FlushEcsSubmissionsForTest();
        _runtime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    [IterationSetup(Target = nameof(ColdWorkerWakeLatencyBenchmark))]
    public void ParkWorkerForColdWakeLatency()
    {
        _scheduler.WaitWorkerParkedForTest(IdleTimeout);
    }

    [Benchmark(Description = "ColdWorkerWakeLatencyBenchmark")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.AsyncSubmitBoundary", "WakeLatency", "Signal", "Wait", "ColdWorker")]
    public void ColdWorkerWakeLatencyBenchmark()
    {
        _wakeProbe.Reset();
        long submitted = Stopwatch.GetTimestamp();
        _scheduler.Schedule(_wakeProbe);
        long fence = _scheduler.FlushSubmissionsForTest();

        SpinWait spin = default;
        while (_wakeProbe.StartedTimestamp == 0)
        {
            spin.SpinOnce();
        }

        EcsBenchmarkSink.FloatValue =
            (float)((_wakeProbe.StartedTimestamp - submitted) * 1_000_000.0 / Stopwatch.Frequency);
        _runtime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    private sealed class NoopEcsWorkItem : IEcsWorkItem
    {
        public string DebugName => "Noop";

        public void Execute(World world, EcsResultQueue results)
        {
        }
    }

    private sealed class WakeProbeEcsWorkItem : IEcsWorkItem
    {
        private long _startedTimestamp;

        public string DebugName => "WakeProbe";

        public long StartedTimestamp => Volatile.Read(ref _startedTimestamp);

        public void Reset()
        {
            Volatile.Write(ref _startedTimestamp, 0);
        }

        public void Execute(World world, EcsResultQueue results)
        {
            Volatile.Write(ref _startedTimestamp, Stopwatch.GetTimestamp());
        }
    }
}

[MemoryDiagnoser]
[CategoriesColumn]
[RankColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EcsPlainQuerySubmitHotPathBenchmarks
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    private LayerRuntime _asyncRuntime = null!;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        _asyncRuntime = EcsBenchmarkWorldFactory.CreateRuntime(EcsExecutionMode.Async);
        EcsBenchmarkWorldFactory.PopulatePlainQueryWorld(_asyncRuntime, 1_000);

        var warmupJob = new MoveWithWorkJob(0);
        _asyncRuntime.EcsWorld
                     .Query<BenchPosition, BenchVelocity>()
                     .ForEach(ref warmupJob);
        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        LayerHub.Reset();
    }

    [Benchmark(Description = "Async PlainQuery SubmitOnly HotPath")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.ExecutionMode", "PlainQuery", "AsyncSubmit", "HotPath")]
    public void Async_PlainQuery_SubmitOnly_HotPath()
    {
        var job = new MoveWithWorkJob(0);
        _asyncRuntime.EcsWorld
                     .Query<BenchPosition, BenchVelocity>()
                     .ForEach(ref job);
    }

    [IterationCleanup(Target = nameof(Async_PlainQuery_SubmitOnly_HotPath))]
    public void CleanupAsyncPlainSubmitOnly()
    {
        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
    }
}

[MemoryDiagnoser]
[CategoriesColumn]
[RankColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EcsPlainQuerySubmitPathBenchmarks
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);
    private const int SubmitOperations = 1024;

    private LayerRuntime _asyncRuntime = null!;
    private AsyncEcsScheduler _asyncScheduler = null!;
    private int _plainQueryId;
    private GeneratedPlainQueryBenchService _generatedService = null!;

    [Params(0, 8, 32)]
    public int WorkIterations { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        _asyncRuntime = EcsBenchmarkWorldFactory.CreateRuntime(EcsExecutionMode.Async);
        _asyncScheduler = (AsyncEcsScheduler)_asyncRuntime.EcsWorkScheduler;
        EcsBenchmarkWorldFactory.PopulatePlainQueryWorld(_asyncRuntime, 1_000);
        _plainQueryId = _asyncRuntime.EcsQueryRegistry.GetOrCreate<BenchPosition, BenchVelocity>();
        _generatedService = new GeneratedPlainQueryBenchService();
        ((IGeneratedEcsQueryRegistrar)_generatedService).RegisterGeneratedEcsQueries(_asyncRuntime);
        PrepareSubmitBatch();

        var warmupJob = new MoveWithWorkJob(0);
        _asyncRuntime.EcsWorld
                     .Query<BenchPosition, BenchVelocity>()
                     .ForEach(ref warmupJob);
        _asyncRuntime.EcsScheduler.SubmitPlainQuery<MoveWithWorkJob, BenchPosition, BenchVelocity>(
            _plainQueryId,
            0,
            in warmupJob);
        _generatedService.GeneratedPlainMove(0);
        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        LayerHub.Reset();
    }

    [IterationSetup(
        Targets = new[]
        {
            nameof(Async_PublicQueryApi_SubmitOnly),
            nameof(Async_DirectSubmit_SubmitOnly),
            nameof(Async_GeneratedQuery_SubmitOnly)
        })]
    public void PrepareSubmitBatch()
    {
        _asyncScheduler.EnsureCurrentSubmissionCapacityForTest(
            SubmitOperations,
            SubmitOperations * Unsafe.SizeOf<MoveWithWorkJob>());
    }

    [Benchmark(OperationsPerInvoke = SubmitOperations, Description = "Async PublicQueryApi SubmitOnly")]
    [BenchmarkCategory("07.ECS.SubmitPath", "PublicQueryApi", "SubmitOnly")]
    public void Async_PublicQueryApi_SubmitOnly()
    {
        for (int i = 0; i < SubmitOperations; i++)
        {
            var job = new MoveWithWorkJob(WorkIterations);
            _asyncRuntime.EcsWorld
                         .Query<BenchPosition, BenchVelocity>()
                         .ForEach(ref job);
        }
    }

    [IterationCleanup(Target = nameof(Async_PublicQueryApi_SubmitOnly))]
    public void CleanupPublicQueryApiSubmitOnly()
    {
        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
    }

    [Benchmark(OperationsPerInvoke = SubmitOperations, Description = "Async DirectSubmit SubmitOnly")]
    [BenchmarkCategory("07.ECS.SubmitPath", "DirectSubmit", "SubmitOnly")]
    public void Async_DirectSubmit_SubmitOnly()
    {
        for (int i = 0; i < SubmitOperations; i++)
        {
            var job = new MoveWithWorkJob(WorkIterations);
            _asyncRuntime.EcsScheduler.SubmitPlainQuery<MoveWithWorkJob, BenchPosition, BenchVelocity>(
                _plainQueryId,
                0,
                in job);
        }
    }

    [IterationCleanup(Target = nameof(Async_DirectSubmit_SubmitOnly))]
    public void CleanupDirectSubmitOnly()
    {
        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
    }

    [Benchmark(OperationsPerInvoke = SubmitOperations, Description = "Async GeneratedQuery SubmitOnly")]
    [BenchmarkCategory("07.ECS.SubmitPath", "GeneratedQuery", "SubmitOnly")]
    public void Async_GeneratedQuery_SubmitOnly()
    {
        for (int i = 0; i < SubmitOperations; i++)
        {
            _generatedService.GeneratedPlainMove(WorkIterations);
        }
    }

    [IterationCleanup(Target = nameof(Async_GeneratedQuery_SubmitOnly))]
    public void CleanupGeneratedQuerySubmitOnly()
    {
        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
    }
}

[MemoryDiagnoser]
[CategoriesColumn]
[RankColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EcsAsyncBringBenchmarks
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    private LayerRuntime _syncRuntime = null!;
    private LayerRuntime _asyncRuntime = null!;
    private AsyncEcsScheduler _asyncScheduler = null!;
    private NoopEcsWorkItem _noopWorkItem = null!;

    [Params(1_000, 10_000, 100_000)]
    public int EntityCount { get; set; }

    [Params(0, 16, 128, 512)]
    public int WorkIterations { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        BenchProjectedActor.MoveCount = 0;
        _syncRuntime = EcsBenchmarkWorldFactory.CreateRuntime(EcsExecutionMode.Sync);
        _asyncRuntime = EcsBenchmarkWorldFactory.CreateRuntime(EcsExecutionMode.Async);
        _asyncScheduler = (AsyncEcsScheduler)_asyncRuntime.EcsWorkScheduler;
        _noopWorkItem = new NoopEcsWorkItem();
        EcsBenchmarkWorldFactory.PopulateBringWorld(_syncRuntime, EntityCount);
        EcsBenchmarkWorldFactory.PopulateBringWorld(_asyncRuntime, EntityCount);
        WarmupProjectedActors(_syncRuntime);
        WarmupProjectedActors(_asyncRuntime);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "Sync BringQuery ExecuteAndPost")]
    [BenchmarkCategory("08.ECS.Bring", "Sync", "ExecuteAndPost")]
    public void Sync_BringQuery_ExecuteAndPost()
    {
        var job = new MoveViewJob(WorkIterations);
        _syncRuntime.EcsWorld
                    .Query<BenchPosition, BenchVelocity, BenchAoi>()
                    .Bring<BenchMoveViewEvent>()
                    .ForEach(ref job)
                    .Batch()
                    .Post();

        _syncRuntime.Pump(0.016f);
    }

    [Benchmark(Description = "Async BringQuery SubmitOnly")]
    [InvocationCount(1)]
    [BenchmarkCategory("08.ECS.Bring", "AsyncSubmit", "SubmitOnly")]
    public void Async_BringQuery_SubmitOnly()
    {
        var job = new MoveViewJob(WorkIterations);
        _asyncRuntime.EcsWorld
                     .Query<BenchPosition, BenchVelocity, BenchAoi>()
                     .Bring<BenchMoveViewEvent>()
                     .ForEach(ref job)
                     .Batch()
                     .Post();
    }

    [IterationCleanup(Target = nameof(Async_BringQuery_SubmitOnly))]
    public void CleanupAsyncBringSubmitOnly()
    {
        long fence = _asyncRuntime.FlushEcsSubmissionsForTest();
        _asyncRuntime.WaitEcsFenceForTest(fence, IdleTimeout);
        _asyncRuntime.Pump(0.016f);
    }

    [IterationSetup(Target = nameof(Async_BringQuery_EndToEnd))]
    public void PrepareAsyncBringEndToEnd()
    {
        _asyncScheduler.Schedule(_noopWorkItem);
        long fence = _asyncScheduler.FlushSubmissionsForTest();
        _asyncRuntime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    [Benchmark(Description = "Async BringQuery WarmWorker EndToEnd")]
    [BenchmarkCategory("08.ECS.Bring", "AsyncEndToEnd", "ExecuteDrainPost")]
    public void Async_BringQuery_EndToEnd()
    {
        var job = new MoveViewJob(WorkIterations);
        _asyncRuntime.EcsWorld
                     .Query<BenchPosition, BenchVelocity, BenchAoi>()
                     .Bring<BenchMoveViewEvent>()
                     .ForEach(ref job)
                     .Batch()
                     .Post();

        long fence = _asyncRuntime.FlushEcsSubmissionsForTest();
        _asyncRuntime.WaitEcsFenceForTest(fence, IdleTimeout);
        _asyncRuntime.Pump(0.016f);
    }

    [IterationSetup(Target = nameof(Async_BringQuery_DrainOnly))]
    public void SeedAsyncBringDrainOnly()
    {
        var job = new MoveViewJob(WorkIterations);
        _asyncRuntime.EcsWorld
                     .Query<BenchPosition, BenchVelocity, BenchAoi>()
                     .Bring<BenchMoveViewEvent>()
                     .ForEach(ref job)
                     .Batch()
                     .Post();

        long fence = _asyncRuntime.FlushEcsSubmissionsForTest();
        _asyncRuntime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    [Benchmark(Description = "Async BringQuery DrainOnly")]
    [InvocationCount(1)]
    [BenchmarkCategory("08.ECS.Bring", "AsyncDrain", "DrainOnly")]
    public void Async_BringQuery_DrainOnly()
    {
        var stats = _asyncRuntime.EcsScheduler.DrainResults(int.MaxValue);
        EcsBenchmarkSink.IntValue = stats.Drained;
    }

    [IterationCleanup(Target = nameof(Async_BringQuery_DrainOnly))]
    public void CleanupAsyncBringDrainOnly()
    {
        _asyncRuntime.Pump(0.016f);
    }

    private static void WarmupProjectedActors(LayerRuntime runtime)
    {
        var job = new MoveViewJob(0);
        runtime.EcsWorld
               .Query<BenchPosition, BenchVelocity, BenchAoi>()
               .Bring<BenchMoveViewEvent>()
               .ForEach(ref job)
               .Batch()
               .Post();

        if (runtime.EcsOptions.ExecutionMode == EcsExecutionMode.Async)
        {
            runtime.WaitEcsIdleForTest(IdleTimeout);
        }

        runtime.Pump(0.016f);
    }

    private sealed class NoopEcsWorkItem : IEcsWorkItem
    {
        public string DebugName => "Noop";

        public void Execute(World world, EcsResultQueue results)
        {
        }
    }
}

[MemoryDiagnoser]
[CategoriesColumn]
[RankColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EcsFrameSimulationBenchmarks
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    private LayerRuntime _syncRuntime = null!;
    private LayerRuntime _asyncRuntime = null!;

    [Params(1_000, 10_000, 100_000)]
    public int EntityCount { get; set; }

    [Params(0, 32, 128)]
    public int WorkIterations { get; set; }

    [Params(60)]
    public int FrameCount { get; set; }

    [Params(0, 256)]
    public int MainThreadWorkIterations { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        _syncRuntime = EcsBenchmarkWorldFactory.CreateRuntime(EcsExecutionMode.Sync);
        _asyncRuntime = EcsBenchmarkWorldFactory.CreateRuntime(EcsExecutionMode.Async);
        EcsBenchmarkWorldFactory.PopulatePlainQueryWorld(_syncRuntime, EntityCount);
        EcsBenchmarkWorldFactory.PopulatePlainQueryWorld(_asyncRuntime, EntityCount);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        LayerHub.Reset();
    }

    [Benchmark(Baseline = true, Description = "Sync FrameLoop")]
    [BenchmarkCategory("09.ECS.FrameLoop", "Sync", "Frame")]
    public void Sync_FrameLoop()
    {
        for (int frame = 0; frame < FrameCount; frame++)
        {
            var job = new MoveWithWorkJob(WorkIterations);
            _syncRuntime.EcsWorld
                        .Query<BenchPosition, BenchVelocity>()
                        .ForEach(ref job);

            SimulateMainThreadWork(MainThreadWorkIterations);
            _syncRuntime.Pump(0.016f);
        }
    }

    [Benchmark(Description = "Async FrameLoop")]
    [BenchmarkCategory("09.ECS.FrameLoop", "Async", "Frame")]
    public void Async_FrameLoop()
    {
        for (int frame = 0; frame < FrameCount; frame++)
        {
            var job = new MoveWithWorkJob(WorkIterations);
            _asyncRuntime.EcsWorld
                         .Query<BenchPosition, BenchVelocity>()
                         .ForEach(ref job);

            SimulateMainThreadWork(MainThreadWorkIterations);
            _asyncRuntime.Pump(0.016f);
        }

        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
        _asyncRuntime.Pump(0.016f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SimulateMainThreadWork(int iterations)
    {
        int acc = EcsBenchmarkSink.IntValue;
        for (int i = 0; i < iterations; i++)
        {
            acc = unchecked((acc * 16777619) ^ (i + 31));
        }

        EcsBenchmarkSink.IntValue = acc;
    }
}

[MemoryDiagnoser]
[CategoriesColumn]
[RankColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EcsFrameBatchBenchmarks
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    private LayerRuntime _asyncRuntime = null!;
    private AsyncEcsScheduler _asyncScheduler = null!;
    private NoopEcsWorkItem _noopWorkItem = null!;
    private long _lastFence;

    [Params(1_000)]
    public int EntityCount { get; set; }

    [Params(1, 10, 100, 1_000)]
    public int QueryCount { get; set; }

    [Params(0, 256)]
    public int MainThreadWorkIterations { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        _asyncRuntime = EcsBenchmarkWorldFactory.CreateRuntime(EcsExecutionMode.Async);
        _asyncScheduler = (AsyncEcsScheduler)_asyncRuntime.EcsWorkScheduler;
        _noopWorkItem = new NoopEcsWorkItem();
        EcsBenchmarkWorldFactory.PopulatePlainQueryWorld(_asyncRuntime, EntityCount);
        EnsureFrameSubmitCapacity();
        WarmWorker();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        LayerHub.Reset();
    }

    [IterationSetup(Target = nameof(Async_FrameBatch_SubmitFlushOnly))]
    public void PrepareSubmitFlushOnly()
    {
        EnsureFrameSubmitCapacity();
    }

    [Benchmark(Description = "Async FrameBatch SubmitManyQueries FlushOnce")]
    [BenchmarkCategory("09.ECS.FrameBatch", "Async", "SubmitFlushOnly")]
    public void Async_FrameBatch_SubmitFlushOnly()
    {
        SubmitFrameQueries();
        SimulateMainThreadWork(MainThreadWorkIterations);
        _lastFence = _asyncRuntime.FlushEcsSubmissionsForTest();
    }

    [IterationCleanup(Target = nameof(Async_FrameBatch_SubmitFlushOnly))]
    public void CleanupSubmitFlushOnly()
    {
        _asyncRuntime.WaitEcsFenceForTest(_lastFence, IdleTimeout);
    }

    [IterationSetup(Target = nameof(Async_FrameBatch_WarmWorkerEndToEnd))]
    public void PrepareWarmWorkerEndToEnd()
    {
        EnsureFrameSubmitCapacity();
        WarmWorker();
    }

    [Benchmark(Description = "Async FrameBatch WarmWorker EndToEnd")]
    [BenchmarkCategory("09.ECS.FrameBatch", "Async", "WarmWorker", "EndToEnd")]
    public void Async_FrameBatch_WarmWorkerEndToEnd()
    {
        SubmitFrameQueries();
        SimulateMainThreadWork(MainThreadWorkIterations);
        long fence = _asyncRuntime.FlushEcsSubmissionsForTest();
        _asyncRuntime.WaitEcsFenceForTest(fence, IdleTimeout);
        _asyncRuntime.Pump(0.016f);
    }

    private void SubmitFrameQueries()
    {
        for (int i = 0; i < QueryCount; i++)
        {
            var job = new MoveWithWorkJob(0);
            _asyncRuntime.EcsWorld
                         .Query<BenchPosition, BenchVelocity>()
                         .ForEach(ref job);
        }
    }

    private void EnsureFrameSubmitCapacity()
    {
        _asyncScheduler.EnsureCurrentSubmissionCapacityForTest(
            QueryCount,
            QueryCount * Unsafe.SizeOf<MoveWithWorkJob>());
    }

    private void WarmWorker()
    {
        _asyncScheduler.Schedule(_noopWorkItem);
        long fence = _asyncScheduler.FlushSubmissionsForTest();
        _asyncRuntime.WaitEcsFenceForTest(fence, IdleTimeout);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SimulateMainThreadWork(int iterations)
    {
        int acc = EcsBenchmarkSink.IntValue;
        for (int i = 0; i < iterations; i++)
        {
            acc = unchecked((acc * 16777619) ^ (i + 31));
        }

        EcsBenchmarkSink.IntValue = acc;
    }

    private sealed class NoopEcsWorkItem : IEcsWorkItem
    {
        public string DebugName => "Noop";

        public void Execute(World world, EcsResultQueue results)
        {
        }
    }
}

public struct BenchPosition : IComponent
{
    public float X;
    public float Y;
}

public struct BenchVelocity : IComponent
{
    public float X;
    public float Y;
}

public struct BenchAoi : IComponent
{
    public bool IsVisible;
}

public struct BenchMoveViewEvent : IActorEvent
{
    public float X;
    public float Y;

    public BenchMoveViewEvent(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public sealed partial class BenchProjectedActor : IPooledActor
{
    public static int MoveCount;

    [ActorBehaviour]
    private void OnMove(in BenchMoveViewEvent value)
    {
        MoveCount++;
        EcsBenchmarkSink.FloatValue = value.X + value.Y;
    }

    public void OnRent()
    {
    }

    public void OnReturn()
    {
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
}

public readonly struct MoveWithWorkJob : IQueryJob<BenchPosition, BenchVelocity>
{
    private readonly int _workIterations;

    public MoveWithWorkJob(int workIterations)
    {
        _workIterations = workIterations;
    }

    public void Execute(Entity entity, ref BenchPosition position, ref BenchVelocity velocity)
    {
        float x = position.X + velocity.X;
        float y = position.Y + velocity.Y;

        for (int i = 0; i < _workIterations; i++)
        {
            x = (x * 1.0001f) + velocity.X;
            y = (y * 0.9999f) + velocity.Y;
        }

        position.X = x;
        position.Y = y;
    }
}

public sealed partial class GeneratedPlainQueryBenchService
{
    [Query]
    [EntryPoint(nameof(GeneratedPlainMove))]
    private static void OnGeneratedPlainMove(
        int workIterations,
        ref BenchPosition position,
        in BenchVelocity velocity)
    {
        float x = position.X + velocity.X;
        float y = position.Y + velocity.Y;

        for (int i = 0; i < workIterations; i++)
        {
            x = (x * 1.0001f) + velocity.X;
            y = (y * 0.9999f) + velocity.Y;
        }

        position.X = x;
        position.Y = y;
    }
}

public readonly struct MoveViewJob :
    IProjectionJob3x1<BenchPosition, BenchVelocity, BenchAoi, BenchMoveViewEvent>
{
    private readonly int _workIterations;

    public MoveViewJob(int workIterations)
    {
        _workIterations = workIterations;
    }

    public ProjectResult Execute(
        Entity entity,
        ref BenchPosition position,
        ref BenchVelocity velocity,
        ref BenchAoi aoi,
        ref BenchMoveViewEvent moveEvent)
    {
        if (!aoi.IsVisible)
        {
            return ProjectResult.Fail;
        }

        float x = position.X + velocity.X;
        float y = position.Y + velocity.Y;

        for (int i = 0; i < _workIterations; i++)
        {
            x = (x * 1.0001f) + velocity.X;
            y = (y * 0.9999f) + velocity.Y;
        }

        position.X = x;
        position.Y = y;
        moveEvent = new BenchMoveViewEvent(x, y);
        return ProjectResult.Success;
    }
}

internal static class EcsBenchmarkWorldFactory
{
    public static LayerRuntime CreateRuntime(
        EcsExecutionMode mode,
        EcsWorkerIdlePolicy idlePolicy = EcsWorkerIdlePolicy.LowLatency)
    {
        return LayerHub.CreateLayers()
                       .Push(new EcsBenchmarkLayer())
                       .SetEcsOptions(new EcsRuntimeOptions(
                           mode,
                           maxResultsDrainPerPump: int.MaxValue,
                           workerIdlePolicy: idlePolicy))
                       .Build();
    }

    public static void PopulatePlainQueryWorld(LayerRuntime runtime, int entityCount)
    {
        for (int i = 0; i < entityCount; i++)
        {
            runtime.EcsWorld.Create(
                new BenchPosition { X = i, Y = i * 0.5f },
                new BenchVelocity { X = 1f, Y = 0.25f });
        }
    }

    public static void PopulateBringWorld(LayerRuntime runtime, int entityCount)
    {
        for (int i = 0; i < entityCount; i++)
        {
            Entity entity = runtime.EcsWorld.Create(
                new BenchPosition { X = i, Y = i * 0.5f },
                new BenchVelocity { X = 1f, Y = 0.25f },
                new BenchAoi { IsVisible = true });

            runtime.EcsWorld.WithProjectedActor<BenchProjectedActor>(
                entity,
                keepAliveSeconds: 60f,
                releasePolicy: ProjectedActorReleasePolicy.ReturnToPool);
        }
    }
}

internal sealed class EcsBenchmarkLayer : Layer
{
}

internal static class EcsBenchmarkSink
{
    public static int IntValue;
    public static float FloatValue;
}
