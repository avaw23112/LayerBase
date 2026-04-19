# 🚀 LayerBase.Task: 工业级游戏异步任务系统

**LayerBase.Task** 是 [LayerBase 架构总线](https://github.com/avaw23112/LayerBase)的核心基建之一，提供了一套专为游戏引擎（如 Unity、Godot）心跳循环（Pump）深度调优的零分配（Zero-Allocation）异步任务模型：`LBTask`。

---

## 🎯 为什么要开发 LBTask？

在现代游戏开发中，异步操作（如等待动画播放、网络请求回调、分帧加载）无处不在。使用 C# 标准的 `Task` 或 `Task<T>` 会面临一个无法回避的痛点：
**每次 `await Task` 都会在托管堆上分配内存。** 在一秒钟 60 帧或 120 帧的游戏主循环中，这种高频的堆分配会导致严重的垃圾回收（GC）尖峰，从而引发游戏卡顿和掉帧。

`LBTask` 专为此而生：
1. **同步路径零 GC 分配**：通过内置的池化技术与状态机机机制，如果一个异步任务是同步完成的，它将**绝对不会**在堆上产生任何 GC Allocation。
2. **完美融合引擎心跳**：LBTask 深度集成于 `LayerBase.LayerHub.Pump(deltaTime)`。它拥有自己的 `LayerBaseSynchronizationContext`，无需依赖引擎原生的同步上下文即可实现安全的线程回归和时间调度。
3. **极简 API**：保留了原生 Task 的手感，支持 `await LBTask.Delay()`、`await LBTask.NextFrame()` 等游戏级特性。

---

## 📦 如何使用？

本包默认已随主库 `LayerBase` 自动集成，无需单独配置。如果您需要单独使用或查看依赖，请参考以下方式。

### 基本异步处理

通过在 `Manager` 或 `Service` 的事件处理器上挂载 `[SubscribeAsync]` 特性，即可轻松使用 `LBTask` 编写全异步逻辑，且全程 0 GC：

```csharp
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.DI;

public partial class BattleManager : ILayerContext
{
    [SubscribeAsync]
    private async LBTask OnPlayerDead(PlayerDeathEvent e)
    {
        // 游戏级的延迟等待：此处的 Delay 完全依托于 LayerHub.Pump 的驱动，
        // 且不会产生标准的 Task GC 垃圾
        await LBTask.Delay(TimeSpan.FromSeconds(3f));
        
        Console.WriteLine("3 秒后，玩家重生逻辑触发...");
        
        // 甚至可以等待下一帧
        await LBTask.NextFrame();
        
        Console.WriteLine("这是下一帧...");
    }
}
```

### 返回值的异步任务 (`LBTask<T>`)

除了空任务，`LBTask` 也支持携带泛型返回值，其底层使用了 `LBTaskCompletionSource<T>` 实现了零分配的缓存回收：

```csharp
public async LBTask<int> CalculateHeavyDataAsync()
{
    await LBTask.Delay(TimeSpan.FromSeconds(1));
    return 42;
}

public async LBTask RunTest()
{
    int result = await CalculateHeavyDataAsync();
    Console.WriteLine(result);
}
```

---

## ⚙️ 核心 API 总览

*   **`LBTask.CompletedTask`**: 返回一个已完成的 LBTask（零分配）。
*   **`LBTask.Delay(TimeSpan)`**: 提供基于引擎时间的延迟操作。
*   **`LBTask.Delay(int milliseconds)`**: 基于毫秒的延迟。
*   **`LBTask.NextFrame()`**: 等待至引擎驱动的下一帧。
*   **`LBTaskCompletionSource<T>`**: 手动控制生命周期的异步状态源，推荐用于包装外部的跨线程回调。

---

## 🔗 关于 LayerBase
`LayerBase.Task` 是 LayerBase 高性能架构生态的一环，配合底层总线（1.5亿 TPS 分发）和源生成器（零反射依赖注入）使用，可解锁完整的工业级能力。

项目主页：[LayerBase GitHub 仓库](https://github.com/avaw23112/LayerBase)