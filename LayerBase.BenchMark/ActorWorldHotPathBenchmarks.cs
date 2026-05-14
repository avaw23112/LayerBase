using BenchmarkDotNet.Attributes;
using LayerBase.Actor;
using LayerBase.Event.EventMetaData;

namespace Benchmarks;

[MemoryDiagnoser]
public partial class ActorWorldArchetypeRowBenchmarks : EventBenchmarkBase
{
    private const int ActorCount = 1000;
    private const int PostLoopCount = 1_000_000;
    private const int QueryEventCount = ActorCount * 12;

    private ActorWorld _singleWorld = null!;
    private ActorId _singleActorId;

    private ActorWorld _prewarmWorld = null!;
    private ActorId _prewarmActorId;


    private ActorWorld _batchWorld = null!;
    private ActorId[] _batchActorIds = null!;

    private ActorWorld _queryWorld = null!;
    private ActorQueryResult _query;

    private Dictionary<int, DictionaryReceiver> _dictionary = null!;
    private int[] _dictionaryKeys = null!;

    [GlobalSetup]
    public void Setup()
    {
        _singleWorld = CreateBenchmarkWorld(PostLoopCount);
        _singleActorId = _singleWorld.CreateActor<ArchetypeRowBenchmarkActor>().GetActorId();

        _prewarmWorld = CreateBenchmarkWorld(PostLoopCount);
        _prewarmActorId = _prewarmWorld.CreateActor<HotPostBenchmarkActor>().GetActorId();

        _batchWorld = CreateBenchmarkWorld(PostLoopCount);
        _batchActorIds = new ActorId[ActorCount];
        for (int i = 0; i < ActorCount; i++)
        {
            _batchActorIds[i] = _batchWorld.CreateActor<ArchetypeRowBenchmarkActor>().GetActorId();
        }

        _queryWorld = CreateBenchmarkWorld(32);
        for (int i = 0; i < ActorCount; i++)
        {
            _queryWorld.CreateActor<QueryBenchmarkActor>();
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

       _prewarmWorld.PostTo(
            _prewarmActorId,
            HotPostBenchmarkEvent.Instance);
        PumpAll(_prewarmWorld);

        _dictionary = new Dictionary<int, DictionaryReceiver>(ActorCount);
        _dictionaryKeys = new int[ActorCount];
        for (int i = 0; i < ActorCount; i++)
        {
            _dictionaryKeys[i] = i;
            _dictionary[i] = new DictionaryReceiver();
        }
    }


    [IterationCleanup(Target = nameof(ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent))]
    public void CleanupSinglePostTo()
    {
        PumpAll(_singleWorld);
    }

    [IterationCleanup(Target = nameof(ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent_Prewarm))]
    public void CleanupPrewarm()
    {
        PumpAll(_prewarmWorld);
    }

    [IterationCleanup(Target = nameof(ActorPost_ArchetypeRow_1000Actors_OneEvent))]
    public void CleanupBatch()
    {
        PumpAll(_batchWorld);
    }

    [IterationCleanup(Target = nameof(ActorPost_Query_PostAll_1000Actors_12Events))]
    public void CleanupQuery()
    {
        PumpAll(_queryWorld);
    }


    [Benchmark(
        Description = "ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent",
        OperationsPerInvoke = PostLoopCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent()
    {
        for (int i = 0; i < PostLoopCount; i++)
        {
             _singleWorld.PostTo(_singleActorId, ActorBenchEvent.Instance);
        }
    }

    [Benchmark(
        Description = "ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent_Prewarm",
        OperationsPerInvoke = PostLoopCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_ArchetypeRow_PostTo_OneActor_OneEvent_Prewarm()
    {
        for (int i = 0; i < PostLoopCount; i++)
        {
            _prewarmWorld.PostTo(_prewarmActorId, HotPostBenchmarkEvent.Instance);
        }
    }

    [Benchmark(
        Description = "ActorPost_ArchetypeRow_1000Actors_OneEvent",
        OperationsPerInvoke = PostLoopCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_ArchetypeRow_1000Actors_OneEvent()
    {
        for (int i = 0; i < PostLoopCount; i++)
        {
            _batchWorld.PostTo(_batchActorIds[i % ActorCount], ActorBenchEvent.Instance);
        }
    }

    [Benchmark(
        Description = "ActorPost_Query_PostAll_1000Actors_12Events",
        OperationsPerInvoke = QueryEventCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_Query_PostAll_1000Actors_12Events()
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

    [Benchmark(
        Description = "Dictionary_1000Actors_LookupAndHandle",
        OperationsPerInvoke = PostLoopCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void Dictionary_1000Actors_LookupAndHandle()
    {
        for (int i = 0; i < PostLoopCount; i++)
        {
            int key = _dictionaryKeys[i % ActorCount];
            if (_dictionary.TryGetValue(key, out DictionaryReceiver? receiver))
            {
                receiver.Handle();
            }
        }
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
        var budget = new RuntimeFrameBudget(
            maxEvents: int.MaxValue,
            usedEvents: 0,
            deadlineTicks: long.MaxValue);

        world.Pump(
            deltaTime: 0f,
            fixedDeltaTime: 0f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    private sealed class DictionaryReceiver
    {
        public void Handle()
        {
            BenchmarkSink.IntValue++;
        }
    }

    public readonly partial struct HotPostBenchmarkEvent
    {
        public static readonly HotPostBenchmarkEvent Instance = default;
    }

    public sealed class HotPostBenchmarkEventMetaData
        : EventMetaData<HotPostBenchmarkEvent>
    {
        public override ActorMailOptions? ActorMailOptions => new(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: 1_048_576,
            maxCapacity: 1_048_576,
            growFactor: 2,
            releaseWhenEmpty: false,
            disabledPolicy: ActorMailDisabledPolicy.Accept,
            pendingDestroyPolicy: ActorMailPendingDestroyPolicy.Reject);
    }

    public readonly struct BenchEvent1
    {
        public static readonly BenchEvent1 Instance = default;
    }

    public readonly struct BenchEvent2
    {
        public static readonly BenchEvent2 Instance = default;
    }

    public readonly struct BenchEvent3
    {
        public static readonly BenchEvent3 Instance = default;
    }

    public readonly struct BenchEvent4
    {
        public static readonly BenchEvent4 Instance = default;
    }

    public readonly struct BenchEvent5
    {
        public static readonly BenchEvent5 Instance = default;
    }

    public readonly struct BenchEvent6
    {
        public static readonly BenchEvent6 Instance = default;
    }

    public readonly struct BenchEvent7
    {
        public static readonly BenchEvent7 Instance = default;
    }

    public readonly struct BenchEvent8
    {
        public static readonly BenchEvent8 Instance = default;
    }

    public readonly struct BenchEvent9
    {
        public static readonly BenchEvent9 Instance = default;
    }

    public readonly struct BenchEvent10
    {
        public static readonly BenchEvent10 Instance = default;
    }

    public readonly struct BenchEvent11
    {
        public static readonly BenchEvent11 Instance = default;
    }

    public readonly struct BenchEvent12
    {
        public static readonly BenchEvent12 Instance = default;
    }
}

public partial class ArchetypeRowBenchmarkActor : IActor
{
    [ActorBehaviour]
    private void OnActorBench(in ActorBenchEvent value)
    {
    }
}

public partial class HotPostBenchmarkActor : IActor
{
    [ActorBehaviour]
    private void OnHotPostBenchmark(
        in ActorWorldArchetypeRowBenchmarks.HotPostBenchmarkEvent value)
    {
        // value 参数作用：
        // HotPostBenchmarkEvent 的事件值。
        // 这里保持空处理，避免 handler 成本污染 Post benchmark。
    }
}

public partial class QueryBenchmarkActor : IActor
{
    [ActorBehaviour]
    private void On1(in ActorWorldArchetypeRowBenchmarks.BenchEvent1 value)
    {
    }

    [ActorBehaviour]
    private void On2(in ActorWorldArchetypeRowBenchmarks.BenchEvent2 value)
    {
    }

    [ActorBehaviour]
    private void On3(in ActorWorldArchetypeRowBenchmarks.BenchEvent3 value)
    {
    }

    [ActorBehaviour]
    private void On4(in ActorWorldArchetypeRowBenchmarks.BenchEvent4 value)
    {
    }

    [ActorBehaviour]
    private void On5(in ActorWorldArchetypeRowBenchmarks.BenchEvent5 value)
    {
    }

    [ActorBehaviour]
    private void On6(in ActorWorldArchetypeRowBenchmarks.BenchEvent6 value)
    {
    }

    [ActorBehaviour]
    private void On7(in ActorWorldArchetypeRowBenchmarks.BenchEvent7 value)
    {
    }

    [ActorBehaviour]
    private void On8(in ActorWorldArchetypeRowBenchmarks.BenchEvent8 value)
    {
    }

    [ActorBehaviour]
    private void On9(in ActorWorldArchetypeRowBenchmarks.BenchEvent9 value)
    {
    }

    [ActorBehaviour]
    private void On10(in ActorWorldArchetypeRowBenchmarks.BenchEvent10 value)
    {
    }

    [ActorBehaviour]
    private void On11(in ActorWorldArchetypeRowBenchmarks.BenchEvent11 value)
    {
    }

    [ActorBehaviour]
    private void On12(in ActorWorldArchetypeRowBenchmarks.BenchEvent12 value)
    {
    }
}