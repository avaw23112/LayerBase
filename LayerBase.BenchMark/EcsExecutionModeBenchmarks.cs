using System.Runtime.CompilerServices;
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
    private EcsSubmissionBatch _batch = null!;
    private EcsSubmissionBatch _recordBatch = null!;
    private NoopEcsWorkItem _workItem = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ring = new SpscRing<EcsSubmissionBatch>(1024);
        _batch = new EcsSubmissionBatch(1);
        _recordBatch = new EcsSubmissionBatch(1024);
        _workItem = new NoopEcsWorkItem();
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
public class EcsExecutionModeBenchmarks
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    private LayerRuntime _syncRuntime = null!;
    private LayerRuntime _asyncRuntime = null!;

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
        EcsBenchmarkWorldFactory.PopulatePlainQueryWorld(_syncRuntime, EntityCount);
        EcsBenchmarkWorldFactory.PopulatePlainQueryWorld(_asyncRuntime, EntityCount);
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

    [Benchmark(Description = "Async PlainQuery SubmitOnly")]
    [InvocationCount(1)]
    [BenchmarkCategory("07.ECS.ExecutionMode", "PlainQuery", "AsyncSubmit")]
    public void Async_PlainQuery_SubmitOnly()
    {
        var job = new MoveWithWorkJob(WorkIterations);
        _asyncRuntime.EcsWorld
                     .Query<BenchPosition, BenchVelocity>()
                     .ForEach(ref job);
    }

    [IterationCleanup(Target = nameof(Async_PlainQuery_SubmitOnly))]
    public void CleanupAsyncPlainSubmitOnly()
    {
        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
    }

    [Benchmark(Description = "Async PlainQuery EndToEnd")]
    [BenchmarkCategory("07.ECS.ExecutionMode", "PlainQuery", "AsyncEndToEnd")]
    public void Async_PlainQuery_EndToEnd()
    {
        var job = new MoveWithWorkJob(WorkIterations);
        _asyncRuntime.EcsWorld
                     .Query<BenchPosition, BenchVelocity>()
                     .ForEach(ref job);

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
        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
        _asyncRuntime.Pump(0.016f);
    }

    [Benchmark(Description = "Async BringQuery EndToEnd")]
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

        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
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

        _asyncRuntime.WaitEcsIdleForTest(IdleTimeout);
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
    public static LayerRuntime CreateRuntime(EcsExecutionMode mode)
    {
        return LayerHub.CreateLayers()
                       .Push(new EcsBenchmarkLayer())
                       .SetEcsOptions(new EcsRuntimeOptions(mode, maxResultsDrainPerPump: int.MaxValue))
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
