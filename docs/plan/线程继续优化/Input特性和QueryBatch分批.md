# LayerBase ECS：`[Input]` 参数特性与 QueryBatch 隐式分批方案

## 0. 总体定位

LayerBase ECS 不应该扩张成一个通用业务调度系统，而应该定位为：

```text
纯数据计算内核。
```

它的职责是：

```text
1. 批量处理 ECS 组件数据。
2. 通过 Source Generator 生成无反射 Query 调用链。
3. 通过 Worker 单线程保证 ECS 数据访问安全。
4. 通过 QueryBatch 自动分批控制 working set 和事件回流压力。
5. 通过 [Input] 接收业务层提交的只读输入参数。
6. 通过 ActorEventBatch / CommandBatch 输出结果。
```

它不应该直接访问：

```text
Layer
Service
Manager
Actor
LayerRuntime
业务上下文
外部全局状态
```

业务层和 ECS 的关系应该是：

```text
业务层 -> 输入 ECS
ECS -> 输出事件 / 命令 / 结果
业务层 -> 消费 ECS 输出
```

而不是：

```text
ECS -> 直接访问业务层
ECS -> 直接调用 Service
ECS -> 直接修改 Actor
```

---

# 1. `[Query]` 的基本生成模型

开发者写内部实现方法：

```csharp
[Query]
public static ProjectResult OnMove(
    ref Position position,
    in Velocity velocity,
    [Input] FrameTimeInput time)
{
    position.Value += velocity.Value * time.DeltaTime;
    return ProjectResult.Keep;
}
```

Source Generator 生成一个去掉 `On` 前缀的外部入口：

```csharp
public static void Move(FrameTimeInput time)
{
    if (__Runtime is null)
    {
        throw new global::System.InvalidOperationException(
            "Generated ECS queries are not registered.");
    }

    var job = default(__MoveJob);
    var input = new __MoveInput(time);

    __Runtime.EcsScheduler
        .SubmitQuery<
            __MoveJob,
            __MoveInput,
            Position,
            Velocity>(
            __MoveQueryId,
            flags: 0,
            in job,
            input);
}
```

生成的外部方法只暴露 `[Input]` 参数，不暴露 ECS 组件参数。

也就是说：

```text
OnMove 是内部实现。
Move 是外部调用入口。
组件参数由 ECS 查询系统填充。
Input 参数由外部调用者传入。
```

---

# 2. `[Input]` 特性设计

## 2.1 `[Input]` 不需要泛型

最终采用非泛型版本：

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class InputAttribute : Attribute
{
}
```

原因：

```text
参数类型本身已经包含输入类型信息。
```

所以不需要：

```csharp
[Input<FrameTimeInput>] FrameTimeInput time
```

而是：

```csharp
[Input] FrameTimeInput time
```

Source Generator 可以直接从参数符号读取：

```text
参数名：time
参数类型：FrameTimeInput
参数语义：[Input]
```

---

## 2.2 `[Input]` 的语义

`[Input]` 表示：

```text
这个参数不是 ECS 组件；
不是 Entity；
不是 Bring；
不是 Service；
不是 Runtime；
而是外部生成 Query 方法需要接收的只读输入参数。
```

其链路是：

```text
[Query] OnMove(...)
  -> Generator 识别 [Input] 参数
  -> 生成 Move(input...)
  -> Move(...) 创建 __MoveInput
  -> SubmitQuery(..., input)
  -> Worker 执行 __MoveJob
  -> __MoveJob.Execute(..., input)
  -> 调用 OnMove(..., input.Time)
```

---

## 2.3 `[Input]` 是 Submit 级输入

`[Input]` 不代表全局输入帧，而代表：

```text
本次 Query 调用提交时传入的一份只读参数包。
```

例如：

```csharp
Move(new FrameTimeInput(deltaTime));
```

表示：

```text
本次 Move Query 执行期间，所有实体都使用这份 FrameTimeInput。
```

下一次调用：

```csharp
Move(new FrameTimeInput(otherDeltaTime));
```

则是另一份输入。

---

# 3. Input 类型约束

为了保持 ECS 纯净和线程安全，`[Input]` 参数类型应该满足：

```text
1. 必须是 readonly struct。
2. 不允许是 ref / out 参数。
3. 不建议使用 class。
4. 不允许携带可变集合引用。
5. 如果需要传大数据集，应传只读句柄或只读 Blob。
```

推荐：

```csharp
public readonly struct FrameTimeInput
{
    public readonly float DeltaTime;
    public readonly float TimeScale;

    public FrameTimeInput(float deltaTime, float timeScale)
    {
        DeltaTime = deltaTime;
        TimeScale = timeScale;
    }
}
```

对于大型只读数据，可以使用：

```csharp
public readonly struct FlowFieldInput
{
    public readonly EcsBlobHandle<FlowFieldBlob> Blob;
}
```

不要直接传：

```csharp
public sealed class FlowFieldInput
{
    public float[] MutableData;
}
```

否则 Query 可能读到外部正在修改的数据。

---

# 4. 生成的 InputPack

每个包含 `[Input]` 参数的 Query 都会生成一个内部 InputPack。

例如：

```csharp
[Query]
public static ProjectResult OnMove(
    ref Position position,
    in Velocity velocity,
    [Input] FrameTimeInput time)
{
    return ProjectResult.Keep;
}
```

生成：

```csharp
private readonly struct __MoveInput
{
    public readonly FrameTimeInput Time;

    public __MoveInput(FrameTimeInput time)
    {
        Time = time;
    }
}
```

Job：

```csharp
private readonly struct __MoveJob :
    IProjectionJob2<Position, Velocity, __MoveInput>
{
    public ProjectResult Execute(
        Entity entity,
        ref Position c0,
        in Velocity c1,
        in __MoveInput input)
    {
        return OnMove(
            ref c0,
            in c1,
            input.Time);
    }
}
```

---

# 5. 多个 `[Input]` 参数

开发者写：

```csharp
[Query]
public static void OnApplyWind(
    ref Velocity velocity,
    [Input] FrameTimeInput time,
    [Input] WindInput wind)
{
    velocity.Value +=
        wind.Direction *
        wind.Strength *
        time.DeltaTime;
}
```

生成外部入口：

```csharp
public static void ApplyWind(
    FrameTimeInput time,
    WindInput wind)
{
    var job = default(__ApplyWindJob);
    var input = new __ApplyWindInput(time, wind);

    __Runtime.EcsScheduler
        .SubmitQuery<
            __ApplyWindJob,
            __ApplyWindInput,
            Velocity>(
            __ApplyWindQueryId,
            flags: 0,
            in job,
            input);
}
```

生成 InputPack：

```csharp
private readonly struct __ApplyWindInput
{
    public readonly FrameTimeInput Time;
    public readonly WindInput Wind;

    public __ApplyWindInput(
        FrameTimeInput time,
        WindInput wind)
    {
        Time = time;
        Wind = wind;
    }
}
```

---

# 6. `[Input]` 和 `[Bring]` 的关系

两者都是 Query 参数语义特性。

```text
[Bring]
  表示该参数由 Bring / Projection / Actor 映射链提供。

[Input]
  表示该参数由外部生成 Query 方法调用者提供。
```

示例：

```csharp
[Query]
public static ProjectResult OnMove(
    ref Position position,
    in Velocity velocity,
    [Bring] MoveViewEvent viewEvent,
    [Input] FrameTimeInput time)
{
    return ProjectResult.Keep;
}
```

参数分类：

```text
组件参数：
  ref Position
  in Velocity

Bring 参数：
  [Bring] MoveViewEvent viewEvent

Input 参数：
  [Input] FrameTimeInput time
```

生成外部入口只暴露 Input：

```csharp
public static void Move(FrameTimeInput time)
{
}
```

Bring 参数仍然由 Runtime / Projection / Scheduler 内部解析。

---

# 7. `[Input]` 的 Analyzer 规则

建议加入以下诊断规则。

## ECSINPUT001：`[Input]` 只能用于 `[Query]` 方法参数

错误：

```csharp
public static void Foo([Input] FrameTimeInput time)
{
}
```

如果 `Foo` 不是 `[Query]` 方法，则报错。

---

## ECSINPUT002：`[Input]` 参数不能是 `ref` / `out`

允许：

```csharp
[Input] FrameTimeInput time
```

可选允许：

```csharp
[Input] in FrameTimeInput time
```

不允许：

```csharp
[Input] ref FrameTimeInput time
```

不允许：

```csharp
[Input] out FrameTimeInput time
```

---

## ECSINPUT003：`[Input]` 参数类型必须是 readonly struct

推荐：

```csharp
public readonly struct FrameTimeInput
{
}
```

如果不是 readonly struct，报错或警告。

---

## ECSINPUT004：`[Input]` 参数不会参与 ECS 组件匹配

即：

```text
[Input] 参数不是组件；
不会要求实体拥有这个类型的组件；
不会参与 Archetype Query。
```

---

## ECSINPUT005：所有 `[Input]` 参数会被提升到生成方法签名

例如：

```csharp
[Query]
public static void OnMove(
    ref Position position,
    [Input] FrameTimeInput time)
{
}
```

生成：

```csharp
public static void Move(FrameTimeInput time)
{
}
```

---

# 8. QueryBatch 隐式分批方案

## 8.1 最终选择：Worker 单线程 + 自动分批

当前阶段不引入多线程 QueryBatch。

核心模型：

```text
Main Thread
  -> 提交 QueryRequest

EcsWorker 单线程
  -> 接收 QueryRequest
  -> 构建 QueryPlan
  -> 隐式拆成 QueryBatch
  -> 顺序执行 Batch 0
  -> 顺序执行 Batch 1
  -> 顺序执行 Batch 2
  -> 每个 Batch 独立投递 ActorEventBatch / CommandBatch
  -> 执行下一个 Query
```

ECS World 的组件存储只由 EcsWorker 线程读写。

因此第一版不需要：

```text
ReadSet / WriteSet 调度
BatchParallelSafety
ShardLocalSafe
Shard Lane Scheduler
QueryBarrier
跨 Worker 数据竞争检测
```

因为没有多个线程同时读写 ECS 组件数组。

---

## 8.2 QueryBatch 的目的

在单线程 Worker 模型下，QueryBatch 的目的不是并行，而是：

```text
1. 控制每次执行的 working set 大小。
2. 降低大 Query 对 cache 的压力。
3. 避免一次 Query 产生超大 ActorEventBuffer。
4. 避免一次 Query 产生超大 CommandBuffer。
5. 让 Actor 事件可以按 Batch 分段回流。
6. 让后续帧预算调度更稳定。
7. 为未来多线程版本保留底层结构。
```

---

## 8.3 QueryBatch 配置放在 Build 阶段

统一配置，不让每个 Query 单独配置。

```csharp
LayerRuntime runtime = LayerHub.CreateLayers()
    .UseEcs(options =>
    {
        options.QueryBatch.DefaultBatchLimitBytes = 512 * 1024;
        options.QueryBatch.EnableImplicitBatching = true;
        options.QueryBatch.MinBatchEntityCount = 256;
        options.QueryBatch.MaxBatchEntityCount = 32768;
    })
    .Build();
```

所有 Query 默认使用同一个 Options。

---

## 8.4 默认 Batch 上限

默认：

```text
DefaultBatchLimitBytes = 512KB
```

含义：

```text
每个 QueryBatch 的目标 working set 尽量控制在 512KB 左右。
```

但这不是硬性精确值，而是调度器用于估算 batch entity count 的目标值。

---

## 8.5 Batch 大小计算

对于一个 Query：

```csharp
[Query]
public static void OnMove(
    ref Position position,
    in Velocity velocity,
    [Input] FrameTimeInput time)
{
}
```

假设：

```text
Position = 12B
Velocity = 12B
```

每实体组件访问大小：

```text
AccessBytesPerEntity = 12 + 12 = 24B
```

Input 不按实体重复计算，因为它是整个 Query 共用的一份输入包。

Batch entity count：

```text
BatchEntityCount =
  DefaultBatchLimitBytes / AccessBytesPerEntity
```

例如：

```text
512KB / 24B ≈ 21845 entities
```

再经过 clamp：

```csharp
batchEntityCount = Math.Clamp(
    batchEntityCount,
    options.MinBatchEntityCount,
    options.MaxBatchEntityCount);
```

---

# 9. QueryBatch 执行流程

生成方法：

```csharp
public static void Move(FrameTimeInput time)
{
    var job = default(__MoveJob);
    var input = new __MoveInput(time);

    __Runtime.EcsScheduler.SubmitQuery<
        __MoveJob,
        __MoveInput,
        Position,
        Velocity>(
        __MoveQueryId,
        flags: 0,
        in job,
        input);
}
```

Scheduler 内部：

```csharp
public void SubmitQuery<TJob, TInput, T0, T1>(
    int queryId,
    int flags,
    in TJob job,
    TInput input)
{
    var request = new EcsQueryRequest<TJob, TInput, T0, T1>(
        queryId,
        flags,
        job,
        input);

    _worker.Enqueue(request);
}
```

Worker 执行：

```csharp
private void ExecuteQuery(EcsQueryRequest request)
{
    EcsQueryPlan plan = _planner.Build(request);

    foreach (EcsQueryBatch batch in plan.Batches)
    {
        EcsQueryBatchContext context =
            _contextPool.Rent(batch);

        try
        {
            _executor.Execute(batch, context);

            if (context.ActorEvents.Count > 0)
            {
                _actorWorld.Enqueue(context.ActorEvents.Detach());
            }

            if (context.Commands.Count > 0)
            {
                _world.EnqueueCommandBatch(context.Commands.Detach());
            }
        }
        finally
        {
            _contextPool.Return(context);
        }
    }
}
```

---

# 10. ActorEventBatch 独立回流

每个 QueryBatch 都有自己的 ActorEventBatch。

```text
Batch 0 -> ActorEventBatch 0
Batch 1 -> ActorEventBatch 1
Batch 2 -> ActorEventBatch 2
```

每个 Batch 执行完后立即投递：

```csharp
if (context.ActorEvents.Count > 0)
{
    actorWorld.Enqueue(context.ActorEvents.Detach());
}
```

不等待整个 Query 全部完成后再合并。

好处：

```text
1. 分摊 Actor 事件压力。
2. 避免一次性灌入大量事件。
3. ActorWorld 可以按帧预算逐批消费。
4. Batch-local 输出保持线程和状态隔离。
5. 后续如果要多线程，也不需要重做事件回流模型。
```

---

# 11. CommandBatch 处理

即使 Worker 是单线程，也不建议 Query 遍历过程中直接结构变更。

不建议：

```csharp
world.Destroy(entity);
world.Add<DeadTag>(entity);
```

推荐：

```text
QueryBatch 生成 CommandBatch；
Worker 在安全点 Apply。
```

可以选择：

```text
1. 每个 Batch 执行后提交 CommandBatch；
2. 一个 Query 所有 Batch 完成后统一 Apply；
3. 每帧统一 Apply。
```

第一版建议：

```text
Query 遍历中不直接结构变更；
CommandBatch 延迟到 Query 安全点 Apply。
```

---

# 12. ECS 与业务层的关系

业务层不直接被 ECS 访问。

业务层通过生成 Query 方法传入 Input：

```csharp
Move(new FrameTimeInput(deltaTime, timeScale));
ApplyWind(new FrameTimeInput(deltaTime, timeScale), windInput);
```

ECS 输出：

```text
ActorEventBatch
CommandBatch
ResultBatch
```

业务层消费输出。

最终流向：

```text
Layer / Service / Actor
  -> 调用生成 Query 方法并传入 [Input]
  -> EcsWorker 执行 ECS Query
  -> Query 产生 ActorEventBatch / CommandBatch
  -> ActorWorld / 主线程按预算消费
```

---

# 13. 普通 Query 与复杂多实体交互

普通 `[Query]` 默认适合：

```text
移动
冷却
Buff Tick
生命恢复
状态衰减
子弹飞行
局部数值刷新
```

它不应该随便访问其他实体。

如果要做碰撞、AOI、空间索引、范围查询，应通过 ECS 内置 Kernel，而不是让普通 Query 拿 World 随便查。

推荐分层：

```text
Entity-local Query
  当前实体局部计算。

Spatial Kernel
  碰撞、AOI、空间分区、范围候选。

Reduce Kernel
  多结果聚合，例如多个子弹命中同一目标。

Apply / Event 回流
  把结果安全地应用到 Actor 或 ECS。
```

这样普通 Query 保持纯净，但 ECS 仍然可以支持大量实体交互。

---

# 14. 当前阶段不做的内容

第一版不做：

```text
1. 多线程 QueryBatch。
2. Shard Lane Scheduler。
3. ReadSet / WriteSet 冲突调度。
4. QueryBarrier。
5. BatchParallelSafety。
6. ShardLocalSafe。
7. 全局 EcsInputFrame。
8. Query 直接访问 Layer / Service / Actor。
```

这些可以留作后续扩展。

尤其是多线程 QueryBatch，不应该成为第一版复杂度。

---

# 15. 推荐落地顺序

## Phase 1：`[Input]` 参数特性

实现：

```text
InputAttribute
Analyzer
Generator 参数分类
生成外部 Query 方法签名
生成 __QueryInput
SubmitQuery 支持 TInput
Job.Execute 支持 TInput
```

验收：

```csharp
[Query]
public static void OnMove(
    ref Position position,
    in Velocity velocity,
    [Input] FrameTimeInput time)
{
}
```

能生成：

```csharp
public static void Move(FrameTimeInput time)
{
}
```

并能把 `time` 传入 `OnMove`。

---

## Phase 2：Build 期 QueryBatchOptions

实现：

```text
EcsQueryBatchOptions
UseEcs(options => ...)
DefaultBatchLimitBytes = 512KB
MinBatchEntityCount
MaxBatchEntityCount
EnableImplicitBatching
```

---

## Phase 3：Worker 单线程隐式分批

实现：

```text
EcsQueryPlan
EcsQueryBatch
EcsQueryBatchPlanner
按 AccessBytesPerEntity 计算 BatchEntityCount
Worker 顺序执行所有 Batch
```

---

## Phase 4：Batch-local 输出

实现：

```text
EcsQueryBatchContext
ActorEventBatch
CommandBatch
DiagnosticsBatch
每 Batch 独立投递 ActorEventBatch
CommandBatch 延迟安全点 Apply
```

---

## Phase 5：Diagnostics

输出：

```text
QueryName
InputTypes
AccessBytesPerEntity
BatchLimitBytes
BatchCount
EntitiesPerBatch
ActorEventsPerBatch
CommandsPerBatch
TotalQueryTime
BatchTime
```

---

# 16. 最终总结

最终设计可以浓缩成一句话：

```text
[Input] 负责把业务层的只读输入参数提升到生成 Query 方法签名中；
QueryBatch 负责在 Worker 单线程内部隐式分批执行大 Query；
每个 Batch 独立回流 ActorEvent / Command；
ECS 不直接访问 LayerBase 业务模型，而是通过 Input 输入、通过事件输出。
```

最终心智模型：

```text
[Query] OnXxx(...)
  是内部 ECS 实现。

Xxx(input...)
  是生成器生成的外部调用入口。

[Input]
  是外部调用入口参数。

QueryBatch
  是内部执行切片。

ActorEventBatch
  是内部回流切片。

EcsWorker
  是 ECS World 的唯一执行线程。
```
