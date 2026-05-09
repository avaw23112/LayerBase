using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using LayerBase.Actor;

namespace Benchmarks;

/// <summary>
/// Actor Post 热路径基准测试。
///
/// 测试目标：
/// 1. 分离 Cold / Hot / PrewarmHot 三种行为模式。
/// 2. 分离 Hot 首次绑定成本与 Hot 缓存命中成本。
/// 3. 分离单 Actor Post、1000 Actor Post、Query PostAll。
/// 4. 使用 BenchmarkAttribute.OperationsPerInvoke 折算单次 Post 成本。
/// 5. 使用 InvocationCount(1) 尽量避免同一 iteration 内重复调用 benchmark 方法导致邮箱被填满。
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[CategoriesColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 5, iterationCount: 10)]
public partial class ActorPostHotPathBenchmarks : EventBenchmarkBase
{
    /// <summary>
    /// 单 Actor benchmark 内部循环次数。
    ///
    /// 作用：
    /// 单次 Post 太短，直接测一次会被 BenchmarkDotNet 的计时开销污染。
    /// 因此单 Actor 测试内部连续 Post 1024 次，再通过 OperationsPerInvoke 折算单次成本。
    /// </summary>
    private const int SingleActorPostOps = 1024;

    /// <summary>
    /// 批量 Actor 数量。
    ///
    /// 作用：
    /// 用于测试 1000 个 Actor 各 Post 一次时的稳定吞吐。
    /// </summary>
    private const int ManyActorCount = 1000;

    /// <summary>
    /// 1000 个 Actor，每个 Actor Post 4 个事件。
    ///
    /// 作用：
    /// 告诉 BenchmarkDotNet 当前方法实际执行了 4000 次 Post。
    /// </summary>
    private const int ManyActorFourEventOps = ManyActorCount * 4;

    /// <summary>
    /// 1000 个 Actor，每个 Actor Post 12 个事件。
    ///
    /// 作用：
    /// 告诉 BenchmarkDotNet 当前方法实际执行了 12000 次 Post。
    /// </summary>
    private const int ManyActorTwelveEventOps = ManyActorCount * 12;

    private ActorWorld _coldWorld = null!;
    private ColdBenchActor _coldActor = null!;

    private ActorWorld _hotFirstBindWorld = null!;
    private HotBenchActor[] _hotFirstBindActors = null!;

    private ActorWorld _hotCachedWorld = null!;
    private HotBenchActor _hotCachedActor = null!;

    private ActorWorld _prewarmWorld = null!;
    private PrewarmBenchActor _prewarmActor = null!;

    private ActorWorld _prewarmManyOneWorld = null!;
    private PrewarmBenchActor[] _prewarmManyOneActors = null!;

    private ActorWorld _prewarmManyFourWorld = null!;
    private PrewarmFourActor[] _prewarmManyFourActors = null!;

    private ActorWorld _queryOneWorld = null!;
    private ActorQueryResult _queryOne;

    private ActorWorld _queryTwelveWorld = null!;
    private ActorQueryResult _queryTwelve;

    private Dictionary<int, BenchDispatchReceiver> _dictionaryOne = null!;
    private Dictionary<int, BenchDispatchReceiver> _dictionaryMany = null!;
    private int[] _dictionaryManyKeys = null!;

    private Dictionary<int, Queue<PrewarmPostEvent>> _dictionaryMailOne = null!;
    private Dictionary<int, Queue<PrewarmPostEvent>> _dictionaryMailMany = null!;
    private Queue<PrewarmPostEvent>[] _dictionaryManyQueues = null!;

    /// <summary>
    /// 全局初始化。
    ///
    /// 作用：
    /// 准备所有可复用的 ActorWorld、Actor、Query、Dictionary baseline。
    /// 这些对象不在 benchmark 方法本体中创建，避免创建成本污染 Post 热路径测试。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _coldWorld = CreateWorld(
            initialCapacity: SingleActorPostOps,
            maxCapacity: SingleActorPostOps);

        _coldActor = _coldWorld.CreateActor<ColdBenchActor>();

        _hotCachedWorld = CreateWorld(
            initialCapacity: SingleActorPostOps,
            maxCapacity: SingleActorPostOps);

        _hotCachedActor = _hotCachedWorld.CreateActor<HotBenchActor>();

        // 先触发一次 Hot 行为，确保后续 Hot_Cached 测的是缓存命中路径。
        _hotCachedActor.PostInside(HotPostEvent.Instance);
        PumpAll(_hotCachedWorld);

        _prewarmWorld = CreateWorld(
            initialCapacity: SingleActorPostOps,
            maxCapacity: SingleActorPostOps);

        _prewarmActor = _prewarmWorld.CreateActor<PrewarmBenchActor>();

        _prewarmManyOneWorld = CreateWorld(
            initialCapacity: 16,
            maxCapacity: 16);

        _prewarmManyOneActors = CreateActors<PrewarmBenchActor>(
            _prewarmManyOneWorld,
            ManyActorCount);

        _prewarmManyFourWorld = CreateWorld(
            initialCapacity: 16,
            maxCapacity: 16);

        _prewarmManyFourActors = CreateActors<PrewarmFourActor>(
            _prewarmManyFourWorld,
            ManyActorCount);

        _queryOneWorld = CreateWorld(
            initialCapacity: 16,
            maxCapacity: 16);

        CreateActors<PrewarmBenchActor>(
            _queryOneWorld,
            ManyActorCount);

        _queryOne = _queryOneWorld.QueryActor<PrewarmPostEvent>();

        _queryTwelveWorld = CreateWorld(
            initialCapacity: 16,
            maxCapacity: 16);

        CreateActors<PrewarmTwelveActor>(
            _queryTwelveWorld,
            ManyActorCount);

        _queryTwelve = _queryTwelveWorld.QueryActor<
            QueryEvent1, QueryEvent2, QueryEvent3, QueryEvent4, QueryEvent5, QueryEvent6,
            QueryEvent7, QueryEvent8, QueryEvent9, QueryEvent10, QueryEvent11, QueryEvent12>();

        _dictionaryOne = new Dictionary<int, BenchDispatchReceiver>
        {
            [0] = new BenchDispatchReceiver()
        };

        _dictionaryMany = new Dictionary<int, BenchDispatchReceiver>(ManyActorCount);
        _dictionaryManyKeys = new int[ManyActorCount];

        for (int i = 0; i < ManyActorCount; i++)
        {
            _dictionaryManyKeys[i] = i;
            _dictionaryMany[i] = new BenchDispatchReceiver();
        }

        _dictionaryMailOne = new Dictionary<int, Queue<PrewarmPostEvent>>
        {
            [0] = new Queue<PrewarmPostEvent>(SingleActorPostOps)
        };

        _dictionaryMailMany = new Dictionary<int, Queue<PrewarmPostEvent>>(ManyActorCount);
        _dictionaryManyQueues = new Queue<PrewarmPostEvent>[ManyActorCount];

        for (int i = 0; i < ManyActorCount; i++)
        {
            Queue<PrewarmPostEvent> queue = new(capacity: 16);
            _dictionaryManyQueues[i] = queue;
            _dictionaryMailMany[i] = queue;
        }
    }

    /// <summary>
    /// 每次 Hot_FirstBind benchmark iteration 前创建一批尚未触发 Hot 缓存绑定的 Actor。
    ///
    /// 作用：
    /// benchmark 方法本体只测试“第一次 Post 导致 Hot 绑定”的成本，
    /// 不把 CreateWorld / CreateActor 成本算进被测方法。
    /// </summary>
    [IterationSetup(Target = nameof(ActorPost_Hot_FirstBind_OneActor_OneEvent))]
    public void SetupHotFirstBind()
    {
        _hotFirstBindWorld = CreateWorld(
            initialCapacity: 16,
            maxCapacity: 16);

        _hotFirstBindActors = CreateActors<HotBenchActor>(
            _hotFirstBindWorld,
            SingleActorPostOps);
    }

    [Benchmark(
        Description = "ActorPost_Cold_SafePath_OneActor_OneEvent",
        OperationsPerInvoke = SingleActorPostOps)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_Cold_SafePath_OneActor_OneEvent()
    {
        for (int i = 0; i < SingleActorPostOps; i++)
        {
            _coldActor.PostInside(ColdPostEvent.Instance);
        }
    }

    [IterationCleanup(Target = nameof(ActorPost_Cold_SafePath_OneActor_OneEvent))]
    public void CleanupCold()
    {
        PumpAll(_coldWorld);
    }

    [Benchmark(
        Description = "ActorPost_Hot_FirstBind_OneActor_OneEvent",
        OperationsPerInvoke = SingleActorPostOps)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_Hot_FirstBind_OneActor_OneEvent()
    {
        for (int i = 0; i < _hotFirstBindActors.Length; i++)
        {
            _hotFirstBindActors[i].PostInside(HotPostEvent.Instance);
        }
    }

    [IterationCleanup(Target = nameof(ActorPost_Hot_FirstBind_OneActor_OneEvent))]
    public void CleanupHotFirstBind()
    {
        PumpAll(_hotFirstBindWorld);
    }

    [Benchmark(
        Description = "ActorPost_Hot_Cached_OneActor_OneEvent",
        OperationsPerInvoke = SingleActorPostOps)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_Hot_Cached_OneActor_OneEvent()
    {
        for (int i = 0; i < SingleActorPostOps; i++)
        {
            _hotCachedActor.PostInside(HotPostEvent.Instance);
        }
    }

    [IterationCleanup(Target = nameof(ActorPost_Hot_Cached_OneActor_OneEvent))]
    public void CleanupHotCached()
    {
        PumpAll(_hotCachedWorld);
    }

    [Benchmark(
        Description = "ActorPost_PrewarmHot_Cached_OneActor_OneEvent",
        OperationsPerInvoke = SingleActorPostOps)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_PrewarmHot_Cached_OneActor_OneEvent()
    {
        for (int i = 0; i < SingleActorPostOps; i++)
        {
            _prewarmActor.PostInside(PrewarmPostEvent.Instance);
        }
    }

    [IterationCleanup(Target = nameof(ActorPost_PrewarmHot_Cached_OneActor_OneEvent))]
    public void CleanupPrewarm()
    {
        PumpAll(_prewarmWorld);
    }

    [Benchmark(
        Description = "ActorPost_PrewarmHot_1000Actors_OneEvent",
        OperationsPerInvoke = ManyActorCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_PrewarmHot_1000Actors_OneEvent()
    {
        for (int i = 0; i < _prewarmManyOneActors.Length; i++)
        {
            _prewarmManyOneActors[i].PostInside(PrewarmPostEvent.Instance);
        }
    }

    [IterationCleanup(Target = nameof(ActorPost_PrewarmHot_1000Actors_OneEvent))]
    public void CleanupPrewarmManyOne()
    {
        PumpAll(_prewarmManyOneWorld);
    }

    [Benchmark(
        Description = "ActorPost_PrewarmHot_1000Actors_4Events",
        OperationsPerInvoke = ManyActorFourEventOps)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_PrewarmHot_1000Actors_4Events()
    {
        for (int i = 0; i < _prewarmManyFourActors.Length; i++)
        {
            PrewarmFourActor actor = _prewarmManyFourActors[i];

            actor.PostInside(FourEvent1.Instance);
            actor.PostInside(FourEvent2.Instance);
            actor.PostInside(FourEvent3.Instance);
            actor.PostInside(FourEvent4.Instance);
        }
    }

    [IterationCleanup(Target = nameof(ActorPost_PrewarmHot_1000Actors_4Events))]
    public void CleanupPrewarmManyFour()
    {
        PumpAll(_prewarmManyFourWorld);
    }

    [Benchmark(
        Description = "ActorPost_Query_PostAll_1000Actors_OneEvent",
        OperationsPerInvoke = ManyActorCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_Query_PostAll_1000Actors_OneEvent()
    {
        _queryOne.PostAll(PrewarmPostEvent.Instance);
    }

    [IterationCleanup(Target = nameof(ActorPost_Query_PostAll_1000Actors_OneEvent))]
    public void CleanupQueryOne()
    {
        PumpAll(_queryOneWorld);
    }

    [Benchmark(
        Description = "ActorPost_Query_PostAll_1000Actors_12Events",
        OperationsPerInvoke = ManyActorTwelveEventOps)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_Query_PostAll_1000Actors_12Events()
    {
        _queryTwelve.PostAll(
            QueryEvent1.Instance,
            QueryEvent2.Instance,
            QueryEvent3.Instance,
            QueryEvent4.Instance,
            QueryEvent5.Instance,
            QueryEvent6.Instance,
            QueryEvent7.Instance,
            QueryEvent8.Instance,
            QueryEvent9.Instance,
            QueryEvent10.Instance,
            QueryEvent11.Instance,
            QueryEvent12.Instance);
    }

    [IterationCleanup(Target = nameof(ActorPost_Query_PostAll_1000Actors_12Events))]
    public void CleanupQueryTwelve()
    {
        PumpAll(_queryTwelveWorld);
    }

    [Benchmark(
        Description = "ActorPost_DictionaryBaseline_OneActor_LookupAndHandle",
        OperationsPerInvoke = SingleActorPostOps)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_DictionaryBaseline_OneActor_LookupAndHandle()
    {
        for (int i = 0; i < SingleActorPostOps; i++)
        {
            if (_dictionaryOne.TryGetValue(0, out BenchDispatchReceiver? receiver))
            {
                receiver.Handle();
            }
        }
    }

    [Benchmark(
        Description = "ActorPost_DictionaryBaseline_OneActor_LookupAndEnqueue",
        OperationsPerInvoke = SingleActorPostOps)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_DictionaryBaseline_OneActor_LookupAndEnqueue()
    {
        for (int i = 0; i < SingleActorPostOps; i++)
        {
            if (_dictionaryMailOne.TryGetValue(0, out Queue<PrewarmPostEvent>? queue))
            {
                queue.Enqueue(PrewarmPostEvent.Instance);
            }
        }
    }

    [IterationCleanup(Target = nameof(ActorPost_DictionaryBaseline_OneActor_LookupAndEnqueue))]
    public void CleanupDictionaryOneMail()
    {
        _dictionaryMailOne[0].Clear();
    }

    [Benchmark(
        Description = "ActorPost_DictionaryBaseline_1000Actors_LookupAndHandle",
        OperationsPerInvoke = ManyActorCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_DictionaryBaseline_1000Actors_LookupAndHandle()
    {
        for (int i = 0; i < _dictionaryManyKeys.Length; i++)
        {
            if (_dictionaryMany.TryGetValue(_dictionaryManyKeys[i], out BenchDispatchReceiver? receiver))
            {
                receiver.Handle();
            }
        }
    }

    [Benchmark(
        Description = "ActorPost_DictionaryBaseline_1000Actors_LookupAndEnqueue",
        OperationsPerInvoke = ManyActorCount)]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
    [InvocationCount(1)]
    public void ActorPost_DictionaryBaseline_1000Actors_LookupAndEnqueue()
    {
        for (int i = 0; i < _dictionaryManyKeys.Length; i++)
        {
            if (_dictionaryMailMany.TryGetValue(_dictionaryManyKeys[i], out Queue<PrewarmPostEvent>? queue))
            {
                queue.Enqueue(PrewarmPostEvent.Instance);
            }
        }
    }

    [IterationCleanup(Target = nameof(ActorPost_DictionaryBaseline_1000Actors_LookupAndEnqueue))]
    public void CleanupDictionaryManyMail()
    {
        for (int i = 0; i < _dictionaryManyQueues.Length; i++)
        {
            _dictionaryManyQueues[i].Clear();
        }
    }

    /// <summary>
    /// 创建 ActorWorld。
    /// </summary>
    /// <param name="initialCapacity">
    /// 每个 Actor 邮箱的初始容量。
    /// 单 Actor 循环测试必须给足容量，否则 benchmark 会测到 Grow 或 Full 分支。
    /// </param>
    /// <param name="maxCapacity">
    /// 每个 Actor 邮箱的最大容量。
    /// 单 Actor cached benchmark 建议等于 SingleActorPostOps。
    /// </param>
    private static ActorWorld CreateWorld(int initialCapacity, int maxCapacity)
    {
        return new ActorWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: initialCapacity,
            maxCapacity: maxCapacity,
            growFactor: 2,
            releaseWhenEmpty: false));
    }

    /// <summary>
    /// 批量创建 Actor。
    /// </summary>
    /// <typeparam name="TActor">
    /// 要创建的 Actor 类型。
    /// 必须实现 IActor，并且拥有无参构造函数。
    /// </typeparam>
    /// <param name="world">
    /// Actor 所属的 ActorWorld。
    /// </param>
    /// <param name="count">
    /// 要创建的 Actor 数量。
    /// </param>
    private static TActor[] CreateActors<TActor>(ActorWorld world, int count)
        where TActor : class, IActor, new()
    {
        var actors = new TActor[count];

        for (int i = 0; i < count; i++)
        {
            actors[i] = world.CreateActor<TActor>();
        }

        return actors;
    }

    /// <summary>
    /// Pump 当前世界中所有待处理消息。
    /// </summary>
    /// <param name="world">
    /// 要 Pump 的 ActorWorld。
    /// </param>
    private static void PumpAll(ActorWorld world)
    {
        var budget = new RuntimeFrameBudget(
            maxEvents: 0,
            usedEvents: 0,
            deadlineTicks: 0);

        world.Pump(
            deltaTime: 0f,
            fixedDeltaTime: 0f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    private sealed class BenchDispatchReceiver
    {
        public void Handle()
        {
            BenchmarkSink.IntValue++;
        }
    }

    public readonly struct ColdPostEvent
    {
        public static readonly ColdPostEvent Instance = default;
    }

    public readonly struct HotPostEvent
    {
        public static readonly HotPostEvent Instance = default;
    }

    public readonly struct PrewarmPostEvent
    {
        public static readonly PrewarmPostEvent Instance = default;
    }

    public readonly struct FourEvent1
    {
        public static readonly FourEvent1 Instance = default;
    }

    public readonly struct FourEvent2
    {
        public static readonly FourEvent2 Instance = default;
    }

    public readonly struct FourEvent3
    {
        public static readonly FourEvent3 Instance = default;
    }

    public readonly struct FourEvent4
    {
        public static readonly FourEvent4 Instance = default;
    }

    public readonly struct QueryEvent1 { public static readonly QueryEvent1 Instance = default; }
    public readonly struct QueryEvent2 { public static readonly QueryEvent2 Instance = default; }
    public readonly struct QueryEvent3 { public static readonly QueryEvent3 Instance = default; }
    public readonly struct QueryEvent4 { public static readonly QueryEvent4 Instance = default; }
    public readonly struct QueryEvent5 { public static readonly QueryEvent5 Instance = default; }
    public readonly struct QueryEvent6 { public static readonly QueryEvent6 Instance = default; }
    public readonly struct QueryEvent7 { public static readonly QueryEvent7 Instance = default; }
    public readonly struct QueryEvent8 { public static readonly QueryEvent8 Instance = default; }
    public readonly struct QueryEvent9 { public static readonly QueryEvent9 Instance = default; }
    public readonly struct QueryEvent10 { public static readonly QueryEvent10 Instance = default; }
    public readonly struct QueryEvent11 { public static readonly QueryEvent11 Instance = default; }
    public readonly struct QueryEvent12 { public static readonly QueryEvent12 Instance = default; }

    public sealed partial class ColdBenchActor : IActor
    {
        [ActorBehaviour]
        private void OnEvent(in ColdPostEvent value)
        {
        }
    }

    public sealed partial class HotBenchActor : IActor
    {
        [ActorBehaviour(BehaviourType.Hot)]
        private void OnEvent(in HotPostEvent value)
        {
        }
    }

    public sealed partial class PrewarmBenchActor : IActor
    {
        [ActorBehaviour(BehaviourType.PrewarmHot)]
        private void OnEvent(in PrewarmPostEvent value)
        {
        }
    }

    public sealed partial class PrewarmFourActor : IActor
    {
        [ActorBehaviour(BehaviourType.PrewarmHot)]
        private void OnEvent(in FourEvent1 value)
        {
        }

        [ActorBehaviour(BehaviourType.PrewarmHot)]
        private void OnEvent(in FourEvent2 value)
        {
        }

        [ActorBehaviour(BehaviourType.PrewarmHot)]
        private void OnEvent(in FourEvent3 value)
        {
        }

        [ActorBehaviour(BehaviourType.PrewarmHot)]
        private void OnEvent(in FourEvent4 value)
        {
        }
    }

    public sealed partial class PrewarmTwelveActor : IActor
    {
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent1 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent2 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent3 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent4 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent5 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent6 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent7 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent8 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent9 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent10 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent11 value) { }
        [ActorBehaviour(BehaviourType.PrewarmHot)] private void OnEvent(in QueryEvent12 value) { }
    }
}