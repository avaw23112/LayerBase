# 21 MainScope ActorWorld、Mailbox 与 Actor 生命周期迁移

> **强制执行规范：** 本文遵守 01 号规范。  
> **复用来源：** `faster` 分支 ActorWorld、TypedStorage、Mailbox、ActorLifecycleScheduler、ActorCall 与对象池。

## 1. 架构意义

Actor 是 Runtime 级表现/行为对象，但其实际执行域固定为 MainScope：

```text
MainScope
    → ActorWorld
    → ActorStorage
    → Mailbox / Lifecycle / Call
```

CustomScope 可以引用 `ActorHandle`，但不能访问 Actor 对象或 ActorWorld。

## 2. 模块关系

```text
MainScope Event/ECS
    → ActorWorld 本地 API

CustomScope
    → ActorCommand ScopeEvent<MainScope>
    → MainScope ActorCommandHandler
    → ActorWorld

CustomScope ActorCall
    → ScopeCall<MainScope>
    → ActorCallRegistry
```

Projection 文档 22 复用同一命令入口。

## 3. 最终公有 API

MainScope 业务：

```csharp
ActorHandle actor =
    this.Actors().Create<PlayerActor>(
        new PlayerActorArgs(playerId));

this.Actors().Post(
    actor,
    new PlayHitAnimationEvent());

HealthResult health =
    await this.Actors().Call<
        GetHealthRequest,
        HealthResult>(
            actor,
            new GetHealthRequest());
```

CustomScope 调用同样的高层 API，但返回内部异步结果：

```csharp
this.Actors().TryPost(
    actor,
    new PlayHitAnimationEvent());
```

`Actors()` 根据 OwnerScope：

```text
MainScope → LocalActorAccessor
CustomScope → RemoteActorAccessor
```

业务代码不取得 ActorWorld。

## 4. Actor Handle

```csharp
public readonly struct ActorHandle
{
    public int TypeId { get; }
    public int Index { get; }
    public int Version { get; }
    public int RuntimeGeneration { get; }
}
```

跨 Scope 只传 Handle。

## 5. 关键内部结构

```csharp
internal sealed class ActorWorld
{
    private readonly TypedActorStorageRuntime[] _storages;
    private readonly ActorMailboxScheduler _mailboxes;
    private readonly ActorLifecycleScheduler _lifecycle;
    private readonly ActorCallRegistry _calls;
}
```

ActorTypeId 直接索引 storage。

Mailbox：

```csharp
internal readonly struct ActorMailEnvelope
{
    public readonly ActorHandle Target;
    public readonly int EventId;
    public readonly PayloadHandle Payload;
}
```

## 6. ActorCommand 协议

```csharp
internal readonly struct ActorCommandBatch
{
    public readonly int OriginScopeId;
    public readonly PayloadHandle Commands;
    public readonly int Count;
}
```

MainScope Handler：

```csharp
[ScopeEvent]
private void Apply(in ActorCommandBatch batch)
{
    _actorWorld.Apply(in batch);
}
```

批量比逐命令 ScopeEvent 更合适。

## 7. 生命周期

保留：

```text
Rent
Enable
Update
FixedUpdate
Disable
Return
Dispose
```

MainScope Tick：

```text
Drain Actor Mail
Pump Lifecycle Budget
Apply Projection Commands
```

Actor 自身不创建线程。

## 8. 业务场景：WorkerScope AI 请求动画

```text
AIScope 判断 NPC 受击
    → RemoteActorAccessor.TryPost
    → ActorCommandBatch
    → MainScope
    → ActorWorld Mailbox
    → NpcActor Handler
```

AI 线程不接触 Unity 对象。

## 9. faster 复用

直接复用：

```text
ActorWorld/TypedStorage
ActorHandle Version
Actor Pool
Mailbox 策略
ActorLifecycleScheduler
ActorCall
现有 Actor Benchmark
```

改造：

```text
ActorWorld 从 Runtime Kernel 明确归 MainScope
CustomScope Actor API 转命令
Actor 类型注册进入 Manifest/Build Plan
```

禁止：

```text
CustomScope ActorWorld
Actor 实例跨线程传递
Actor 生命周期 Worker Thread 化
```

## 10. 迁移任务

```text
1. MainScope LocalAccess 增加 ActorAccessor。
2. ActorWorld 创建移入 MainScope Activate。
3. CustomScope 生成 RemoteActorAccessor。
4. 定义批量 ActorCommand/Call 路由。
5. Actor Plan 使用 TypeId/Slot。
6. Scope Stop 清理来源 Scope 的 Pending Command。
```

## 11. API 校验场景

MainScope 与 WorkerScope 使用相同 `this.Actors().TryPost(handle,event)` 业务代码；前者本地入 Mailbox，后者进入 ScopeEvent，但最终都在 MainScope ActorWorld 处理。
