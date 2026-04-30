# Event System

Event 系统用于实现模块间的异步通知。

## 特性
- **结构体事件**：强制使用 `struct` 以减少 GC。
- **高性能分发**：底层采用 SOA 布局，缓存友好。

## 订阅方式对比

| 特性 | [Subscribe] | [SubscribeFlow] |
| :--- | :--- | :--- |
| **返回类型** | `void` | `EventHandledState` |
| **语义** | 纯通知，不可中断 | 业务流控制，支持截断 |
| **截断能力** | 否 | 是 (返回 `Handled` 可中断后续) |
| **典型用途** | 状态更新、日志打点 | 逻辑拦截、条件检查、业务流分支 |

## Subscribe vs SubscribeFlow 深度解析

### 1. [Subscribe]：纯粹的通知
当模块仅仅关心“事件发生了什么”而不打算改变事件的后续处理路径时使用。它与 `SubscribeNotify` 的区别在于它内部包裹了异常捕获机制，具备故障隔离能力。

```csharp
[Subscribe]
private void OnItemPickedUp(in ItemEvent e) 
{
    _ui.RefreshInventory(); // 不影响事件的后续传播
}
```

### 2. [SubscribeFlow]：逻辑流控制
当模块需要参与“业务决策”时使用。通过返回 `EventHandledState.Handled`，您可以阻止事件传递给该层级后续的处理器，从而实现逻辑拦截（例如在处理伤害前拦截“无敌”状态）。

```csharp
[SubscribeFlow]
private EventHandledState OnBeforeDamage(in DamageEvent e) 
{
    if (this.IsInvincible) 
    {
        return EventHandledState.Handled; // 截断：伤害事件不会传给实际的扣血逻辑
    }
    return EventHandledState.Continue; // 继续：允许事件继续传播
}
```

## 建议
- **默认使用 `[Subscribe]`**。
- 只有在明确需要“拦截”或“逻辑分流”时才使用 `[SubscribeFlow]`。
- 切勿在 `[SubscribeFlow]` 中执行过于耗时的逻辑，因为它会阻塞同步分发路径。
