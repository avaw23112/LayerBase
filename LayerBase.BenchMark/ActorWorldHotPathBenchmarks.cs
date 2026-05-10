using BenchmarkDotNet.Attributes;
using LayerBase.Actor;

namespace Benchmarks;

[MemoryDiagnoser]
public sealed class ActorWorldHotPathBenchmarks : EventBenchmarkBase
{
    private const int ActorCount = 1000;

    private ActorWorld _prewarmWorld = null!;
    private PrewarmHotBenchmarkActor _prewarmActor = null!;
    private ActorId _prewarmActorId;

    private ActorWorld _hotWorld = null!;
    private HotBenchmarkActor _hotActor = null!;
    private ActorId _hotActorId;

    private ActorWorld _prewarmBatchWorld = null!;
    private ActorId[] _prewarmActorIds = null!;

    private ActorWorld _queryWorld = null!;
    private ActorQueryResult _query = default;

    [GlobalSetup]
    public void Setup()
    {
        _prewarmWorld = CreateBenchmarkWorld(OneMillion);
        _prewarmActor = _prewarmWorld.CreateActor<PrewarmHotBenchmarkActor>();
        _prewarmActorId = _prewarmActor.GetActorId();

        _hotWorld = CreateBenchmarkWorld(OneMillion);
        _hotActor = _hotWorld.CreateActor<HotBenchmarkActor>();
        _hotActorId = _hotActor.GetActorId();
        _ = _hotWorld.PostTo(_hotActorId, ActorBenchEvent.Instance);
        PumpAll(_hotWorld);

        _prewarmBatchWorld = CreateBenchmarkWorld(OneMillion);
        _prewarmActorIds = new ActorId[ActorCount];
        for (int i = 0; i < ActorCount; i++)
        {
            _prewarmActorIds[i] = _prewarmBatchWorld.CreateActor<PrewarmHotBenchmarkActor>().GetActorId();
        }

        _queryWorld = CreateBenchmarkWorld(32);
        for (int i = 0; i < ActorCount; i++)
        {
            _queryWorld.CreateActor<QueryFastBenchmarkActor>();
        }

        _query = _queryWorld.QueryActor<
            BenchEvent1,
            BenchEvent2,
            BenchEvent3,
            BenchEvent4,
            BenchEvent5,
            BenchEvent6,
            BenchEvent7,
            BenchEvent8,
            BenchEvent9,
            BenchEvent10,
            BenchEvent11,
            BenchEvent12>();
    }

    [IterationCleanup(Target = nameof(ActorPost_PrewarmHot_PostFast_OneActor_OneEvent))]
    public void CleanupPrewarmFastSingle() => PumpAll(_prewarmWorld);

    [IterationCleanup(Target = nameof(ActorPost_Hot_PostFast_OneActor_OneEvent))]
    public void CleanupHotFastSingle() => PumpAll(_hotWorld);

    [IterationCleanup(Target = nameof(ActorPost_PrewarmHot_PostTo_OneActor_OneEvent))]
    public void CleanupPrewarmPostToSingle() => PumpAll(_prewarmWorld);

    [IterationCleanup(Target = nameof(ActorPost_PrewarmHot_PostFast_1000Actors_OneEvent))]
    public void CleanupPrewarmBatch() => PumpAll(_prewarmBatchWorld);

    [IterationCleanup(Target = nameof(ActorPost_Query_PostAllFast_1000Actors_12Events))]
    public void CleanupQueryFast() => PumpAll(_queryWorld);

    [Benchmark(Description = "ActorPost_PrewarmHot_PostFast_OneActor_OneEvent")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    public void ActorPost_PrewarmHot_PostFast_OneActor_OneEvent()
    {
        for (int i = 0; i < OneMillion; i++)
        {
            _ = _prewarmWorld.PostFast(_prewarmActorId, ActorBenchEvent.Instance);
        }
    }

    [Benchmark(Description = "ActorPost_Hot_PostFast_OneActor_OneEvent")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    public void ActorPost_Hot_PostFast_OneActor_OneEvent()
    {
        for (int i = 0; i < OneMillion; i++)
        {
            _ = _hotWorld.PostFast(_hotActorId, ActorBenchEvent.Instance);
        }
    }

    [Benchmark(Description = "ActorPost_PrewarmHot_PostTo_OneActor_OneEvent")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    public void ActorPost_PrewarmHot_PostTo_OneActor_OneEvent()
    {
        for (int i = 0; i < OneMillion; i++)
        {
            _ = _prewarmWorld.PostTo(_prewarmActorId, ActorBenchEvent.Instance);
        }
    }

    [Benchmark(Description = "ActorPost_PrewarmHot_PostFast_1000Actors_OneEvent")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    public void ActorPost_PrewarmHot_PostFast_1000Actors_OneEvent()
    {
        for (int i = 0; i < OneMillion; i++)
        {
            _ = _prewarmBatchWorld.PostFast(_prewarmActorIds[i % ActorCount], ActorBenchEvent.Instance);
        }
    }

    [Benchmark(Description = "ActorPost_Query_PostAllFast_1000Actors_12Events")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    public void ActorPost_Query_PostAllFast_1000Actors_12Events()
    {
        _query.PostAll(
            BenchEvent1.Instance,
            BenchEvent2.Instance,
            BenchEvent3.Instance,
            BenchEvent4.Instance,
            BenchEvent5.Instance,
            BenchEvent6.Instance,
            BenchEvent7.Instance,
            BenchEvent8.Instance,
            BenchEvent9.Instance,
            BenchEvent10.Instance,
            BenchEvent11.Instance,
            BenchEvent12.Instance);
    }

    private static ActorWorld CreateBenchmarkWorld(int maxCapacity)
    {
        return new ActorWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: maxCapacity,
            maxCapacity: maxCapacity,
            growFactor: 2,
            releaseWhenEmpty: false));
    }

    private static void PumpAll(ActorWorld world)
    {
        var budget = new RuntimeFrameBudget(maxEvents: 0, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);
    }

    public readonly struct BenchEvent1 { public static readonly BenchEvent1 Instance = default; }
    public readonly struct BenchEvent2 { public static readonly BenchEvent2 Instance = default; }
    public readonly struct BenchEvent3 { public static readonly BenchEvent3 Instance = default; }
    public readonly struct BenchEvent4 { public static readonly BenchEvent4 Instance = default; }
    public readonly struct BenchEvent5 { public static readonly BenchEvent5 Instance = default; }
    public readonly struct BenchEvent6 { public static readonly BenchEvent6 Instance = default; }
    public readonly struct BenchEvent7 { public static readonly BenchEvent7 Instance = default; }
    public readonly struct BenchEvent8 { public static readonly BenchEvent8 Instance = default; }
    public readonly struct BenchEvent9 { public static readonly BenchEvent9 Instance = default; }
    public readonly struct BenchEvent10 { public static readonly BenchEvent10 Instance = default; }
    public readonly struct BenchEvent11 { public static readonly BenchEvent11 Instance = default; }
    public readonly struct BenchEvent12 { public static readonly BenchEvent12 Instance = default; }
}

public sealed partial class PrewarmHotBenchmarkActor : IActor
{
    [ActorBehaviour(BehaviourType.PrewarmHot)]
    private void OnActorBench(in ActorBenchEvent value)
    {
    }
}

public sealed partial class HotBenchmarkActor : IActor
{
    [ActorBehaviour(BehaviourType.Hot)]
    private void OnActorBench(in ActorBenchEvent value)
    {
    }
}

public sealed partial class QueryFastBenchmarkActor : IActor
{
    [ActorBehaviour] private void On1(in ActorWorldHotPathBenchmarks.BenchEvent1 value) { }
    [ActorBehaviour] private void On2(in ActorWorldHotPathBenchmarks.BenchEvent2 value) { }
    [ActorBehaviour] private void On3(in ActorWorldHotPathBenchmarks.BenchEvent3 value) { }
    [ActorBehaviour] private void On4(in ActorWorldHotPathBenchmarks.BenchEvent4 value) { }
    [ActorBehaviour] private void On5(in ActorWorldHotPathBenchmarks.BenchEvent5 value) { }
    [ActorBehaviour] private void On6(in ActorWorldHotPathBenchmarks.BenchEvent6 value) { }
    [ActorBehaviour] private void On7(in ActorWorldHotPathBenchmarks.BenchEvent7 value) { }
    [ActorBehaviour] private void On8(in ActorWorldHotPathBenchmarks.BenchEvent8 value) { }
    [ActorBehaviour] private void On9(in ActorWorldHotPathBenchmarks.BenchEvent9 value) { }
    [ActorBehaviour] private void On10(in ActorWorldHotPathBenchmarks.BenchEvent10 value) { }
    [ActorBehaviour] private void On11(in ActorWorldHotPathBenchmarks.BenchEvent11 value) { }
    [ActorBehaviour] private void On12(in ActorWorldHotPathBenchmarks.BenchEvent12 value) { }
}
