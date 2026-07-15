# 22 ECS→Actor Projection 与 ActorWorld 固定通讯管线迁移

> **强制执行规范：** 本文必须遵守 `01_mandatory_architecture_aot_performance_standards.md`；冲突时以该规范为准。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`  
> **依赖阶段：** `02_scope_runtime_resources.md`、`03_scope_event_call_protocol.md`  
> **文档性质：** 独立阶段任务。本文只补全 02 号文档中“ActorWorld 固定通讯管线”的 Projection 实现，不重新设计 ActorWorld、ECS、ScopeEvent 或 ProjectedActor 的原有业务语义。

---

## 0. 本阶段核心目的

必须落实以下固定边界：

```text
Entity OwnerScope：
    拥有 EcsWorld
    拥有 ProjectedActorMeta / ProjectedActorRef
    判断 Ensure、Touch、Disable、Release 意图
    不持有 ActorWorld
    不直接调用 ActorWorld

MainScope：
    ActorWorld 唯一写入者
    ActorWorld 唯一推进者
    处理所有 CustomScope 发来的 Actor 命令

跨 Scope：
    只使用现有 ScopeEvent MPSC
    不建立 ProjectionChannel
    不建立 ActorCommandQueue
    不建立 ProjectionResultQueue
```

最终数据流：

```text
CustomScope EcsWorld
    → Projection Query / Sweep
    → ActorProjectionCommandBatchScopeEvent
    → MainScope.ScopeEventInbox
    → MainScopeActorProjectionDispatcher
    → ActorWorld
    → ActorProjectionResultBatchScopeEvent
    → OriginScope.ScopeEventInbox
    → OriginScope EcsWorld Binding Apply
```

MainScope 本地 ECS 已经运行在 ActorWorld Owner Thread，可使用同一 Dispatcher 的本地直达方法，不进入 MPSC。

---

## 1. 本阶段不修改的核心语义

以下 `faster` 现有语义必须保留：

```text
ProjectedActorRef：
    ActorId
    ActorTypeId
    KeepAliveTicks
    ExpireAtTicks
    TouchIntervalTicks
    NextTouchTicks
    RetirePolicy
    CreatePolicy
    ReleasePolicy

ProjectedActorMeta：
    ActorId
    ActorTypeId
    ActiveListIndex
    ProjectedActorState
    EnsurePending
    EnablePending

ProjectedActorState：
    None
    Projectable
    Active
    DisablePending
    Disabled
    ReleasePending
    Released
```

继续保留：

```text
Ensure
Touch 节流
KeepAlive 到期
Disable 后保留绑定
再次 Touch 后 Enable
ReturnToPool
DestroyImmediately
DetachAndLetActorFinish
ActiveProjectedActorList 预算化 Sweep
ActorId / Entity Version 失效检查
ProjectionBatchBuffer 的 ArrayPool 和批量 Post 热路径
```

本文只替换：

```text
CustomScope 或非 Actor Owner Thread直接访问 ActorWorld
EcsResultQueue 中转 Projection Result
LayerRuntime ActorEventInbox / ActorLifecycleInbox 旁路
Action<ActorWorld> 闭包批量投递
```

---

## 2. 最终公有 API

现有 Projection 高层 API保持，不增加 Scope 专用版本。

业务仍然表达：

```csharp
ProjectedActorRef actor =
    projection.Ensure<MyActor>(
        entity);

projection.Touch(entity);
projection.Disable(entity);
projection.Release(entity);
```

Actor Event 批量投递仍使用现有 Projection/Query API：

```csharp
projection.PostBatch(
    in damageEvent);
```

具体扩展方法名称以 `faster` 现有 API为准。

不得新增：

```text
projection.EnsureInMainScope(...)
projection.GetActorWorld()
scope.ActorWorld
scope.ProjectionChannel
ScopeRef.GetActor(...)
```

Scope 差异只存在于内部 `IProjectedActorCommandSink` 和标准 ScopeEvent Route。

---

## 3. 02 号文档中 Actor 命令的具体映射

02 号文档中的：

```text
ActorEnsureScopeEvent
ActorEnableScopeEvent
ActorDisableScopeEvent
ActorReleaseScopeEvent
ActorTouchScopeEvent
ActorPostBatchScopeEvent<TEvent>
```

是业务命令语义，不需要创建六条队列，也不需要为每个命令创建一套 Transport。

实现收敛为三种标准 ScopeEvent Payload：

```text
ActorProjectionCommandBatchScopeEvent
    Ensure / Enable / Disable / Release

ActorProjectionResultBatchScopeEvent
    MainScope → OriginScope 的执行结果

ActorPostBatchScopeEvent<TEvent>
    批量向已有 ActorId Post Event
```

### 3.1 Touch 不创建独立物理消息

`Touch` 的期限和节流属于 Entity OwnerScope：

```text
Actor Active：
    只刷新 ProjectedActorRef.ExpireAtTicks
    只刷新 NextTouchTicks
    不向 MainScope 发消息

ActorId 无效：
    转换为 Ensure Command

Actor Disabled / DisablePending：
    转换为 Enable Command

Actor EnablePending：
    不重复提交
```

因此不实现独立的 `ActorTouchScopeEvent` 类型。

这不是删除 Touch 功能，而是避免发送不需要 ActorWorld 参与的冗余消息。

---

## 4. 固定 ScopeEvent Route

Build 阶段为以下内部事件生成稳定 RouteId：

```text
InternalActorProjectionCommandBatchRoute
InternalActorProjectionResultBatchRoute
InternalActorPostBatchRoute<TEvent>
```

它们使用既有：

```text
ScopeEndpoint
ScopeEventEnvelope
ScopeEventInbox
ScopePostDispatchGenerator / ScopeEvent Dispatcher
```

不得增加：

```text
Projection RouteTable
Actor 专用 Endpoint
Actor 专用 MPSC
Projection Dispatcher Thread
```

### 4.1 MainScope Dispatcher

MainScope 的标准 ScopeEvent Dispatcher 遇到内部 Route 后，调用：

```csharp
internal sealed class MainScopeActorProjectionDispatcher
{
    private readonly ActorWorld _actors;

    internal void Apply(
        in ActorProjectionCommandBatchScopeEvent value);

    internal void PostBatch<TEvent>(
        in ActorPostBatchScopeEvent<TEvent> value)
        where TEvent : struct;
}
```

如果不需要独立对象，可将同样的方法直接放入 MainScope 的 Actor Runtime 内部实现。

关键约束：

```text
Dispatcher 不是新队列。
Dispatcher 只在 MainScope Owner Thread 执行。
Dispatcher 不进入 CustomScope。
Dispatcher 不暴露 ActorWorld。
```

---

## 5. Command Batch

### 5.1 Command Kind

```csharp
internal enum ActorProjectionCommandKind :
    byte
{
    Ensure,
    Enable,
    Disable,
    Release
}
```

### 5.2 Entity 地址

MainScope 不能访问 OriginScope EcsWorld，因此 Entity 只作为回执地址返回：

```csharp
internal readonly struct ProjectedEntityKey
{
    internal readonly int EntityId;
    internal readonly int EntityVersion;
}
```

如果 `Arch.Core.Entity` 已经是稳定的值类型并包含 Id/Version，可以直接复用，不重复新增 Key 类型。

### 5.3 Command

```csharp
internal readonly struct ActorProjectionCommand
{
    internal readonly ActorProjectionCommandKind
        Kind;

    internal readonly ProjectedEntityKey Entity;

    internal readonly ActorId ActorId;
    internal readonly int ActorTypeId;

    internal readonly
        ProjectedActorReleasePolicy
        ReleasePolicy;
}
```

字段使用规则：

```text
Ensure：
    Entity
    ActorTypeId
    ReleasePolicy

Enable / Disable：
    Entity
    ActorId

Release：
    Entity
    ActorId
    ReleasePolicy
```

### 5.4 Batch Event

```csharp
internal readonly struct
    ActorProjectionCommandBatchScopeEvent
{
    internal readonly ScopePayloadHandle
        Payload;

    internal readonly int Count;
}
```

`OriginScopeId` 已存在于 `ScopeEventEnvelope`，不在 Payload 中重复保存。

批次数据必须通过现有 ScopeEvent Payload/Lease 机制转移所有权；不得引入 Actor 专用 Payload Dictionary。

---

## 6. Result Batch

### 6.1 Result Code

```csharp
internal enum ActorProjectionResultCode :
    byte
{
    Applied,
    AlreadyApplied,
    ActorMissing,
    CreateFailed
}
```

`ActorMissing` 对 Enable、Disable、Release 表示 Actor 已失效；OriginScope 应清理旧绑定。

### 6.2 Result

```csharp
internal readonly struct ActorProjectionResult
{
    internal readonly ActorProjectionCommandKind
        Kind;

    internal readonly ProjectedEntityKey Entity;

    internal readonly ActorId RequestedActorId;
    internal readonly ActorId ResultActorId;

    internal readonly int ActorTypeId;

    internal readonly ActorProjectionResultCode
        Code;
}
```

### 6.3 Result Event

```csharp
internal readonly struct
    ActorProjectionResultBatchScopeEvent
{
    internal readonly ScopePayloadHandle
        Payload;

    internal readonly int Count;
}
```

Result 进入 OriginScope 的标准 `ScopeEventInbox`。

不得使用：

```text
EcsResultQueue
CompletionPort
ProjectionResultQueue
PromiseQueue
```

Projection 是异步状态同步，不使用 ScopeCall 等待每个 Actor 命令。

---

## 7. World 与 ActorWorld 解耦

`World.Projection` 不再持有：

```text
ActorWorld _scopeActors
LayerRuntime → ActorWorld fallback
GetActorWorld()
BindScopeActors(ActorWorld)
```

替换为只表达 Projection 命令能力的内部 Sink：

```csharp
internal interface IProjectedActorCommandSink
{
    ProjectedActorEnsureSubmitResult
        TryEnsure(
            Entity entity,
            int actorTypeId,
            ProjectedActorReleasePolicy releasePolicy);

    ControlEnqueueResult TryEnable(
        Entity entity,
        ActorId actorId);

    ControlEnqueueResult TryDisable(
        Entity entity,
        ActorId actorId);

    ControlEnqueueResult TryRelease(
        Entity entity,
        ActorId actorId,
        ProjectedActorReleasePolicy releasePolicy);
}
```

具体实现：

```text
MainScopeProjectedActorCommandSink：
    在 MainScope Owner Thread
    直接调用 MainScopeActorProjectionDispatcher
    同步返回结果

ScopeEventProjectedActorCommandSink：
    写入当前 OwnerScope 的本地 CommandBatch
    Flush 时投递 MainScope ScopeEvent
```

`World` 只能持有 `IProjectedActorCommandSink`，不能持有 ActorWorld 或 MainScope Runtime。

### 7.1 Ensure Submit Result

```csharp
internal readonly struct
    ProjectedActorEnsureSubmitResult
{
    internal readonly
        ControlEnqueueResult SubmitResult;

    internal readonly bool
        CompletedSynchronously;

    internal readonly ActorId ActorId;
}
```

MainScope 本地直达：

```text
CompletedSynchronously = true
ActorId = 创建结果
```

CustomScope：

```text
CompletedSynchronously = false
SubmitResult = Accepted
ActorId = Invalid
EnsurePending = true
```

实际类型可与 `faster` 现有 `ControlEnqueueResult`、`ProjectedActorHandle` 合并复用，不要求为了名字一致创建重复结构。

---

## 8. OriginScope 命令提交

### 8.1 Ensure

```text
前置：
    Entity 有 ProjectedActorRef
    ActorId Invalid
    EnsurePending = false

TryEnsure：
    MainScope Direct：
        立即创建并 Bind

    CustomScope：
        加入 CommandBatch
        Flush 被 MainScope EventInbox 接受后：
            EnsurePending = true

        Flush 被拒绝：
            EnsurePending = false
            保持 Projectable，后续重试
```

MainScope 不访问 OriginScope World。

### 8.2 Touch

```text
ActorId Invalid：
    TryEnsure

State Disabled / DisablePending：
    若 EnablePending = false：
        TryEnable

State Active：
    nowTicks < NextTouchTicks：
        直接返回 true

    否则：
        刷新 ExpireAtTicks
        刷新 NextTouchTicks
```

只有 Ensure/Enable 需要 MainScope。

### 8.3 Disable

Sweep 到期且 `RetirePolicy.Disable`：

```text
TryDisable accepted：
    State = DisablePending
    ExpireAtTicks = long.MaxValue

TryDisable rejected：
    保持 Active
    保持可重试状态
```

Disable Result成功：

```text
State = Disabled
ActorId 保留
ProjectedActorRef 保留
ActiveProjectedActorList Binding 保留
```

下一次 Touch 发送 Enable。

### 8.4 Enable

```text
TryEnable accepted：
    EnablePending = true

Enable Result Applied / AlreadyApplied：
    EnablePending = false
    State = Active
    刷新 ExpireAtTicks
    刷新 NextTouchTicks

Enable Result ActorMissing：
    Clear ActorId
    State = Projectable
    下一次 Touch 重新 Ensure
```

### 8.5 Release

Release 命令被 EventInbox 接受后：

```text
State = ReleasePending
ActorId 暂时保留
ProjectedActorRef 暂时保留
```

不能像同步 ActorWorld 路径一样立即清空 Binding。

Release Result：

```text
Applied / AlreadyApplied / ActorMissing：
    Clear ProjectedActorMeta.ActorId
    Clear ProjectedActorRef.ActorId
    从 ActiveProjectedActorList 移除
    State 回到 Projectable 或 Released
```

等待 Result 后清理，是 CustomScope 异步管线成立所必需的修改。

---

## 9. MainScope Command Apply

MainScope Dispatcher 对 CommandBatch 进行顺序 Apply：

```csharp
private ActorProjectionResult Apply(
    in ActorProjectionCommand command)
{
    switch (command.Kind)
    {
        case ActorProjectionCommandKind.Ensure:
            return Ensure(in command);

        case ActorProjectionCommandKind.Enable:
            return Enable(in command);

        case ActorProjectionCommandKind.Disable:
            return Disable(in command);

        case ActorProjectionCommandKind.Release:
            return Release(in command);

        default:
            throw new ArgumentOutOfRangeException();
    }
}
```

### 9.1 Ensure

复用：

```text
ProjectedActorTypeRegistry.CreateActorByTypeId
ActorWorld.CreateProjectedActor<TActor>
ProjectedActorHandle
```

MainScope 只创建 Actor，不写 OriginScope Entity Binding。

### 9.2 Enable / Disable / Release

直接复用：

```text
ActorWorld.EnableProjectedActorIfDisabled
ActorWorld.DisableProjectedActor
ActorWorld.ReleaseProjectedActor
```

MainScope 将执行结果转换为 ResultCode。

### 9.3 Result 返回

```text
Apply 完整 CommandBatch
    → 构建 ResultBatch
    → OriginScope Endpoint.TryPost(ResultEvent)
```

Result Event 使用 ScopeEvent 的 Internal/Critical 保留容量。

如果 OriginScope Endpoint 已关闭：

```text
Ensure 成功产生的新 Actor：
    MainScope 立即按 Command.ReleasePolicy 释放
    防止 Actor 泄漏

其他 Result：
    丢弃 Result Payload
```

不得建立 Result Retry Queue。

MainScope 按 Runtime 停止顺序最后关闭，因此正常停止期间 Result Event 应可返回。

---

## 10. OriginScope Result Apply

OriginScope Result Handler 只在 Owner Thread 修改 EcsWorld。

### 10.1 Entity 校验

每个 Result 首先验证：

```text
Entity 仍存在
Entity.Version 等于 Result.EntityVersion
Entity 仍有 ProjectedActorRef
ActorTypeId 仍匹配
当前 Pending / State 与 Result.Kind 一致
```

不满足时：

```text
不修改 Entity Binding
```

如果被拒绝的是成功 Ensure Result：

```text
向 MainScope 发送 Release Command
释放无人接收的新 Actor
```

### 10.2 不新增 Revision 字段

第一阶段不新增 ProjectionRevision。

依赖以下现有约束防止迟到结果覆盖新状态：

```text
Entity Id + Version
ActorTypeId
RequestedActorId
EnsurePending
EnablePending
ProjectedActorState
MPSC 同一 Producer FIFO
```

只有在压力测试证明这些条件无法区分合法状态时，才单独提案增加 Version；本任务不得预先增加冗余状态字段。

---

## 11. Actor Event Batch

现有：

```text
ProjectionBatchBuffer<TEvent>
ActorId[]
TEvent[]
ArrayPool
4 路展开 PostTo
```

全部保留。

删除或替换：

```text
PostToRuntimeOwner
ActorEventBatchResult<TEvent>
Action<ActorWorld> closure
new ActorId[Count]
new TEvent[Count]
ActorCommandPayloadStorage Dictionary
LayerRuntime.ActorEventInbox
```

### 11.1 ScopeEvent Transport

```csharp
internal readonly struct
    ActorPostBatchScopeEvent<TEvent>
    where TEvent : struct
{
    internal readonly ScopePayloadHandle
        Payload;

    internal readonly int Count;
}
```

`ProjectionBatchBuffer<TEvent>` 增加所有权转移操作：

```csharp
internal ScopePayloadHandle
    DetachToScopeEventPayload(
        IScopePayloadPool payloadPool);
```

成功 Detach 后，源 Buffer 清空内部数组引用，不能再次 Dispose 同一数组。

发送失败：

```text
OriginScope 释放 Payload
```

发送成功：

```text
MainScope Handler finally 释放 Payload
```

MainScope Handler：

```text
Take Batch Lease
    → batch.PostTo(ActorWorld)
    → finally Dispose/Return ArrayPool
```

不得复制数组，不得创建委托闭包。

---

## 12. Batch Flush 与 Pending 回滚

CustomScope 的 CommandBatch 是 ECS Projection 的本地临时批次，不是第三条通讯通道。

```text
Projection Query / Sweep：
    Add Command
    标记临时 Pending

ECS Projection SafePoint：
    Detach Batch
    TryPost MainScope ScopeEvent
```

如果 ScopeEvent Inbox 拒绝 Batch：

```text
逐条回滚本次提交产生的：
    EnsurePending
    EnablePending
    DisablePending
    ReleasePending

释放 Batch Payload
保留 Entity 可重试状态
```

如果接受：

```text
Payload 所有权转移给 MainScope EventInbox
Pending 状态保持，等待 Result
```

Agent 必须复用或扩展现有 Projection Batch 结构，不建立永久 `ProjectionCommandQueue`。

---

## 13. Tick 顺序

### 13.1 CustomScope

```text
1. Drain ScopeCall
2. Drain ScopeEvent
   - 应用 Projection ResultBatch
3. 本地 Timer / Post / Service / Context
4. ECS Query / Bring
5. Projection Sweep
6. Flush Projection CommandBatch 到 MainScope
7. ECS Structural SafePoint
```

Result 必须在下一轮 Projection Query/Sweep 前应用，避免重复 Ensure/Enable。

### 13.2 MainScope

```text
1. Drain MainScope ScopeEvent
   - Apply CustomScope Projection Commands
   - Apply Actor PostBatch
2. MainScope 本地 ECS / Projection
   - 使用同一 Dispatcher 的本地直达方法
3. Tick Inline Scope
4. 再次执行标准 MainScope ScopeEvent Drain
   - 接收 Inline Scope 本帧产生的 Actor Command
5. ActorWorld.Pump
```

第二次 Drain 仍是标准 `ScopeEventInbox` Drain，不是 Actor 专用队列。

如果 Runtime 决定不允许同帧第二次 Event Drain，则 Inline Scope 的 Actor 命令统一下一帧 Apply；必须全局选择一种语义并写入 Tick 测试，不能建立 Projection 旁路。

本文按 02 号文档采用“ActorWorld.Pump 前第二次标准 ScopeEvent Drain”。

---

## 14. Stop / Dispose

### 14.1 CustomScope Stop

```text
1. 停止产生新的 Projection 业务命令。
2. 继续接收 Projection Result Event。
3. 将所有仍绑定 ActorId 的 Entity 组成 ReleaseBatch。
4. ReleaseBatch 以 Internal/Critical ScopeEvent 发往 MainScope。
5. 等待所有已接受 Projection Command 进入终态。
6. 应用 Release Result，清理 Binding。
7. Dispose EcsWorld。
```

即使 StopPolicy 是 Drop，Actor Release Internal Event也不能作为普通业务事件丢弃。

不得在未释放 MainScope Actor 的情况下直接 Dispose CustomScope EcsWorld。

### 14.2 MainScope Stop

MainScope 最后停止：

```text
CustomScope Projection 全部终结
    → MainScope Drain 剩余 Actor ScopeEvent
    → MainScope 本地 Projection 清理
    → ActorWorld.Pump / Release
    → ActorWorld Dispose
```

### 14.3 Scope Dispose

Dispose 后到达的 Result：

```text
ScopeEndpoint Generation 失效
MainScope 释放 Ensure 新建 Actor
Result Payload 由发送方或 EventInbox 所有权规则释放
```

---

## 15. 错误处理

不得吞掉 Projection 运输和 Actor Apply 异常。

```text
Command Batch Payload 解码失败：
    MainScope Fault Pipeline
    释放 Payload

Actor Ensure CreateFailed：
    返回 Result
    Origin Clear EnsurePending

ActorWorld 操作抛出异常：
    记录 Command Kind / ActorId / OriginScope
    返回失败 Result或触发既有 Runtime Fault Policy

Result Apply 发现 Entity 失效：
    忽略 Binding
    Ensure 新 Actor必须 Release
```

不增加业务回滚系统。

---

## 16. faster 分支复用

### 16.1 直接复用

| faster 文件 | 复用内容 |
|---|---|
| `LayerBase/ECS/Projection/ProjectedActorRef.cs` | ActorId、KeepAlive、ExpireAt、Touch 节流和策略字段 |
| `LayerBase/ECS/Projection/ProjectedActorMeta.cs` | 状态机、ActiveListIndex、EnsurePending、EnablePending |
| `LayerBase/ECS/Projection/ProjectedActorBindingUtility.cs` | Bind / Clear |
| `LayerBase/ECS/Projection/ActiveProjectedActorList.cs` | 预算化 Sweep、ActiveListIndex、RetirePolicy 判断 |
| `LayerBase/ECS/Projection/ProjectedActorPolicies.cs` | Create / Retire / Release 策略 |
| `LayerBase/Actor/Storage/ActorWorld.ProjectedActor.cs` | Create / Enable / Disable / Release 实际操作 |
| `LayerBase/ECS/Projection/Flow/ProjectionBatchBuffer.cs` | ArrayPool、容量预测、批量 PostTo 和展开循环 |
| `ProjectedActorTypeRegistry` | ActorTypeId → Actor Factory |
| Projection Touch / Disable / Ensure 测试 | 原业务语义 |

### 16.2 修改后复用

| faster 文件 | 保留 | 必须修改 |
|---|---|---|
| `ProjectedActorBinding.cs` | Ensure、Touch、RefreshDeadline、状态判断 | 删除 ActorWorld 参数和 `ShouldDeferActorWorldAccess`；改用 `IProjectedActorCommandSink` |
| `IProjectedActorLifecycleSink.cs` | Enable / Disable / Release 抽象 | Remote 实现改为 MainScope ScopeEvent；MainScope Direct 实现保留 |
| `World.Projection.cs` | ActiveProjectedActorList、ECS Scheduler、Meta访问 | 删除 `_scopeActors`、`GetActorWorld`、`BindScopeActors`；绑定 CommandSink |
| `ActiveProjectedActorList.cs` | Sweep 算法 | Release 在 Result 前不清 Binding；Disable/Release 使用异步状态 |
| `ProjectionBatchBuffer<TEvent>` | Pool、Grow、PostTo | 删除 `PostToRuntimeOwner` 和 EcsResultQueue 分支；增加 ScopeEvent Payload Detach |
| ScopeEvent Generator / Route | MPSC 与强类型 Dispatcher | 增加内部 Projection Command/Result/PostBatch Route |

### 16.3 删除或禁止移植

```text
ProjectedActorEnsureResult : IEcsResultItem
ActorEventBatchResult<TEvent> : IEcsResultItem

EcsResultQueue 作为 ActorWorld 中转
LayerRuntime.ActorEventInbox
LayerRuntime.ActorLifecycleInbox
ScopeActorGateway
独立 ProjectionChannel

ActorCommandPayloadStorage 中：
    Dictionary<int, PayloadEntry>
    Action<ActorWorld>
    闭包批量 Post
```

如果 `LayerRuntime.ActorEventInbox` 仍被非 Projection 功能使用，只删除 Projection 接入分支，不得顺带删除其他功能。

---

## 17. 需要修改的代码位置

优先检查：

```text
LayerBase/ECS/Projection/
    ProjectedActorBinding.cs
    ProjectedActorBindingUtility.cs
    ProjectedActorMeta.cs
    ProjectedActorRef.cs
    ActiveProjectedActorList.cs
    IProjectedActorLifecycleSink.cs
    World.Projection.cs
    Flow/ProjectionBatchBuffer.cs
    Flow/ProjectedActorEnsureResult.cs
    Flow/ActorEventBatchResult.cs

LayerBase/Actor/Storage/
    ActorWorld.ProjectedActor.cs

LayerBase/Application/
    LayerRuntime.ActorCommands.cs

LayerBase/Scope/
    ScopeMessages.cs
    ScopeRouteTable.cs
    ScopeEvent Dispatcher

LayerBase.Generator/
    ScopePostDispatchGenerator.cs
```

只在现有 ScopeEvent Payload 系统无法承载批量 Lease 时新增通用 Payload 类型；不得新增 Actor 专用 Payload Store 或 Queue。

---

## 18. Agent 执行任务

```text
1. 保留 ProjectedActorRef/Meta/Policies 原字段和语义。
2. 删除 CustomScope World 对 ActorWorld 的直接引用。
3. 删除 ProjectedActorBinding 方法的 ActorWorld 参数。
4. 引入或改造 IProjectedActorCommandSink。
5. MainScope 实现本地直达 Sink。
6. CustomScope 实现 ScopeEvent Batch Sink。
7. 定义 Command Kind、Command、Result。
8. 定义 CommandBatch/ResultBatch 内部 ScopeEvent Route。
9. Build/Generator 分配稳定 RouteId。
10. MainScope ScopeEvent Dispatcher 调 ActorWorld 原方法。
11. Result 通过 OriginScope ScopeEventInbox 返回。
12. OriginScope 只在 Owner Thread Apply Result。
13. Ensure 使用 Entity Version、ActorTypeId、EnsurePending 校验。
14. Enable/Disable/Release 使用 ActorId 和 State 校验。
15. Release 等待 Result 后再 Clear Binding。
16. Touch Active 只刷新本地期限，不发消息。
17. Touch Invalid/Disabled 分别转换为 Ensure/Enable。
18. ProjectionBatchBuffer 复用 ArrayPool。
19. 删除 Action<ActorWorld>、数组复制和 Actor Payload Dictionary 热路径。
20. 发送失败回滚 Pending 并释放 Payload。
21. Result 发送失败时释放 Ensure 新建 Actor。
22. Stop 发送最终 ReleaseBatch并等待终态。
23. 删除 Projection 使用的 EcsResultQueue/Actor Inbox 旁路。
24. 保留 MainScope 本地直达快路径。
25. 建立线程、状态、Payload 和性能测试。
```

---

## 19. 必须测试

```text
Custom_scope_world_does_not_reference_actor_world

Projection_uses_main_scope_event_inbox

No_projection_channel_exists

No_actor_command_queue_exists_for_projection

Main_scope_projection_uses_local_direct_path

Custom_scope_ensure_round_trip_binds_actor_id

Ensure_pending_prevents_duplicate_command

Ensure_queue_rejection_clears_pending

Stale_entity_version_rejects_ensure_result

Rejected_ensure_result_releases_created_actor

Touch_active_only_refreshes_local_deadline

Touch_active_before_next_touch_sends_no_event

Touch_invalid_submits_ensure

Touch_disabled_submits_enable

Enable_result_restores_active_state

Enable_actor_missing_clears_binding

Disable_result_preserves_actor_id

Disable_result_marks_disabled

Release_does_not_clear_binding_before_result

Release_result_clears_ref_meta_and_active_list

Return_to_pool_policy_is_preserved

Destroy_immediately_policy_is_preserved

Detach_and_finish_policy_is_preserved

Projection_command_batch_preserves_per_scope_fifo

Result_returns_through_origin_scope_event_inbox

Projection_payload_is_released_exactly_once

Command_queue_rejection_rolls_back_pending_state

Actor_post_batch_does_not_allocate_new_arrays

Actor_post_batch_does_not_create_action_closure

Inline_scope_commands_are_drained_before_actor_world_pump

Custom_scope_stop_releases_all_projected_actors

Main_scope_stops_after_custom_projection_cleanup

Actor_world_is_only_written_on_main_scope_owner_thread

Steady_state_touch_is_zero_allocation

Steady_state_actor_post_batch_is_zero_allocation
```

继续复用并迁移 `faster` 原 Projection、ProjectedActor 和 ActorWorld 测试。

---

## 20. 验收否决项

出现以下任意一项，任务不通过：

```text
CustomScope 或 CustomScope EcsWorld 持有 ActorWorld

ScopeRuntime 暴露 ActorWorld

Projection 使用独立 ProjectionChannel

Projection 使用 ActorCommandQueue / ActorLifecycleInbox 旁路

Result 使用 EcsResultQueue / CompletionPort

MainScope 之外调用 ActorWorld Create/Enable/Disable/Release/Post

Touch 每次都向 MainScope 发消息

Release 在 MainScope Result 前清空 Entity Binding

Ensure Result 直接从 MainScope 修改 OriginScope EcsWorld

跨 Scope 传 World、Chunk、ref Component 或 Actor 对象

批量 Post 使用 Action<ActorWorld> 闭包

批量 Post 每次 new ActorId[] / TEvent[]

运行期通过 Dictionary/Type/反射选择 Projection Route

Payload 发生双重释放或发送失败泄漏

为了本任务重写 ActorWorld Pool、Mailbox、ProjectedActor Policy 或 ECS Query
```

---

## 21. 本阶段不修改的内容

本文不修改：

```text
ActorWorld 内部 Archetype / Pool / Mailbox 算法
EventCenter 注册和派发语义
ScopeCall 协议
DI / Mount / Provide / From
WorkerEventJob
ECS Query 与 CommandBuffer 通用实现
ProjectedActor 公开业务 API
```

本文只完成：

```text
CustomScope Projection 意图
    → 标准 ScopeEvent
    → MainScope ActorWorld
    → 标准 ScopeEvent Result
    → OriginScope Binding

并确保 ActorWorld 始终由 MainScope唯一写入和推进。
```
