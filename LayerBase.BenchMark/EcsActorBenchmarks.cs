using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using LayerBase;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.ECS.Projection;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;

namespace Benchmarks;

/// <summary>
/// ECS ↔ Actor 协作性能与 GC 来源定位 benchmark。
///
/// 这份 benchmark 重点解决四类问题：
/// 1. Hybrid cached ActorId 不再缓存 ActorId.Invalid。
/// 2. Projected Actor 会在 Setup 阶段显式绑定为真实 Actor。
/// 3. 可选预热 Projected Actor 邮箱，区分“首次邮箱分配”和“稳定热路径分配”。
/// 4. Create + Destroy 拆成 Cold 创建和 Runtime 复用，避免把冷启动分配误判为帧内热路径 GC。
///
/// 新名词说明：
/// ECS：Entity Component System，实体组件系统，用连续组件数组处理大批量数据。
/// Actor：行为对象，用于承载事件、生命周期、业务行为。
/// Projection：投影关系，这里指 ECS Entity 与 ActorId 之间的绑定。
/// ActorId：Actor 的轻量句柄，用来定位 Actor，不等于 Actor 对象本身。
/// Mailbox：Actor 邮箱，用于暂存 PostTo 投递过来的事件。
/// Pump：从 Actor 邮箱取出事件并分发给 ActorBehaviour 的过程。
/// Drain：主动执行 Pump，把残留邮箱事件清空，避免污染下一轮 benchmark。
/// Cold Path：冷路径，通常指初始化、首次创建、首次分配等低频路径。
/// Hot Path：热路径，通常指每帧、每事件、每实体都会高频执行的路径。
/// </summary>
[MemoryDiagnoser]
[CategoriesColumn]
[RankColumn]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public partial class EcsActorBenchmarks : EventBenchmarkBase
{
    // ─────────────────────────────────────────────────────
    // 常量
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// 大批量测试规模。
    /// 作用：模拟 10000 个实体或 Actor 的大规模运行时压力。
    /// </summary>
    private const int LargeCount = 10_000;

    /// <summary>
    /// 小批量测试规模。
    /// 作用：模拟单帧内较常见的一批业务对象投递规模。
    /// </summary>
    private const int SmallCount = 1_000;

    /// <summary>
    /// 未处理事件测试数量。
    /// 作用：定位 unsupported event，也就是 Actor 没有对应 ActorBehaviour 时的分配来源。
    /// </summary>
    private const int UnsupportedEventCount = 100;

    /// <summary>
    /// Drain 的最大执行轮数。
    /// 作用：兼容 ready / next 这类分阶段邮箱队列，避免只 Pump 一次没清干净。
    /// </summary>
    private const int MaxDrainPasses = 8;

    /// <summary>
    /// Drain 时的事件预算。
    /// 作用：给清理阶段足够大的预算，防止残留事件污染下一轮测试。
    /// </summary>
    private const int DrainEventBudget = 200_000;

    // ─────────────────────────────────────────────────────
    // ECS 组件
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// 位置组件。
    /// X / Y 表示二维坐标。
    /// </summary>
    public struct Position
    {
        public float X;
        public float Y;
    }

    /// <summary>
    /// 速度组件。
    /// Dx / Dy 表示每次更新时的位置增量。
    /// </summary>
    public struct Velocity
    {
        public float Dx;
        public float Dy;
    }

    /// <summary>
    /// 生命值组件。
    /// Current 表示当前生命值。
    /// </summary>
    public struct Health
    {
        public float Current;
    }

    /// <summary>
    /// ECS 小批量查询标签。
    /// 作用：让 Query 精确命中 1000 个普通 ECS Entity。
    /// </summary>
    public struct EcsQuerySmallTag { }

    /// <summary>
    /// ECS 大批量查询标签。
    /// 作用：让 Query 精确命中 10000 个普通 ECS Entity。
    /// </summary>
    public struct EcsQueryLargeTag { }

    /// <summary>
    /// ECS 临时创建销毁标签。
    /// 作用：标记 benchmark 中临时创建的 Entity。
    /// </summary>
    public struct EcsCreateDestroyTag { }

    /// <summary>
    /// Hybrid 小批量标签。
    /// 作用：让 Query 精确命中 1000 个带 Projected Actor 的 Entity。
    /// </summary>
    public struct HybridSmallTag { }

    /// <summary>
    /// Hybrid 大批量标签。
    /// 作用：让 Query 精确命中 10000 个带 Projected Actor 的 Entity。
    /// </summary>
    public struct HybridLargeTag { }

    // ─────────────────────────────────────────────────────
    // Actor 事件
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// 移动事件。
    /// DeltaX / DeltaY 表示一次移动事件携带的位移。
    /// </summary>
    public partial struct MoveEvent
    {
        public float DeltaX;
        public float DeltaY;
    }
    /// <summary>
    /// MoveEvent 的 benchmark 专用元数据。
    ///
    /// 作用：
    /// 1. 专门为 benchmark 放大 EventStreamSegmentPool 的保留上限。
    /// 2. 验证 10000 规模下的 132KB GC 是否来自 Segment 重新分配。
    /// 3. 不影响正式业务代码，因为这个类型只写在 benchmark 项目里。
    /// </summary>
    public sealed class MoveEventBenchmarkMetaData : EventMetaData<MoveEvent>
    {
        /// <summary>
        /// MoveEvent 的 Actor 邮件配置。
        ///
        /// segmentCapacity：
        /// 每个 EventStreamSegment 能存多少封 MoveEvent 邮件。
        ///
        /// maxRetainedSegments：
        /// Segment 池最多保留多少个空闲 Segment。
        ///
        /// 这里使用：
        /// 1024 * 10 = 10240
        ///
        /// 作用：
        /// 覆盖 benchmark 的 LargeCount = 10000，避免每轮测试重新 new Segment。
        /// </summary>
        public override ActorMailOptions? ActorMailOptions =>
            LayerBase.Actor.ActorMailOptions.EventStream(
                segmentCapacity: 1024,
                maxRetainedSegments: 10);
    }
    /// <summary>
    /// 伤害事件。
    /// 当前 MinimalActor / PooledActor 不处理该事件。
    /// 作用：专门定位 unsupported event 路径是否产生 GC。
    /// </summary>
    public struct DamageEvent
    {
        public float Amount;
    }

    /// <summary>
    /// 同步事件。
    /// 作用：给 LifecycleActor 提供一个最小 ActorBehaviour。
    /// </summary>
    public struct SyncEvent { }

    // ─────────────────────────────────────────────────────
    // Actor 类型
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// 最小 Actor。
    /// 只处理 MoveEvent，用来测量 Actor 正常事件分发热路径。
    /// </summary>
    public sealed partial class MinimalActor : IActor
    {
        public int MoveCount;

        /// <summary>
        /// MoveEvent 处理函数。
        ///
        /// 参数说明：
        /// e：移动事件。
        ///    使用 in 只读引用传入，避免结构体事件复制。
        /// </summary>
        [ActorBehaviour]
        private void OnMove(in MoveEvent e)
        {
            MoveCount++;
        }
    }

    /// <summary>
    /// 池化 Actor。
    /// 必须实现 IPooledActor，才能被 WithProjectedActor<TActor>() 用作 Projected Actor。
    /// </summary>
    public sealed partial class PooledActor : IActor, IPooledActor
    {
        /// <summary>
        /// 回收截止时间戳。
        /// 作用：Projected Actor 的 keep-alive 系统会根据它判断是否可以回收。
        /// </summary>
        public long RecycleDeadlineTicks { get; set; }

        public int MoveCount;

        /// <summary>
        /// 从对象池租出时调用。
        /// 作用：清空上一次使用留下的业务状态。
        /// </summary>
        public void OnRent()
        {
            MoveCount = 0;
        }

        /// <summary>
        /// 归还对象池时调用。
        /// 当前不需要额外清理。
        /// </summary>
        public void OnReturn()
        {
        }

        /// <summary>
        /// MoveEvent 处理函数。
        ///
        /// 参数说明：
        /// e：移动事件，只读引用传入，避免复制。
        /// </summary>
        [ActorBehaviour]
        private void OnMove(in MoveEvent e)
        {
            MoveCount++;
        }
    }

    /// <summary>
    /// 生命周期 Actor。
    /// 用于测量 IStart / IUpdate / IDestroy 的调度成本。
    /// </summary>
    public sealed partial class LifecycleActor : IActor, IStart, IUpdate, IDestroy
    {
        public int StartCount;
        public int UpdateCount;
        public int DestroyCount;

        /// <summary>
        /// Actor 首次进入生命周期时调用。
        /// </summary>
        public void Start()
        {
            StartCount++;
        }

        /// <summary>
        /// 每帧 Update 调用。
        ///
        /// 参数说明：
        /// dt：deltaTime，表示当前帧与上一帧之间经过的秒数。
        /// </summary>
        public void Update(float dt)
        {
            UpdateCount++;
        }

        /// <summary>
        /// Actor 销毁前调用。
        /// </summary>
        public void Destroy()
        {
            DestroyCount++;
        }

        /// <summary>
        /// 空行为函数。
        /// 作用：让 LifecycleActor 同时具备 ActorBehaviour 元数据。
        ///
        /// 参数说明：
        /// e：同步事件，只读引用传入。
        /// </summary>
        [ActorBehaviour]
        private void OnSync(in SyncEvent e)
        {
        }
    }

    // ─────────────────────────────────────────────────────
    // Runtime 对象
    // ─────────────────────────────────────────────────────

    private LayerRuntime _runtime = null!;
    private ActorWorld _actorWorld = null!;
    private World _ecsWorld = null!;

    private ActorWorld _pureActorWorld = null!;
    private ActorWorld _pooledActorWorld = null!;
    private ActorWorld _lifecycleWorld = null!;
    private ActorWorld _runtimeCreateDestroyWorld = null!;

    private ActorId[] _pureActorIds = null!;
    private ActorId[] _pooledActorIds = null!;
    private ActorId[] _lifecycleActorIds = null!;
    private ActorId[] _runtimeCreateDestroyIds = null!;

    private Entity[] _hybridSmallEntities = null!;
    private Entity[] _hybridLargeEntities = null!;

    private ActorId[] _hybridSmallActorIds = null!;
    private ActorId[] _hybridLargeActorIds = null!;

    private Entity[] _createDestroyTempEntities = null!;

    private Position[] _baselinePositions = null!;
    private Velocity[] _baselineVelocities = null!;

    private QueryDescription _ecsQuerySmall;
    private QueryDescription _ecsQueryLarge;
    private QueryDescription _hybridQuerySmall;
    private QueryDescription _hybridQueryLarge;

    private MoveEvent _moveEvent;
    private DamageEvent _damageEvent;

    /// <summary>
    /// 防止 JIT 消除整数统计结果。
    /// JIT：Just-In-Time Compiler，即 .NET 的即时编译器。
    /// </summary>
    private int _intSink;

    /// <summary>
    /// 防止 JIT 消除浮点写入结果。
    /// </summary>
    private float _floatSink;

    // ─────────────────────────────────────────────────────
    // Setup / Cleanup
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// 全局初始化。
    /// BenchmarkDotNet 在正式计时前调用一次。
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        LayerHub.Reset();
        
        EventMetaDataRegistry.RegisterMetaData<MoveEvent>(
            new MoveEventBenchmarkMetaData());
        
        _moveEvent = new MoveEvent
        {
            DeltaX = 1,
            DeltaY = 0
        };

        _damageEvent = new DamageEvent
        {
            Amount = 10
        };

        // PostSchedulerOptions：Actor 事件调度器配置。
        //
        // readyCapacity：ready 队列初始容量。
        // nextCapacity：next 队列初始容量。
        // maxEventsPerPump：每次 Pump 最多处理多少事件；0 表示不由该配置限制。
        // maxMillisecondsPerPump：每次 Pump 最多执行多少毫秒；0 表示不由该配置限制。
        // maxWavesPerPump：每次 Pump 最多处理多少波事件。
        // timeCheckInterval：每处理多少个事件检查一次时间预算。
        // defaultBackpressure：默认背压策略；RejectNew 表示满载时拒绝新事件。
        var postOptions = new PostSchedulerOptions(
            readyCapacity: LargeCount * 4,
            nextCapacity: LargeCount * 4,
            maxEventsPerPump: 0,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure: BackpressurePolicy.RejectNew);

        var layer = new EcsActorBenchLayer();
        layer.RegisterService(new EcsActorBenchService());

        _runtime = LayerHub.CreateLayers()
            .Push(layer)
            .SetPostOptions(postOptions)
            .Build();

        _actorWorld = _runtime.Actors;
        _ecsWorld = _runtime.EcsWorld;

        BuildQueryDescriptions();
        BuildBaselineArrays();
        BuildPureActorWorld();
        BuildPooledActorWorld();
        BuildEcsWorld();
        BuildHybridWorld();
        BuildLifecycleWorld();
        BuildRuntimeCreateDestroyWorld();

        _createDestroyTempEntities = new Entity[SmallCount];

        // 重点：
        // WithProjectedActor<TActor>() 只标记 Entity 可投影。
        // 这里显式把 ProjectedActorMeta 里的 ActorTypeId 转成真实 Actor，并写回 ActorId。
        ForceBindAllProjectedActors();

        // 预热正常 Actor 邮箱，避免首次邮箱数组分配混进稳定热路径。
        WarmupActorMailboxes(_pureActorWorld, _pureActorIds, SmallCount);
        WarmupActorMailboxes(_pooledActorWorld, _pooledActorIds, SmallCount);

        // 预热 projected actor 邮箱。
        // 如果不预热，Hybrid cached ActorId 第一次 PostTo 可能测到“每个 Actor 首次开邮箱”的分配。
        WarmupActorMailboxes(_actorWorld, _hybridSmallActorIds, SmallCount);
        WarmupActorMailboxes(_actorWorld, _hybridLargeActorIds, LargeCount);

        DrainAllActorWorlds();
    }

    /// <summary>
    /// 每次 benchmark iteration 前调用。
    /// 作用：清空上一次测试可能残留的邮箱事件。
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        DrainAllActorWorlds();
    }

    /// <summary>
    /// 每次 benchmark iteration 后调用。
    /// 作用：防止 Post-only 测试留下事件，污染后面的测试。
    /// </summary>
    [IterationCleanup]
    public void IterationCleanup()
    {
        DrainAllActorWorlds();
    }

    /// <summary>
    /// 构建 QueryDescription。
    ///
    /// QueryDescription：Arch ECS 的查询描述对象。
    /// WithAll：要求实体必须同时拥有这些组件。
    /// </summary>
    private void BuildQueryDescriptions()
    {
        _ecsQuerySmall = new QueryDescription()
            .WithAll<Position, Velocity, EcsQuerySmallTag>();

        _ecsQueryLarge = new QueryDescription()
            .WithAll<Position, Velocity, EcsQueryLargeTag>();

        _hybridQuerySmall = new QueryDescription()
            .WithAll<Position, Velocity, HybridSmallTag, ProjectedActorRef>();

        _hybridQueryLarge = new QueryDescription()
            .WithAll<Position, Velocity, HybridLargeTag, ProjectedActorRef>();
    }

    /// <summary>
    /// 构建直接数组写入基线。
    /// 作用：提供非框架路径的最低成本对照组。
    /// </summary>
    private void BuildBaselineArrays()
    {
        _baselinePositions = new Position[LargeCount];
        _baselineVelocities = new Velocity[LargeCount];

        for (int i = 0; i < LargeCount; i++)
        {
            _baselinePositions[i] = new Position
            {
                X = i,
                Y = i
            };

            _baselineVelocities[i] = new Velocity
            {
                Dx = 1,
                Dy = 1
            };
        }
    }

    /// <summary>
    /// 构建普通 ActorWorld。
    /// </summary>
    private void BuildPureActorWorld()
    {
        _pureActorWorld = new ActorWorld();
        _pureActorIds = new ActorId[LargeCount];

        for (int i = 0; i < LargeCount; i++)
        {
            var actor = _pureActorWorld.CreateActor<MinimalActor>();
            _pureActorIds[i] = actor.GetActorId();
        }
    }

    /// <summary>
    /// 构建池化 ActorWorld。
    /// </summary>
    private void BuildPooledActorWorld()
    {
        _pooledActorWorld = new ActorWorld();

        // PrewarmPool 参数说明：
        // TActor：要预热的 Actor 类型。
        // LargeCount：预热数量。
        //
        // 作用：
        // 提前把池化 Actor 实例创建好，避免稳定投递 benchmark 混入首次分配。
        _pooledActorWorld.PrewarmPool<PooledActor>(LargeCount);

        _pooledActorIds = new ActorId[LargeCount];

        for (int i = 0; i < LargeCount; i++)
        {
            var actor = _pooledActorWorld.CreateActor<PooledActor>(usePool: true);
            _pooledActorIds[i] = actor.GetActorId();
        }
    }

    /// <summary>
    /// 构建普通 ECS 实体。
    /// </summary>
    private void BuildEcsWorld()
    {
        for (int i = 0; i < SmallCount; i++)
        {
            _ecsWorld.Create(
                new Position
                {
                    X = i,
                    Y = i
                },
                new Velocity
                {
                    Dx = 1,
                    Dy = 1
                },
                new Health
                {
                    Current = 100
                },
                new EcsQuerySmallTag());
        }

        for (int i = 0; i < LargeCount; i++)
        {
            _ecsWorld.Create(
                new Position
                {
                    X = i,
                    Y = i
                },
                new Velocity
                {
                    Dx = 1,
                    Dy = 1
                },
                new Health
                {
                    Current = 100
                },
                new EcsQueryLargeTag());
        }
    }

    /// <summary>
    /// 构建带 Projected Actor 的 ECS 实体。
    ///
    /// 注意：
    /// WithProjectedActor<PooledActor>() 这里只标记投影关系。
    /// 真正 ActorId 绑定在 ForceBindAllProjectedActors() 里完成。
    /// </summary>
    private void BuildHybridWorld()
    {
        _hybridSmallEntities = new Entity[SmallCount];
        _hybridLargeEntities = new Entity[LargeCount];

        _hybridSmallActorIds = new ActorId[SmallCount];
        _hybridLargeActorIds = new ActorId[LargeCount];

        for (int i = 0; i < SmallCount; i++)
        {
            var entity = _ecsWorld.Create(
                new Position
                {
                    X = i,
                    Y = i
                },
                new Velocity
                {
                    Dx = 1,
                    Dy = 1
                },
                new HybridSmallTag(),
                new ProjectedActorRef());

            _hybridSmallEntities[i] = entity;

            // keepAliveSeconds 参数说明：
            // Projected Actor 在没有被使用后还能存活多少秒。
            //
            // releasePolicy 参数说明：
            // ReturnToPool 表示释放时归还到对象池。
            _ecsWorld.WithProjectedActor<PooledActor>(
                entity,
                keepAliveSeconds: 60f,
                releasePolicy: ProjectedActorReleasePolicy.ReturnToPool);
        }

        for (int i = 0; i < LargeCount; i++)
        {
            var entity = _ecsWorld.Create(
                new Position
                {
                    X = i,
                    Y = i
                },
                new Velocity
                {
                    Dx = 1,
                    Dy = 1
                },
                new HybridLargeTag(),
                new ProjectedActorRef());

            _hybridLargeEntities[i] = entity;

            _ecsWorld.WithProjectedActor<PooledActor>(
                entity,
                keepAliveSeconds: 60f,
                releasePolicy: ProjectedActorReleasePolicy.ReturnToPool);
        }
    }

    /// <summary>
    /// 构建生命周期 ActorWorld。
    /// </summary>
    private void BuildLifecycleWorld()
    {
        _lifecycleWorld = new ActorWorld();
        _lifecycleActorIds = new ActorId[LargeCount];

        for (int i = 0; i < LargeCount; i++)
        {
            var actor = _lifecycleWorld.CreateActor<LifecycleActor>();
            _lifecycleActorIds[i] = actor.GetActorId();
        }
    }

    /// <summary>
    /// 构建运行时创建销毁专用 ActorWorld。
    ///
    /// 作用：
    /// 把 Runtime Rent + Return 和 Cold Create + Destroy 分开。
    /// 这样不会把 new ActorWorld、池预热、内部数组初始化误判为帧内热路径 GC。
    /// </summary>
    private void BuildRuntimeCreateDestroyWorld()
    {
        _runtimeCreateDestroyWorld = new ActorWorld();
        _runtimeCreateDestroyWorld.PrewarmPool<PooledActor>(SmallCount);
        _runtimeCreateDestroyIds = new ActorId[SmallCount];
    }

    /// <summary>
    /// 强制把所有 Projected Entity 绑定成真实 Actor。
    ///
    /// 背景：
    /// WithProjectedActor<TActor>() 当前只是把 ProjectedActorMeta 标记为 Projectable。
    /// 如果 Setup 后立刻读取 meta.ActorId，很可能拿到 ActorId.Invalid。
    ///
    /// 这里使用 ProjectedActorTypeRegistry.CreateActorByTypeId 创建真实 Actor，
    /// 然后调用 meta.BindActor(handle.ActorId) 写回 ActorId。
    /// </summary>
    private void ForceBindAllProjectedActors()
    {
        for (int i = 0; i < SmallCount; i++)
        {
            _hybridSmallActorIds[i] = ForceBindProjectedActor(
                _ecsWorld,
                _actorWorld,
                _hybridSmallEntities[i]);
        }

        for (int i = 0; i < LargeCount; i++)
        {
            _hybridLargeActorIds[i] = ForceBindProjectedActor(
                _ecsWorld,
                _actorWorld,
                _hybridLargeEntities[i]);
        }
    }

    /// <summary>
    /// 强制绑定单个 Projected Entity。
    ///
    /// 参数说明：
    /// world：Entity 所在的 ECS World。
    /// actorWorld：要创建 Actor 的 ActorWorld。
    /// entity：需要绑定 Projected Actor 的 Entity。
    ///
    /// 返回值：
    /// 成功时返回有效 ActorId；失败时返回 ActorId.Invalid。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ActorId ForceBindProjectedActor(
        World world,
        ActorWorld actorWorld,
        Entity entity)
    {
        if (!world.TryGetProjectionMeta(entity, out var metaRef))
        {
            return ActorId.Invalid;
        }

        ref var meta = ref metaRef.Value;

        if (meta.HasActor)
        {
            return meta.ActorId;
        }

        if (meta.ActorTypeId < 0)
        {
            return ActorId.Invalid;
        }

        // CreateActorByTypeId 参数说明：
        // actorWorld：Actor 创建所在的 ActorWorld。
        // meta.ActorTypeId：Projected Actor 类型 ID，由 WithProjectedActor<TActor>() 注册。
        var handle = ProjectedActorTypeRegistry.CreateActorByTypeId(
            actorWorld,
            meta.ActorTypeId);

        if (!handle.IsValid)
        {
            return ActorId.Invalid;
        }

        // 避免 benchmark 运行过程中 projected actor 被 keep-alive 扫描回收。
        handle.Actor.RecycleDeadlineTicks = long.MaxValue;

        meta.BindActor(handle.ActorId);

        // ProjectedActorRef：
        // Entity 到 ActorId 的热路径缓存组件。
        // Hybrid / FullPipeline benchmark 会直接读取它，不再通过 TryGetProjectionMeta 反查。
        if (world.Has<ProjectedActorRef>(entity))
        {
            ref ProjectedActorRef actorRef =
                ref world.Get<ProjectedActorRef>(entity);

            actorRef.ActorId = handle.ActorId;
        }
        else
        {
            world.Add(
                entity,
                new ProjectedActorRef
                {
                    ActorId = handle.ActorId
                });
        }

        // AddActiveProjectedActor 只在 Setup 中调用。
        // 作用：让框架内部 active projected actor 列表保持一致。
        world.AddActiveProjectedActor(entity, ref meta);

        return handle.ActorId;
    }

    /// <summary>
    /// 预热一批 Actor 的邮箱。
    ///
    /// 参数说明：
    /// world：ActorWorld。
    /// actorIds：要预热的 ActorId 数组。
    /// count：预热数量。
    ///
    /// 作用：
    /// 触发每个 Actor 第一次接收 MoveEvent 时可能发生的邮箱 buffer 初始化。
    /// 如果预热后正式 benchmark 变为 0 B，就说明之前的 GC 是首次邮箱分配，不是稳定热路径分配。
    /// </summary>
    private void WarmupActorMailboxes(
        ActorWorld world,
        ActorId[] actorIds,
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!actorIds[i].IsValid)
            {
                continue;
            }

            world.PostTo(actorIds[i], in _moveEvent);
        }

        var budget = new RuntimeFrameBudget(
            maxEvents: count * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        world.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    /// <summary>
    /// 清空所有 ActorWorld 的事件残留。
    /// </summary>
    private void DrainAllActorWorlds()
    {
        DrainActorWorld(_actorWorld);
        DrainActorWorld(_pureActorWorld);
        DrainActorWorld(_pooledActorWorld);
        DrainActorWorld(_lifecycleWorld);
        DrainActorWorld(_runtimeCreateDestroyWorld);
    }

    /// <summary>
    /// 清空指定 ActorWorld 的邮箱。
    ///
    /// 参数说明：
    /// world：要清理的 ActorWorld。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DrainActorWorld(ActorWorld world)
    {
        for (int i = 0; i < MaxDrainPasses; i++)
        {
            var budget = new RuntimeFrameBudget(
                maxEvents: DrainEventBudget,
                usedEvents: 0,
                deadlineTicks: 0);

            world.Pump(
                deltaTime: 0.016f,
                fixedDeltaTime: 0.016f,
                pumpFixedUpdate: false,
                budget: ref budget);
        }
    }

    // ══════════════════════════════════════════════════════
    // Debug / Validation
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 验证 cached projected ActorId 是否全部有效。
    ///
    /// 如果结果不是 1000，说明 Hybrid cached ActorId benchmark 不能信。
    /// </summary>
    [Benchmark(Description = "Debug: Cached Projected ActorId Valid Count × 1000")]
    [BenchmarkCategory("Debug")]
    public void Debug_CachedProjectedActorId_ValidCount_1000()
    {
        int valid = 0;

        for (int i = 0; i < SmallCount; i++)
        {
            if (_hybridSmallActorIds[i].IsValid &&
                _actorWorld.TryGetActor(_hybridSmallActorIds[i], out _))
            {
                valid++;
            }
        }

        _intSink = valid;
    }

    /// <summary>
    /// 验证 projected meta 中 HasActor 的数量。
    ///
    /// 如果结果不是 1000，说明 WithProjectedActor 后没有完成真实 Actor 绑定。
    /// </summary>
    [Benchmark(Description = "Debug: Projected Meta HasActor Count × 1000")]
    [BenchmarkCategory("Debug")]
    public void Debug_ProjectedMeta_HasActorCount_1000()
    {
        int valid = 0;

        for (int i = 0; i < SmallCount; i++)
        {
            if (_ecsWorld.TryGetProjectionMeta(_hybridSmallEntities[i], out var metaRef) &&
                metaRef.Value.HasActor)
            {
                valid++;
            }
        }

        _intSink = valid;
    }

    /// <summary>
    /// 验证 ProjectedActorRef 中的 ActorId 是否全部有效。
    ///
    /// 作用：
    /// 如果这个测试结果不是 1000，说明 ForceBindProjectedActor 没有同步写入 ProjectedActorRef。
    /// </summary>
    [Benchmark(Description = "Debug: ProjectedRef ActorId Valid Count × 1000")]
    [BenchmarkCategory("Debug")]
    public void Debug_ProjectedRef_ActorId_ValidCount_1000()
    {
        int valid = 0;

        _ecsWorld.Query(
            in _hybridQuerySmall,
            (ref Position pos, ref Velocity vel, ref ProjectedActorRef actorRef) =>
            {
                if (actorRef.ActorId.IsValid &&
                    _actorWorld.TryGetActor(actorRef.ActorId, out _))
                {
                    valid++;
                }
            });

        _intSink = valid;
    }

    // ══════════════════════════════════════════════════════
    // Baseline
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 直接数组写入。
    /// 作用：作为非框架路径的最低成本对照。
    /// </summary>
    [Benchmark(Description = "Baseline: Direct Array Write × 10000", Baseline = true)]
    [BenchmarkCategory("Baseline")]
    public void Baseline_DirectArrayWrite_10000()
    {
        for (int i = 0; i < LargeCount; i++)
        {
            _baselinePositions[i].X += _baselineVelocities[i].Dx;
            _baselinePositions[i].Y += _baselineVelocities[i].Dy;
        }

        _floatSink = _baselinePositions[LargeCount - 1].X;
    }

    // ══════════════════════════════════════════════════════
    // Actor
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Actor 邮箱投递 + Pump，1000 个事件。
    /// </summary>
    [Benchmark(Description = "Actor: PostTo + Pump × 1000")]
    [BenchmarkCategory("Actor")]
    public void Actor_PostTo_Pump_1000()
    {
        for (int i = 0; i < SmallCount; i++)
        {
            _pureActorWorld.PostTo(_pureActorIds[i], in _moveEvent);
        }

        var budget = new RuntimeFrameBudget(
            maxEvents: SmallCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        _pureActorWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    /// <summary>
    /// Actor 邮箱投递 + Pump，10000 个事件。
    /// </summary>
    [Benchmark(Description = "Actor: PostTo + Pump × 10000")]
    [BenchmarkCategory("Actor")]
    public void Actor_PostTo_Pump_10000()
    {
        for (int i = 0; i < LargeCount; i++)
        {
            _pureActorWorld.PostTo(_pureActorIds[i], in _moveEvent);
        }

        var budget = new RuntimeFrameBudget(
            maxEvents: LargeCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        _pureActorWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    /// <summary>
    /// Actor 同步分发。
    /// 作用：绕过邮箱，直接调用 ActorBehaviour。
    /// </summary>
    [Benchmark(Description = "Actor: DispatchNow × 1000")]
    [BenchmarkCategory("Actor")]
    public void Actor_DispatchNow_1000()
    {
        for (int i = 0; i < SmallCount; i++)
        {
            _pureActorWorld.DispatchNow(_pureActorIds[i], in _moveEvent);
        }
    }

    /// <summary>
    /// 池化 Actor 邮箱投递 + Pump。
    /// </summary>
    [Benchmark(Description = "Pooled Actor: PostTo + Pump × 1000")]
    [BenchmarkCategory("Actor")]
    public void PooledActor_PostTo_Pump_1000()
    {
        for (int i = 0; i < SmallCount; i++)
        {
            _pooledActorWorld.PostTo(_pooledActorIds[i], in _moveEvent);
        }

        var budget = new RuntimeFrameBudget(
            maxEvents: SmallCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        _pooledActorWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    /// <summary>
    /// Unsupported Event：只投递，不 Pump。
    ///
    /// 作用：
    /// 如果这个测试产生接近 12KB 分配，说明分配来自 PostTo 的失败路径。
    /// 如果这个测试 0 分配，而 PostTo + Pump 有分配，说明分配来自 Pump 处理 unsupported event 的路径。
    /// </summary>
    [Benchmark(Description = "Actor: Unsupported Event Post Only × 100")]
    [BenchmarkCategory("Actor-GC")]
    public void Actor_UnsupportedEvent_PostOnly_100()
    {
        for (int i = 0; i < UnsupportedEventCount; i++)
        {
            _pureActorWorld.PostTo(_pureActorIds[i], in _damageEvent);
        }
    }

    /// <summary>
    /// Unsupported Event：投递 + Pump。
    ///
    /// 作用：
    /// 对照 Post Only，用来判断 GC 来自投递失败，还是来自 Pump 阶段。
    /// </summary>
    [Benchmark(Description = "Actor: Unsupported Event PostTo + Pump × 100")]
    [BenchmarkCategory("Actor-GC")]
    public void Actor_UnsupportedEvent_PostTo_Pump_100()
    {
        for (int i = 0; i < UnsupportedEventCount; i++)
        {
            _pureActorWorld.PostTo(_pureActorIds[i], in _damageEvent);
        }

        var budget = new RuntimeFrameBudget(
            maxEvents: UnsupportedEventCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        _pureActorWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    /// <summary>
    /// Cold Actor 创建 + 销毁。
    ///
    /// 注意：
    /// 这个测试包含 new ActorWorld、内部数组、存储结构初始化。
    /// 它是冷启动成本，不代表帧内热路径 GC。
    /// </summary>
    [Benchmark(Description = "Actor Cold: New World + Create + Destroy × 1000")]
    [BenchmarkCategory("Actor-Cold")]
    public void ActorCold_NewWorld_Create_Destroy_1000()
    {
        var tempWorld = new ActorWorld();
        var tempIds = new ActorId[SmallCount];

        for (int i = 0; i < SmallCount; i++)
        {
            var actor = tempWorld.CreateActor<MinimalActor>();
            tempIds[i] = actor.GetActorId();
        }

        for (int i = 0; i < SmallCount; i++)
        {
            tempWorld.DestroyActor(tempIds[i]);
        }

        var budget = new RuntimeFrameBudget(
            maxEvents: SmallCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        tempWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    /// <summary>
    /// Runtime 池化 Actor 租借 + 归还。
    ///
    /// 作用：
    /// 测量运行期对象池复用成本，不包含 new ActorWorld 和池预热。
    /// </summary>
    [Benchmark(Description = "Pooled Actor Runtime: Rent + Return × 1000")]
    [BenchmarkCategory("Actor")]
    public void PooledActorRuntime_Rent_Return_1000()
    {
        for (int i = 0; i < SmallCount; i++)
        {
            var actor = _runtimeCreateDestroyWorld.CreateActor<PooledActor>(usePool: true);
            _runtimeCreateDestroyIds[i] = actor.GetActorId();
        }

        for (int i = 0; i < SmallCount; i++)
        {
            _runtimeCreateDestroyWorld.DestroyActor(_runtimeCreateDestroyIds[i]);
        }

        var budget = new RuntimeFrameBudget(
            maxEvents: SmallCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        _runtimeCreateDestroyWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    // ══════════════════════════════════════════════════════
    // ECS
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// ECS Query 遍历 1000 个实体。
    /// </summary>
    [Benchmark(Description = "ECS: Query<Position,Velocity> × 1000")]
    [BenchmarkCategory("ECS")]
    public void ECS_Query_PositionVelocity_1000()
    {
        _ecsWorld.Query(
            in _ecsQuerySmall,
            static (ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.Dx;
                pos.Y += vel.Dy;
            });
    }

    /// <summary>
    /// ECS Query 遍历 10000 个实体。
    /// </summary>
    [Benchmark(Description = "ECS: Query<Position,Velocity> × 10000")]
    [BenchmarkCategory("ECS")]
    public void ECS_Query_PositionVelocity_10000()
    {
        _ecsWorld.Query(
            in _ecsQueryLarge,
            static (ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.Dx;
                pos.Y += vel.Dy;
            });
    }

    /// <summary>
    /// ECS Entity 创建 + 销毁。
    /// </summary>
    [Benchmark(Description = "ECS: World.Create + Destroy × 1000")]
    [BenchmarkCategory("ECS")]
    public void ECS_Create_Destroy_1000()
    {
        for (int i = 0; i < SmallCount; i++)
        {
            _createDestroyTempEntities[i] = _ecsWorld.Create(
                new Position
                {
                    X = i,
                    Y = i
                },
                new Velocity
                {
                    Dx = 1,
                    Dy = 1
                },
                new EcsCreateDestroyTag());
        }

        for (int i = 0; i < SmallCount; i++)
        {
            _ecsWorld.Destroy(_createDestroyTempEntities[i]);
        }
    }

    // ══════════════════════════════════════════════════════
    // Projection
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Entity → ProjectionMeta → ActorId 查找，1000 个。
    /// </summary>
    [Benchmark(Description = "Projection: Entity → ActorId Lookup × 1000")]
    [BenchmarkCategory("Projection")]
    public void Projection_EntityToActorId_Lookup_1000()
    {
        int found = 0;

        for (int i = 0; i < SmallCount; i++)
        {
            if (_ecsWorld.TryGetProjectionMeta(_hybridSmallEntities[i], out var metaRef) &&
                metaRef.Value.HasActor)
            {
                found++;
            }
        }

        _intSink = found;
    }

    /// <summary>
    /// Entity → ProjectionMeta → ActorId 查找，10000 个。
    /// </summary>
    [Benchmark(Description = "Projection: Entity → ActorId Lookup × 10000")]
    [BenchmarkCategory("Projection")]
    public void Projection_EntityToActorId_Lookup_10000()
    {
        int found = 0;

        for (int i = 0; i < LargeCount; i++)
        {
            if (_ecsWorld.TryGetProjectionMeta(_hybridLargeEntities[i], out var metaRef) &&
                metaRef.Value.HasActor)
            {
                found++;
            }
        }

        _intSink = found;
    }

    /// <summary>
    /// ProjectedActorRef → ActorId 读取，1000 个。
    ///
    /// 作用：
    /// 测量新的 projected cache 热路径。
    /// 该路径不再通过 Entity 调 TryGetProjectionMeta，也不再读取 ProjectedActorMeta。
    /// </summary>
    [Benchmark(Description = "Projection: ProjectedRef ActorId Read × 1000")]
    [BenchmarkCategory("Projection")]
    public void Projection_ProjectedRef_ActorIdRead_1000()
    {
        int found = 0;

        _ecsWorld.Query(
            in _hybridQuerySmall,
            (ref Position pos, ref Velocity vel, ref ProjectedActorRef actorRef) =>
            {
                if (actorRef.ActorId.IsValid)
                {
                    found++;
                }
            });

        _intSink = found;
    }

    /// <summary>
    /// ProjectedActorRef → ActorId 读取，10000 个。
    ///
    /// 作用：
    /// 测量新的 projected cache 大批量热路径。
    /// </summary>
    [Benchmark(Description = "Projection: ProjectedRef ActorId Read × 10000")]
    [BenchmarkCategory("Projection")]
    public void Projection_ProjectedRef_ActorIdRead_10000()
    {
        int found = 0;

        _ecsWorld.Query(
            in _hybridQueryLarge,
            (ref Position pos, ref Velocity vel, ref ProjectedActorRef actorRef) =>
            {
                if (actorRef.ActorId.IsValid)
                {
                    found++;
                }
            });

        _intSink = found;
    }

    // ══════════════════════════════════════════════════════
    // Hybrid
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Cached ActorId 直接 PostTo + Pump。
    ///
    /// 这个测试用于验证：
    /// 如果 cached ActorId 全部有效，并且邮箱已经预热，那么这里应该接近 0 GC。
    /// </summary>
    [Benchmark(Description = "Hybrid Isolate: Cached ActorId PostTo + Pump × 1000")]
    [BenchmarkCategory("Hybrid")]
    public void Hybrid_CachedActorId_PostTo_Pump_1000()
    {
        for (int i = 0; i < SmallCount; i++)
        {
            _actorWorld.PostTo(_hybridSmallActorIds[i], in _moveEvent);
        }

        var budget = new RuntimeFrameBudget(
            maxEvents: SmallCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        _actorWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    /// <summary>
    /// ECS Query → Projection Lookup → Actor PostTo，1000 个。
    /// 不包含 Pump。
    /// </summary>
    [Benchmark(Description = "Hybrid: ECS Query → ProjectedRef → Actor PostTo × 1000")]
    [BenchmarkCategory("Hybrid")]
    public void Hybrid_ECSQuery_ProjectedRef_ActorPost_1000()
    {
        _ecsWorld.Query(
            in _hybridQuerySmall,
            (ref Position pos, ref Velocity vel, ref ProjectedActorRef actorRef) =>
            {
                ActorId actorId = actorRef.ActorId;

                if (!actorId.IsValid)
                {
                    return;
                }

                _actorWorld.PostTo(
                    actorId,
                    in _moveEvent);
            });
    }

    /// <summary>
    /// ECS Query → Projection Lookup → Actor PostTo，10000 个。
    /// 不包含 Pump。
    /// </summary>
    [Benchmark(Description = "Hybrid: ECS Query → ProjectedRef → Actor PostTo × 10000")]
    [BenchmarkCategory("Hybrid")]
    public void Hybrid_ECSQuery_ProjectedRef_ActorPost_10000()
    {
        _ecsWorld.Query(
            in _hybridQueryLarge,
            (ref Position pos, ref Velocity vel, ref ProjectedActorRef actorRef) =>
            {
                ActorId actorId = actorRef.ActorId;

                if (!actorId.IsValid)
                {
                    return;
                }

                _actorWorld.PostTo(
                    actorId,
                    in _moveEvent);
            });
    }

    /// <summary>
    /// 完整小批量链路：
    /// ECS Query → Projection Lookup → Actor PostTo → Pump。
    /// </summary>
    [Benchmark(Description = "Full Pipeline: ECS Query → ProjectedRef → Actor PostTo → Pump × 1000")]
    [BenchmarkCategory("Hybrid")]
    public void FullPipeline_ECSQuery_ProjectedRef_ActorPost_Pump_1000()
    {
        _ecsWorld.Query(
            in _hybridQuerySmall,
            (ref Position pos, ref Velocity vel, ref ProjectedActorRef actorRef) =>
            {
                ActorId actorId = actorRef.ActorId;

                if (!actorId.IsValid)
                {
                    return;
                }

                _actorWorld.PostTo(
                    actorId,
                    in _moveEvent);
            });

        var budget = new RuntimeFrameBudget(
            maxEvents: SmallCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        _actorWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    /// <summary>
    /// 完整大批量链路：
    /// ECS Query → Projection Lookup → Actor PostTo → Pump。
    /// </summary>
    [Benchmark(Description = "Full Pipeline: ECS Query → ProjectedRef → Actor PostTo → Pump × 10000")]
    [BenchmarkCategory("Hybrid")]
    public void FullPipeline_ECSQuery_ProjectedRef_ActorPost_Pump_10000()
    {
        _ecsWorld.Query(
            in _hybridQueryLarge,
            (ref Position pos, ref Velocity vel, ref ProjectedActorRef actorRef) =>
            {
                ActorId actorId = actorRef.ActorId;

                if (!actorId.IsValid)
                {
                    return;
                }

                _actorWorld.PostTo(
                    actorId,
                    in _moveEvent);
            });

        var budget = new RuntimeFrameBudget(
            maxEvents: LargeCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        _actorWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    // ══════════════════════════════════════════════════════
    // Lifecycle
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 生命周期 Update 调度，10000 个 Actor。
    /// </summary>
    [Benchmark(Description = "Lifecycle: Pump Update × 10000 actors")]
    [BenchmarkCategory("Lifecycle")]
    public void Lifecycle_PumpUpdate_10000()
    {
        var budget = new RuntimeFrameBudget(
            maxEvents: LargeCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        _lifecycleWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }
    [Benchmark(Description = "Actor: PostTo Only × 1000")]
    [BenchmarkCategory("Actor-Split")]
    public void Actor_PostToOnly_1000()
    {
        for (int i = 0; i < SmallCount; i++)
        {
            _pureActorWorld.PostTo(_pureActorIds[i], in _moveEvent);
        }
    }
    [IterationSetup(Target = nameof(Actor_PumpOnly_1000))]
    public void Setup_Actor_PumpOnly_1000()
    {
        for (int i = 0; i < SmallCount; i++)
        {
            // 提前塞入事件，让正式 benchmark 只测 Pump。
            _pureActorWorld.PostTo(_pureActorIds[i], in _moveEvent);
        }
    }

    [Benchmark(Description = "Actor: Pump Only × 1000")]
    [BenchmarkCategory("Actor-Split")]
    public void Actor_PumpOnly_1000()
    {
        var budget = new RuntimeFrameBudget(
            maxEvents: SmallCount * 2,
            usedEvents: 0,
            deadlineTicks: 0);

        _pureActorWorld.Pump(
            deltaTime: 0.016f,
            fixedDeltaTime: 0.016f,
            pumpFixedUpdate: false,
            budget: ref budget);
    }

    // ─────────────────────────────────────────────────────
    // 辅助类型
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Benchmark 专用 Layer。
    /// </summary>
    private sealed partial class EcsActorBenchLayer : Layer { }

    /// <summary>
    /// Benchmark 专用 Service。
    /// 当前不注册额外服务，只用于满足 Layer 构建流程。
    /// </summary>
    private sealed class EcsActorBenchService : IService
    {
        /// <summary>
        /// 配置服务。
        ///
        /// 参数说明：
        /// services：LayerBase 的服务注册容器。
        /// 当前 benchmark 不需要额外服务，所以方法体为空。
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }
}