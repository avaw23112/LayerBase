# LayerBase 多线程异步运行时设计方案：Query / Bring / Actor / Worker 正确模型版

## 0. 设计目标

LayerBase 的多线程模型不改变用户已经接受的业务写法，而是改变底层执行域。

目标是：

```text id="ee1dkr"
1. 普通 Query 仍然写成 Query + ForEach。
2. Bring Query 仍然写成 Query + Bring + ForEach + Batch + Post。
3. ActorBehaviour 仍然只写在 IActor 类中。
4. Worker 仍然以 Job 形式存在，完成后回流 Event。
5. 异步模式下，ECS 由 EcsWorker 独占。
6. 主线程不直接读写异步 EcsWorld。
7. 普通 Query 不碰 Actor。
8. 只有 Bring Query 可以 Touch Actor / Post ActorEvent。
```

一句话：

```text id="urn9it"
Context 编排任务，ECS 处理真实大规模数据，Bring 把 ECS 结果带给 Actor，Actor 被动表现，Worker 处理纯计算和 IO。
```

---

## 1. 现有代码确认的基础语义

### 1.1 Query 有两条路径

当前 `QueryAttribute` 的注释已经说明，源生成器会根据 `[Query]` 生成两类代码：

```text id="qcrdvv"
Query + ForEach
Query + Bring + Post
```

这说明普通 Query 和 Bring Query 在现有模型里本来就是两条不同路径。

---

### 1.2 Bring 的真实语义是“输出 Actor 事件类型”

`BringAttribute` 的注释是：

```text id="5yq5ij"
标记一个 [Query] 方法要输出的 Actor 事件类型。
```

它内部保存的是当前 Query 方法要输出的 Actor 事件类型集合。

所以 Bring 不是普通 Query 的性能优化，也不是普通事件修饰符。

它的真实语义是：

```text id="rfzfqa"
ECS Query 处理完成后，允许把结果投递给 Projected Actor。
```

---

### 1.3 ProjectResult 决定 Bring 的 Actor 行为

`ProjectResult` 已经定义了三种结果：

```text id="877k08"
Fail：
  不 Touch，不 Post。

Touch：
  TouchProjectedActor，不 Post。

Success：
  TouchProjectedActor，并 Post Bring 事件。
```

这说明 Bring 还承担 ProjectedActor 生命周期控制，而不仅是发事件。

---

### 1.4 IActorEvent 只能用于 Bring / Actor Handler / ActorWorld.PostTo

`IActorEvent` 注释说明它是 Actor 行为事件标记接口，只能用于 Bring、Actor Handler、ActorWorld.PostTo。

因此正式规则应写成：

```text id="iu48qd"
普通 ECS Query 不能产出 IActorEvent。
只有 Bring Query 可以产出 IActorEvent。
```

---

### 1.5 生成器已经区分普通 Query 与 Bring Query

当前 `QueryBringGenerator` 在普通 Query 分支生成：

```csharp id="x5wswy"
.Query<T...>(this)
.ForEach(ref job);
```

在 Bring 分支生成：

```csharp id="1r3246"
.Query<T...>(this)
.Bring<TEvent...>()
.ForEach(ref job)
.Batch()
.Post();
```

也就是说，普通 Query 使用 `IQueryJob<T...>`，Bring Query 使用 `IProjectionJob{C}x{E}<T...>`。

---

### 1.6 测试已经验证“普通 Query 不碰 Actor”

`QueryBringJobTests` 里对纯 Query 的测试注释明确写着：

```text id="8rp4fd"
纯 Query + Job ForEach 能正确遍历并修改组件数据。
不创建 Actor，不 Touch Actor，不 Post ActorEvent。
```

测试只执行 `Query<Position, Velocity>().ForEach(ref job)`，然后验证 ECS 组件被修改。

Bring Success 分支则验证：

```text id="8einym"
修改 ECS 数据。
Touch/Ensure Actor。
Add Event 到 Batch。
Post ActorEvent。
```

对应链路是 `Query + Bring + ForEach + Batch + Post`。

Touch / Fail 测试也分别验证了 Touch 不 Post、Fail 不 Touch 不 Post。

---

### 1.7 ActorBehaviour 是 IActor 内的被动处理入口

`ActorBehaviourGenerator` 会检查使用 ActorBehaviour 的类必须实现 `LayerBase.Actor.IActor`，否则报错。

ActorBehaviour 方法还必须满足：

```text id="3ft69g"
实例方法。
返回 void。
只有一个参数。
参数必须是 in。
参数类型必须是 struct。
同一个 Actor 内不能重复处理同一事件类型。
```

这些约束说明 ActorBehaviour 是被 ActorEvent 驱动的处理器，不是 Context 级业务编排入口。

---

# 2. 正确心智模型

LayerBase 应该把运行时拆成四个概念：

```text id="hvq2wu"
Context / Service：
  业务编排入口。
  管理小规模即时状态。
  触发 Query。
  提交 WorkerJob。
  发送普通 Event。

ECS：
  真实大规模数据源。
  普通 Query 只处理 ECS 数据。
  不直接控制 Actor。

Bring：
  ECS -> Actor 的投影通信边界。
  只有 Bring 可以 Touch ProjectedActor。
  只有 Bring 可以生成 ActorEvent。

Actor：
  被动表现 / 行为切片。
  只响应 ActorEvent。
  不主动拿 Context。
  不直接访问 ECS。
```

一句话：

```text id="a53n2o"
Query 算 ECS，Bring 带结果给 Actor，Actor 被事件驱动表现。
```

---

# 3. 线程域设计

## 3.1 Main Thread

主线程拥有：

```text id="z93w51"
Context / Service 编排
PostScheduler 主处理阶段
ActorWorld
ActorScheduler
EventStreamCenter 主处理阶段
Timer / Delay
引擎对象桥接
表现系统
```

主线程不应该在异步 ECS 模式下直接访问 `EcsWorld`。

---

## 3.2 EcsWorker Thread

异步模式下，EcsWorker 独占：

```text id="l67m7b"
EcsWorld
ECS Query 执行
组件读写
结构性 ECS 命令应用
Projection / Bring 扫描
ActorEventCommandBuffer 生成
```

它不能直接调用：

```text id="ig4ht7"
ActorWorld
EventStreamCenter 内部结构
PostScheduler 内部结构
Context / Service
Unity / Godot 引擎对象
```

它只能把结果放入 MPSC 队列，等待主线程处理。

---

## 3.3 WorkerPool

WorkerPool 负责：

```text id="k8s8hb"
纯计算
IO
资源解析
存档编解码
压缩 / 解压缩
寻路
AI 评分
过程生成
流场 / 压力场
```

WorkerPool 的结果以普通 Event 回流到主线程 PostScheduler。

---

# 4. ECS 执行模式

Runtime 初始化时选择 ECS 执行模式。

```csharp id="owq4fh"
public enum EcsExecutionMode
{
    Sync,
    Async
}
```

配置：

```csharp id="9i0odx"
LayerRuntime runtime = LayerRuntimeBuilder.Create()
    .UseEcsExecutionMode(EcsExecutionMode.Async)
    .Build();
```

构建后不可切换。

---

## 4.1 Sync 模式

```text id="0puqc6"
EcsWorld 归主线程所有。
Query.ForEach 立即执行。
Bring.Post 立即执行 ECS 部分。
ActorEvent 仍建议进入统一队列，按 Pump 阶段处理。
```

适合：

```text id="egc3ow"
测试
小项目
调试
单线程环境
```

---

## 4.2 Async 模式

```text id="y55rsa"
EcsWorld 归 EcsWorker 独占。
Query.ForEach 不立即执行。
Bring.Post 不立即执行。
主线程只收集任务、输入数据、生成 WorkItem。
WorkItem 投递到 EcsWorker MPSC 队列。
ECS 结果分帧回流。
```

禁止：

```text id="2dj9xh"
主线程直接 Query EcsWorld。
主线程直接 GetComponent / SetComponent。
Actor 直接访问 ECS。
Worker 直接访问 ECS。
```

允许：

```text id="tmfqe5"
Context / Service 调用现有 Query API。
普通 Query 生成 PlainEcsQueryWorkItem。
Bring Query 生成 BringProjectionWorkItem。
```

---

# 5. 对外 Query API 不变

这条是本设计最重要的外部约束：

```text id="j343n8"
不新增主入口。
不要求用户改成 PostBlock。
不破坏当前 Query / Bring 模型。
```

开发者仍然写：

```csharp id="4iaw1u"
context.Query<Position, Velocity>()
    .ForEach(ref job);
```

或者通过源生成器写：

```csharp id="4xf4k5"
[Query]
private void OnMove(ref Position position, in Velocity velocity)
{
    position.Value += velocity.Value;
}
```

生成器仍然生成入口方法，底层再根据 `EcsExecutionMode` 决定立即执行还是提交 EcsWorker。

---

# 6. 普通 Query：纯 ECS 运算

## 6.1 业务写法

```csharp id="kt2v3t"
[Query]
private void OnMove(ref Position position, in Velocity velocity)
{
    position.Value += velocity.Value;
}
```

生成入口仍然类似：

```csharp id="yoo7ir"
public void Move()
{
    var job = new __MoveJob(this);

    this.Query<Position, Velocity>()
        .ForEach(ref job);
}
```

---

## 6.2 Sync 模式语义

```text id="ih8wao"
Move()
  ↓
立即遍历 ECS
  ↓
修改 Position
  ↓
结束
```

---

## 6.3 Async 模式语义

```text id="mdqttb"
Move()
  ↓
收集 Query 描述
  ↓
收集输入数据
  ↓
打包 PlainEcsQueryWorkItem
  ↓
投递到 EcsWorker MPSC
  ↓
返回主线程
```

EcsWorker：

```text id="r0k73a"
Dequeue PlainEcsQueryWorkItem
  ↓
执行 Query
  ↓
修改 ECS 数据
  ↓
结束
```

普通 Query 不产生 ActorEvent。

正式规则：

```text id="mswn91"
Plain Query 只能读写 ECS 组件。
Plain Query 不允许 Touch Actor。
Plain Query 不允许 Post ActorEvent。
```

---

# 7. Bring Query：ECS -> Actor 通讯边界

## 7.1 业务写法

```csharp id="dl9h6x"
[Query]
[Bring<MoveViewEvent>]
private ProjectResult OnMoveView(
    ref Position position,
    in Velocity velocity,
    ref MoveViewEvent moveView)
{
    position.Value += velocity.Value;

    moveView = new MoveViewEvent(position.Value);

    return ProjectResult.Success;
}
```

生成入口仍然类似：

```csharp id="wzn1x2"
public void MoveView()
{
    var job = new __MoveViewJob(this);

    this.Query<Position, Velocity>()
        .Bring<MoveViewEvent>()
        .ForEach(ref job)
        .Batch()
        .Post();
}
```

---

## 7.2 Sync 模式语义

```text id="1qv47g"
MoveView()
  ↓
立即执行 ECS Query
  ↓
根据 ProjectResult Touch / Post
  ↓
ActorEvent 进入 Actor 投递链路
```

---

## 7.3 Async 模式语义

```text id="5w4n7q"
MoveView()
  ↓
收集 Query 描述
  ↓
收集 Bring 事件类型
  ↓
收集输入数据
  ↓
打包 BringProjectionWorkItem
  ↓
投递到 EcsWorker MPSC
  ↓
返回主线程
```

EcsWorker：

```text id="s05rnh"
Dequeue BringProjectionWorkItem
  ↓
执行 Query
  ↓
对每个匹配实体执行 ProjectionJob
  ↓
根据 ProjectResult：
      Fail    -> 不 Touch，不 Post
      Touch   -> Touch ProjectedActor，不 Post
      Success -> Touch ProjectedActor，并写 ActorEventCommandBuffer
  ↓
ActorEventCommandBuffer 入 EcsResultQueue
```

主线程：

```text id="k9b73z"
Drain EcsResultQueue
  ↓
Flush ActorEventCommandBuffer
  ↓
ActorScheduler / EventStreamCenter
  ↓
ActorBehaviour 被动处理
```

---

# 8. Actor 模型修正

Actor 不是业务编排入口。

Actor 的定位是：

```text id="y9or0n"
被动表现 / 行为切片。
```

Actor 只能通过 ActorEvent 被操控。

Actor 不应该：

```text id="53fz3e"
主动访问 ECS。
主动访问 ILayerContext。
主动访问 Service。
作为真实数据源。
负责大规模数据。
```

Actor 应该：

```text id="yht1aj"
保存表现字段。
播放动画 / 音效 / 特效。
维护轻量表现状态。
响应 ActorEvent。
```

示例：

```csharp id="qg1u0v"
public sealed partial class EnemyActor : IActor
{
    private float _viewX;
    private float _viewY;

    [ActorBehaviour]
    private void OnMoveView(in MoveViewEvent e)
    {
        _viewX = e.X;
        _viewY = e.Y;

        PlayMoveAnimation();
    }
}
```

心智：

```text id="zjxga7"
ECS 告诉 Actor 发生了什么。
Actor 负责怎么表现。
```

---

# 9. Worker 模型

Worker 用于 ECS 之外的纯计算、IO、资源加载、存档编解码。

## 9.1 WorkerEventJob

```csharp id="zl9ksn"
public interface IWorkerEventJob<TInput, TEvent>
    where TInput : struct
    where TEvent : struct
{
    TEvent Execute(in TInput input);
}
```

调用：

```csharp id="c5fb7p"
WorkerHandle handle = context.Worker.RunEventJob(
    new BuildPressureFieldJob(),
    input);
```

流程：

```text id="za6bwa"
Main Thread:
  RunEventJob
  ↓
  WorkerJobQueue.Enqueue

WorkerPool:
  Execute
  ↓
  返回 TEvent
  ↓
  WorkerEventQueue.Enqueue

Main Thread:
  Drain WorkerEventQueue
  ↓
  PostScheduler.Post(TEvent)
```

---

## 9.2 WorkerHandle

```csharp id="x6s4dt"
public readonly struct WorkerHandle
{
    public readonly int Id;
    public readonly int Version;
}
```

状态：

```csharp id="6kasxu"
public enum WorkerState
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
```

常用操作：

```csharp id="3ien7x"
WorkerState state = context.Worker.GetState(handle);

context.Worker.Cancel(handle);
```

第一版规则：

```text id="53ggfb"
未开始的 Job 可以取消。
已经开始的 Job 不强行中断。
```

---

## 9.3 Worker 异常

异常统一转成事件：

```csharp id="k113q8"
public readonly struct WorkerJobFailedEvent : ILayerDto
{
    public readonly WorkerHandle Handle;
    public readonly Type JobType;
    public readonly Exception Exception;
}
```

处理方式：

```csharp id="k87qev"
[Subscribe]
private void OnWorkerFailed(in WorkerJobFailedEvent e)
{
    logger.Error(e.Exception);
}
```

---

# 10. 资源加载与存档

资源和存档不是直接把 Context 丢给 Worker。

它们必须拆成：

```text id="cylo5z"
主线程 Capture
  ↓
Worker Encode / Decode / IO
  ↓
主线程 Apply / Event
```

---

## 10.1 保存

```text id="q9ly1w"
主线程：
  Runtime / Context / Service / Actor / ECS
  -> SnapDocument / SaveSnapshot

Worker：
  SnapDocument / SaveSnapshot
  -> Binary / Json
  -> Compress
  -> Write File

主线程：
  SaveCompletedEvent
```

示例：

```csharp id="in7p7y"
SnapDocument document = runtime.FullSnap.Serialize();

WorkerHandle handle = context.Worker.RunEventJob(
    new SaveDocumentJob(),
    new SaveDocumentInput(saveId, path, document));
```

---

## 10.2 读取

```text id="qvsjc2"
Worker：
  Read File
  -> Decompress
  -> Decode
  -> SnapDocument

主线程：
  runtime.FullSnap.Deserialize(document)
  -> LoadAppliedEvent
```

示例：

```csharp id="6j2pfy"
WorkerHandle handle = context.Worker.RunEventJob(
    new LoadDocumentJob(),
    new LoadDocumentInput(saveId, path));
```

事件处理：

```csharp id="f0gkrl"
[Subscribe]
private void OnSaveLoaded(in SaveLoadedEvent e)
{
    runtime.FullSnap.Deserialize(e.Document);

    context.Post(new LoadAppliedEvent(e.SaveId));
}
```

---

# 11. 异步 ECS 下的生成器关键问题

当前生成器生成的 Job 会保存 `_self`：

```text id="osn487"
private readonly SelfType _self;
```

然后在 Execute 里调用：

```text id="f1ie20"
_self.OnXxx(...)
```

这个在同步模式下没问题。

但在异步 ECS 模式下要警惕：

```text id="91nvnz"
_self 可能是 Context / Service / Manager 实例。
如果 EcsWorker 执行 job.Execute，就会跨线程访问 _self。
```

因此异步模式需要对 Query 方法做额外限制。

---

## 11.1 异步 Query 方法限制

异步 ECS 模式下，[Query] 方法必须满足：

```text id="hot9jn"
1. 不访问 context。
2. 不访问 ActorWorld。
3. 不访问 Service 可变字段。
4. 不访问引擎对象。
5. 不访问非线程安全引用对象。
6. 只使用参数组件、值类型输入、只读配置。
```

推荐下一步改造生成器：

```text id="l2m91g"
将 Query 方法生成成更纯的 Job。
尽量避免 EcsWorker 持有业务对象 _self。
```

---

## 11.2 推荐的长期写法

普通 Query：

```csharp id="py7hqz"
[Query]
private static void OnMove(
    ref Position position,
    in Velocity velocity,
    in MoveInput input)
{
    position.Value += velocity.Value * input.DeltaTime;
}
```

或者通过生成器把外部输入显式打包：

```csharp id="39es50"
Move(new MoveInput(deltaTime));
```

而不是让 Job 捕获整个 `this`。

---

## 11.3 第一版兼容方案

第一版可以保留 `_self` 生成，但 Analyzer 要限制：

```text id="0tr91l"
异步 ECS 模式下，[Query] 方法体不能访问实例字段。
不能调用实例方法。
不能访问 this。
```

否则异步模式下会出现隐藏数据竞争。

更好的第二版是改生成器，让 Query Job 不再持有 `_self`，而是只持有显式输入。

---

# 12. MPSC 队列设计

## 12.1 Main -> ECS

```text id="kb3bhm"
MPSC<EcsWorkItem>
```

虽然第一版可能只有主线程提交，但后续 Worker / IO / 工具也可能提交 ECS 命令，所以使用 MPSC。

WorkItem 类型：

```text id="cu17ts"
PlainEcsQueryWorkItem
BringProjectionWorkItem
EcsStructuralCommandWorkItem
```

---

## 12.2 ECS -> Main

```text id="3fyfz0"
MPSC<EcsResult>
```

其中：

```text id="te67yy"
Plain Query：
  通常没有 EcsResult，除非需要统计 / Fence。

Bring Query：
  EcsResult 携带 ActorEventCommandBuffer。

错误：
  EcsResult 携带 EcsWorkFailedEvent。
```

---

## 12.3 Worker -> Main

```text id="57j68b"
MPSC<WorkerEventEnvelope>
```

Worker 完成后，把普通 Event 放入 WorkerEventQueue，主线程 Drain 后交给 PostScheduler。

---

# 13. Pump 阶段

建议主线程 Pump 顺序：

```text id="x1seoe"
Frame Begin:
  1. Drain WorkerEventQueue -> PostScheduler
  2. Drain EcsResultQueue -> ActorScheduler / Actor EventStream
  3. Pump PostScheduler
  4. Pump ActorScheduler / ActorWorld
  5. Pump Timer / Delay
  6. Context / Service Update 提交 Query / WorkerJob
  7. Flush EcsWorkQueue
Frame End
```

EcsWorker 独立循环：

```text id="ibtycs"
while running:
  1. Dequeue EcsWorkItem
  2. Execute Plain Query or Bring Query
  3. Produce EcsResult if needed
  4. Enqueue result to Main
```

---

# 14. 开发者业务场景

## 14.1 纯 ECS 位移

```csharp id="cbx94l"
[Query]
private void OnMove(ref Position position, in Velocity velocity)
{
    position.Value += velocity.Value;
}
```

开发者心智：

```text id="09cu6c"
这是纯 ECS 运算。
不会碰 Actor。
异步模式下由 EcsWorker 执行。
```

---

## 14.2 ECS 驱动 Actor 表现

```csharp id="34cj55"
[Query]
[Bring<MoveViewEvent>]
private ProjectResult OnMoveView(
    ref Position position,
    in Velocity velocity,
    ref MoveViewEvent moveView)
{
    position.Value += velocity.Value;

    moveView = new MoveViewEvent(position.Value);

    return ProjectResult.Success;
}
```

开发者心智：

```text id="dauq0r"
这是 ECS -> Actor 通讯。
只有 Success 才会 Post ActorEvent。
Touch 只保活 Actor。
Fail 什么都不做。
```

---

## 14.3 Actor 被动表现

```csharp id="w7fe1o"
public sealed partial class EnemyActor : IActor
{
    [ActorBehaviour]
    private void OnMoveView(in MoveViewEvent e)
    {
        PlayMoveAnimation(e.Position);
    }
}
```

开发者心智：

```text id="cezfl7"
Actor 不问数据从哪里来。
Actor 只接收事件并表现。
```

---

## 14.4 Worker 纯计算

```csharp id="koac8f"
WorkerHandle handle = context.Worker.RunEventJob(
    new EvaluateAiPlanJob(),
    input);
```

Job：

```csharp id="700vcz"
public readonly struct EvaluateAiPlanJob :
    IWorkerEventJob<EvaluateAiPlanInput, AiPlanReadyEvent>
{
    public AiPlanReadyEvent Execute(in EvaluateAiPlanInput input)
    {
        AiPlan plan = AiPlanner.Evaluate(input.Snapshot);

        return new AiPlanReadyEvent(input.ActorId, plan);
    }
}
```

结果：

```text id="2s7m80"
AiPlanReadyEvent 进入 PostScheduler。
由普通 Event 模型继续处理。
```

---

# 15. Analyzer 规则

## 15.1 普通 Query 规则

```text id="8d9wot"
普通 Query 不允许 Bring Event 参数。
普通 Query 返回值必须 void。
普通 Query 不允许调用 Actor 投递 API。
```

这与现有生成器约束一致：无 Bring 时方法必须返回 void。

---

## 15.2 Bring Query 规则

```text id="gjqka7"
带 Bring 的 Query 必须返回 ProjectResult。
Bring 事件参数必须在方法末尾。
Bring 事件参数必须 ref。
BringAttribute 声明几个事件，方法末尾就必须接收几个事件。
```

这些也与当前生成器规则一致。

---

## 15.3 Async ECS 规则

异步模式下 Analyzer 额外检查：

```text id="eyzzfu"
[Query] 方法不能访问 this。
不能访问实例字段。
不能调用实例方法。
不能访问 context。
不能访问 ActorWorld / EcsWorld。
不能访问 Unity / Godot 对象。
捕获数据必须是值对象或只读配置。
```

---

## 15.4 ActorBehaviour 规则

```text id="l3f73z"
ActorBehaviour 只能在 IActor 类中。
方法必须 void。
参数必须 in struct。
ActorBehaviour 不允许访问 ILayerContext。
ActorBehaviour 不允许直接访问 EcsWorld。
```

---

## 15.5 WorkerJob 规则

```text id="etpkrt"
WorkerJob.Execute 不能访问 context。
不能访问 ActorWorld。
不能访问 EcsWorld。
不能访问引擎对象。
Input / Event 必须是 struct。
Job 推荐 readonly struct。
```

---

# 16. 实现阶段

## P1：文档与心智修正

```text id="7x9hcv"
重写 README 核心模型：
  Query = ECS 数据处理
  Bring = ECS -> Actor 通讯边界
  ActorBehaviour = 被动表现
  WorkerEventJob = 纯计算 / IO -> 普通 Event
```

---

## P2：EcsExecutionMode

```text id="o1w5t8"
新增 EcsExecutionMode.Sync / Async。
Runtime 初始化时选择。
构建后不可切换。
```

---

## P3：EcsWorkItem 抽象

```text id="rcp8lj"
PlainEcsQueryWorkItem
BringProjectionWorkItem
EcsResult
ActorEventCommandBuffer
```

---

## P4：异步 Query Flow 底层改造

```text id="7c8gou"
ProjectionQueryFlow / QueryFlow 在 Async 模式下不立即调用 Executor。
改为生成 WorkItem 并入队。
Sync 模式保持现有立即执行。
```

---

## P5：EcsWorker

```text id="nfa36y"
EcsWorker 独占 EcsWorld。
执行 PlainEcsQueryWorkItem。
执行 BringProjectionWorkItem。
Bring Success 时生成 ActorEventCommandBuffer。
```

---

## P6：主线程 Drain

```text id="yoak8i"
Drain EcsResultQueue。
Flush ActorEventCommandBuffer。
接入 ActorScheduler / EventStreamCenter。
```

---

## P7：WorkerEventJob

```text id="i7idbq"
IWorkerEventJob<TInput,TEvent>
WorkerHandle
WorkerEventQueue
WorkerJobFailedEvent
```

---

## P8：Analyzer

```text id="4e0tz0"
Query / Bring 签名规则。
Async ECS 访问规则。
ActorBehaviour 访问规则。
WorkerJob 访问规则。
```

---

## P9：Benchmark

新增 Benchmark：

```text id="k6fu6f"
Sync Plain Query
Async Plain Query submit cost
Async Plain Query execute cost
Async Bring Query submit cost
Async Bring Query ECS execute cost
EcsWorker -> ActorEventCommandBuffer flush cost
WorkerEventJob throughput
```

---

# 17. 最终总结

正确模型是：

```text id="pouhvk"
普通 Query：
  纯 ECS 运算。
  不碰 Actor。

Query + Bring：
  ECS -> Actor 投影通信。
  由 ProjectResult 控制 Fail / Touch / Success。

ActorBehaviour：
  IActor 内的被动表现切片。
  只响应 ActorEvent。

WorkerEventJob：
  纯计算 / IO / 编解码。
  完成后回流普通 Event。

Async ECS：
  不改变 Query API。
  只把底层执行从主线程改为 EcsWorker。
```

最终开发者只需要记住：

```text id="j25o6s"
算 ECS：写 Query。
把 ECS 结果带给 Actor：写 Query + Bring。
表现结果：写 ActorBehaviour。
做纯计算 / IO：写 WorkerEventJob。
```

底层线程、MPSC、队列、合并、异常、分帧回流，都由 LayerBase 承担。
