# LayerBase Scope 迁移执行任务总纲

> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **最高级架构原则：** Layer-first 管理结构，Scope-local 执行资源。  
> **阅读顺序：** 先读 00、01，再按任务编号执行。任何阶段文档与 00、01 冲突时，以 00、01 为准。

---

## 1. 最终架构定义

### 1.1 Layer 是上层业务管理结构

`LayersBuilder.Push(Layer)` 继续决定：

```text
Layer 层级
LayerIndex
Layer 顺序
Service / Context 的业务归属
DI / Mount / Provide / From 的范围
Event Handler 的 Layer 顺序
Tool 的管理范围
Lifecycle 的管理顺序
```

```csharp
using LayerRuntime runtime = LayerHub.CreateLayers()
    .Push(new FoundationLayer())
    .Push(new GameplayLayer())
    .Push(new PresentationLayer())
    .Build();
```

Layer 不能被降级为：

```text
Scope 中的普通标签
只对 MainScope 有意义的顺序
诊断字段
可被 ServiceSlot / ObjectSlot 替代的索引
```

所有业务对象必须同时具有：

```text
OwnerLayer
OwnerScope
```

其中：

```text
OwnerLayer：
    决定管理范围和层级语义。

OwnerScope：
    决定实例、线程和本地执行资源。
```

### 1.2 Scope 是 Layer 之下的执行维度

Scope 负责：

```text
Owner Thread / TickRate
EventCenter
PostScheduler
Timer / Delay
EcsWorld / EcsScheduler
LBTask SynchronizationContext
ScopeLocalCallRegistry
ScopeEventInbox
ScopeCallInbox
Fault / Diagnostics 本地状态
FixedUpdate accumulator
```

Scope 不负责重新定义：

```text
Service 属于哪个 Layer
DI 是否可以跨 Layer
Handler 的 Layer 顺序
Lifecycle 的管理顺序
Tool 属于哪个 Layer
```

### 1.3 Build 是 Layer-first，Running 是 Scope-local

```text
LayersBuilder.Push
    → LayerBuildPlan[]
        → 每个 Layer 在不同 Scope 的 Contribution
    → 投影 ScopeExecutionPlan[]
        → ScopeLayerSlice[]
        → ProviderSlot / ObjectSlot / Range / Invoker
```

运行期允许使用：

```text
连续数组
Slot
Offset
Count
BitSet
生成式 Factory / Invoker
```

运行期不要求重建完整 Layer 对象树。

但扁平化只能优化物理布局，不能改变业务所有权。

---

## 2. 最终对象关系

```text
LayerRuntime
    ├── RuntimeCompositionPlan
    │   ├── LayerBuildPlan[]
    │   └── ScopeExecutionPlan[]
    │
    ├── ScopeRuntimeHost
    │   ├── MainScopeRuntime
    │   ├── InlineScopeRuntime[]
    │   └── WorkerScopeRuntime[]
    │
    ├── MainActorRuntime
    │   └── ActorWorld
    │
    └── WorkerJobScheduler
```

每个 `ScopeRuntime`：

```text
ScopeRuntime
    ├── ScopeTransport
    │   ├── ScopeEventInbox
    │   └── ScopeCallInbox
    │
    ├── EventCenter
    ├── PostScheduler
    ├── Timer / Delay
    ├── EcsWorld / EcsScheduler
    ├── SynchronizationContext
    ├── ScopeLocalCallRegistry
    ├── LayerProviderRuntime[LayerIndex]
    ├── ScopeLayerSlice[]
    └── ScopeLifecyclePlan
```

这里的 `LayerProviderRuntime[]` 和 `ScopeLayerSlice[]` 是：

```text
由 LayerBuildPlan 投影得到的执行视图
```

不是：

```text
Scope 对所有 Layer 的业务所有权
```

---

## 3. Scope 间唯一通讯方式

Scope 间只允许：

```text
ScopeEvent MPSC
ScopeCall MPSC
```

### 3.1 ScopeEvent

用于：

```text
单向业务消息
WorkerEventJob Result
Actor Projection Command / Result
Fault 上报
不要求响应的内部通知
```

### 3.2 ScopeCall

用于：

```text
跨 Scope 请求/响应
Activate
Stop
Dispose
Snap SafePoint
需要结果的内部控制命令
```

禁止新增：

```text
ControlQueue
CompletionQueue
StopQueue
DisposeQueue
ProjectionQueue
ActorCommandQueue
WorkerResultQueue
```

线程信号只负责唤醒消费者，不承载业务或生命周期语义。

---

## 4. Event、DI 与 Call 的范围

### 4.1 EventCenter

```text
每 Scope 一个 EventCenter
Handler 按 Push LayerIndex 执行 faster 原注册流程
注册完成后不保留第二份 Handler Registry
```

EventCenter 保留原：

```text
Notify
Subscribe
Flow
Async
手动订阅
Unsubscribe
Circuit
Prewarm
Freeze
Bucket / Fast Cache
```

`SubscribeParallel` 直接删除。

### 4.2 DI / Mount / Provide / From

```text
Scope：
    隔离实例和线程。

Layer：
    限制直接依赖范围。
```

同 Scope、同 Layer：

```text
允许 DI / Mount / Provide / From
```

同 Scope、不同 Layer：

```text
禁止直接取得 Service
必须使用 this.Call<TRequest,TResponse>()
```

不同 Scope：

```text
使用 ScopeEvent / ScopeCall
```

`From` 必须显式指定来源 Service：

```csharp
[From(
    typeof(CombatService),
    "Combat.Registry")]
private CombatRegistry _registry = null!;
```

### 4.3 本地 Call

本地 Call 是当前 Scope 内的跨 Layer通讯方案：

```csharp
InventoryResult result =
    await this.Call<
        InventoryRequest,
        InventoryResult>(
            in request);
```

寻址范围：

```text
Current Scope
+ RequestType
+ ResponseType
```

不包含 Layer。

同一 Scope 内相同 Request/Response 只能有一个 Handler。

跨 Scope Call 是 03 号文档中的独立实现：

```csharp
PathResult result =
    await this.Scope<PathfindingScope>()
        .Call<
            FindPathRequest,
            PathResult>(
                in request);
```

---

## 5. Post 与 Worker 并行边界

删除：

```text
PostFromAnyThread
TryPostFromAnyThread
SubscribeParallel
```

跨线程或跨 Scope 投递必须使用明确 Endpoint：

```csharp
ScopeRef<PathfindingScope> path =
    runtime.Scope<PathfindingScope>();

path.TryPost(
    in command);
```

WorkerEventJob：

```text
OwnerScope
    → WorkerJobScheduler
    → 纯计算
    → Result ScopeEvent
    → OriginScope EventInbox
    → OriginScope Owner Thread
```

Worker 结果不直接写 PostScheduler，也不使用全局 ResultQueue。

---

## 6. ActorWorld 固定边界

`ActorWorld` 不属于通用 `ScopeRuntime`。

```text
LayerRuntime
    → MainActorRuntime
        → ActorWorld
```

只有 MainScope Owner Thread 可以：

```text
创建 Actor
Enable / Disable / Release Actor
向 Actor Mailbox 写消息
推进 ActorWorld
```

CustomScope：

```text
ActorHandle + DTO
    → ScopeEvent / ScopeCall<MainScope>
```

CustomScope 的 EcsWorld 不持有 ActorWorld。

Projection 的具体实现由 22 号文档负责。

---

## 7. 生命周期与 Tick

每个 Scope 都保留完整 Push Layer 顺序：

```text
LayerIndex 0
LayerIndex 1
LayerIndex 2
```

CustomScope 不能“没有 Layer”。

空 Layer 使用零长度 Slice，不创建无意义对象。

正向阶段：

```text
Initialize
PostBuild
RuntimeStart
FixedUpdate
Update
```

按 LayerIndex 正序。

逆向阶段：

```text
RuntimeStop
Dispose
```

按 LayerIndex 逆序。

Scope Owner Thread 执行当前 Scope 的轻量生命周期数组，不遍历 Layer 对象树。

---

## 8. 最终业务 API 心智模型

```csharp
public sealed partial class CombatService :
    IService
{
    public void Damage(
        in DamageEvent value)
    {
        this.Send(
            in value);
    }

    public void DamageLater(
        in DamageEvent value)
    {
        this.Post(
            in value);
    }

    public async LBTask<InventoryResult>
        QueryInventory(
            in InventoryRequest request)
    {
        return await this.Call<
            InventoryRequest,
            InventoryResult>(
                in request);
    }

    public LBTask<PathResult>
        FindPath(
            in FindPathRequest request)
    {
        return this.Scope<PathfindingScope>()
            .Call<
                FindPathRequest,
                PathResult>(
                    in request);
    }

    public WorkerHandle CalculateScore(
        in ScoreInput input)
    {
        return this.WorkerJobs()
            .RunEventJob<
                ScoreJob,
                ScoreInput,
                ScoreCompletedEvent>(
                    new ScoreJob(),
                    in input);
    }
}
```

路由规则：

```text
Send / Post / Timer / Delay / ECS：
    OwnerScope

DI / Mount / Provide / From / Tool：
    OwnerScope + OwnerLayer

this.Call：
    Current Scope

Scope<T>().Call / TryPost：
    Explicit TargetScope

Actor：
    MainScope Direct
    CustomScope → MainScope ScopeEvent/ScopeCall
```

---

## 9. Build、Activate、Running、Stop

### Build

```text
Push Layer
    → LayerIndex
    → 收集 Layer Service / Context / Handler / Tool / Lifecycle
    → 解析 OwnerScope
    → 生成 LayerBuildPlan[]
    → 投影 ScopeExecutionPlan[]
    → 分配 Slot / Offset / Count / RouteId
    → Freeze
```

### Activate

```text
Scope Owner Thread
    → 创建本地资源
    → 按 LayerIndex 创建 LayerProvider
    → 创建 Service / Context
    → Mount
    → Provide / From
    → Event Handler 注册
    → LocalCall Handler 绑定
    → Initialize / PostBuild / Prewarm / RuntimeStart
```

### Running

```text
只使用数组、Slot、Range、Invoker
OwnerScope 本地资源无锁
跨 Scope 只写两条 MPSC
```

### Stop / Dispose

```text
MainScope 通过 ScopeCall 控制 CustomScope
目标 Owner Thread 执行 Stop / Dispose
MainScope 最后停止
ActorWorld 最后释放
```

---

## 10. faster 复用总原则

任何任务实施前必须先定位 `faster` 的：

```text
现有类型
算法
对象池
RingQueue
Handle
Invoker
生成器
测试
Benchmark
```

复用分类：

```text
直接复用：
    所有权与最新架构一致。

修改复用：
    算法可用，只替换 Owner / Route / Lifecycle。

仅参考：
    行为和测试有价值，对象关系错误。

禁止移植：
    与被否定架构强绑定。
```

每个提交必须说明：

```text
faster 文件 / 类型
复用方式
保留的算法和快路径
修改的所有权和路由
未复用原因
复用的测试与 Benchmark
```

禁止因为旧宿主错误而重写成熟算法。

---

## 11. 阶段文档关系

| 编号 | 阶段目的 |
|---:|---|
| 01 | 强制架构、AOT、性能和代码门禁 |
| 02 | Scope 本地运行内核与资源 |
| 03 | ScopeEvent / ScopeCall 跨域协议 |
| 04 | Scope 生命周期控制 |
| 05 | Layer-first Build 与 Scope 执行投影 |
| 06 | AssemblyModule Contribution |
| 07 | LBTask OwnerScope Context |
| 08 | Scope 隔离、Layer 范围 DI |
| 09 | Provide / From 来源 Service 绑定 |
| 10 | LayerHub、Runtime 与 OwnerScope API |
| 11 | `[Input]` 与 Scope ECS Query |
| 12 | WorkerEventJob 与 SubscribeParallel 删除 |
| 13 | 同 Scope、同 Layer Mount |
| 14 | Layer 管理下的 Scope Tool 实例 |
| 15 | Scope Fault / Circuit |
| 16 | 静态 Metadata 与 Runtime 隔离 |
| 17 | EventCenter 迁移为 Scope 本地资源 |
| 18 | Scope Post / Timer / Delay |
| 19 | Layer 管理下的 Scope 生命周期和 Tick |
| 20 | Scope 本地 Call |
| 21 | MainScope ActorWorld |
| 22 | ECS Projection 固定 Actor 管线 |

后续 ECS、Snap、拓扑、Prewarm、Diagnostics 和总测试文档必须同样遵守本总纲。

---

## 12. 推荐实施顺序

```text
第一阶段：冻结总架构
    01 → 00 → 05 → 02 → 19

第二阶段：跨 Scope 内核
    03 → 04 → 07 → 15

第三阶段：Layer 管理下的对象系统
    08 → 13 → 09 → 14 → 10

第四阶段：事件、时间与 Call
    17 → 18 → 20 → 12

第五阶段：ECS 与 Actor
    11 → ECS Scheduler 文档
    21 → 22

第六阶段：工程能力
    Snap → Audit → Prewarm → Diagnostics → Tests
```

---

## 13. 总体验收否决项

出现以下任意一项，迁移不通过：

```text
Scope 被定义为 Service / Context 的业务管理根

CustomScope 被描述为没有 Layer

Service / Context 只有 OwnerScope，没有 OwnerLayer

DI 可以在同 Scope 跨 Layer解析

From 不声明来源 Service

EventCenter 使用 ServiceSlot / ContextSlot 代替 LayerIndex

本地 Call 重新以 Layer 寻址

跨 Scope 出现 ScopeEvent / ScopeCall 之外的消息队列

ScopeRuntime 持有 ActorWorld

存在 PostFromAnyThread

存在 SubscribeParallel

运行期扫描 Assembly 或编译 SyntaxTree

热路径使用 Dictionary / List / LINQ 做路由

Build 可以确定的下标在 Running 重新计算

没有检查 faster 就重新实现已有算法
```
