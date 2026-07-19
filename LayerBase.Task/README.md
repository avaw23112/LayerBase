# 🚀 LayerBase.Task: 工业级游戏异步任务系统

**LayerBase.Task** 是 [LayerBase 架构总线](https://github.com/avaw23112/LayerBase)的核心基建之一，提供了一套专为游戏引擎（如
Unity、Godot）心跳循环（Pump）深度调优的零分配（Zero-Allocation）异步任务模型：`LBTask`。

---

## 🎯 为什么要开发 LBTask？

在现代游戏开发中，异步操作（如等待动画播放、网络请求回调、分帧加载）无处不在。使用 C# 标准的 `Task` 或 `Task<T>` 会面临一个无法回避的痛点：
**每次 `await Task` 都会在托管堆上分配内存。** 在一秒钟 60 帧或 120 帧的游戏主循环中，这种高频的堆分配会导致严重的垃圾回收（GC）尖峰，从而引发游戏卡顿和掉帧。

`LBTask` 专为此而生：

1. **同步路径零 GC 分配**：通过内置的池化技术与状态机机机制，如果一个异步任务是同步完成的，它将**绝对不会**在堆上产生任何
   GC Allocation。
2. **双核驱动机制 (自驱动延迟 + 帧同步)**：
    - **自驱动的 `Delay`**：`LBTask.Delay` 底层自带了一个基于最小堆（Min-Heap）和 `System.Threading.Timer`
      的高精度调度器。它完全是自驱动的，跑在线程池中，**不需要**依赖主循环的 `Pump` 来推进时间，这意味着即便主线程卡死，您的异步超时逻辑依然精准。
    - **帧同步与线程回归**：对于 `NextFrame()` 或回到主线程的操作，`LBTask` 依赖标准的 `SynchronizationContext`。如果您在
      Unity 中，它会自动使用 Unity 的上下文；如果您在纯 C# 服务端，可以手动调用
      `LayerBaseSynchronizationContext.InstallAsCurrent()` 并在您的主循环中驱动它。
3. **极简 API**：保留了原生 Task 的手感，支持 `await LBTask.Delay()`、`await LBTask.NextFrame()` 等特性。

---

## 📦 如何单独使用？

虽然本包默认已随主库 `LayerBase` 自动集成，但您完全可以**将它剥离出来单独使用**，作为原生 `Task` 的零 GC 替代品。

### 1. 基础的异步延迟

`LBTask.Delay` 不需要任何外部驱动，直接 `await` 即可。它委托给 .NET `Task.Delay` / `TimeProvider.System`，不维护全局 Delay 堆、锁或自建 Timer：

```csharp
public async LBTask DoSomethingDelay()
{
    await LBTask.Delay(TimeSpan.FromSeconds(3f));
    Console.WriteLine("3 秒后触发...");
}
```

### 2. 帧同步与上下文配置 (如需独立驱动)

如果您想使用 `LBTask.NextFrame()`，或者希望确保 `await` 之后的代码回到您的主线程，您需要一个同步上下文。

* **在 Unity / Godot 中**：引擎已经为您配置好了原生的上下文，直接使用即可。
* **在纯 C# 环境中**：您可以使用内置的 `LayerBaseSynchronizationContext`：

```csharp
// 1. 在游戏/服务器启动时，安装上下文
var ctx = LayerBaseSynchronizationContext.InstallAsCurrent();

// 2. 编写分帧逻辑
public async LBTask FrameLogic()
{
    Console.WriteLine("第一帧");
    await LBTask.NextFrame();
    Console.WriteLine("第二帧 (已回到主线程)");
}

// 3. 在您的主循环 (如 while (true) 或者是 Update) 中驱动它
public void GameLoop()
{
    // 调用 Update 消费 NextFrame 与 Post 进来的回调
    ctx.Update(); 
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

* **`LBTask.CompletedTask`**: 返回一个已完成的 LBTask（零分配）。
* **`LBTask.Delay(TimeSpan)`**: 提供基于引擎时间的延迟操作。
* **`LBTask.Delay(int milliseconds)`**: 基于毫秒的延迟。
* **`LBTask.NextFrame()`**: 等待至引擎驱动的下一帧。
* **`LBTaskCompletionSource<T>`**: 手动控制生命周期的异步状态源，推荐用于包装外部的跨线程回调。

---

## 🔗 关于 LayerBase

`LayerBase.Task` 是 LayerBase 高性能架构生态的一环，配合底层总线（1.5亿 TPS 分发）和源生成器（零反射依赖注入）使用，可解锁完整的工业级能力。

项目主页：[LayerBase GitHub 仓库](https://github.com/avaw23112/LayerBase)

---
---

# 🚀 LayerBase.Task: Industrial-Grade Game Async Task System

**LayerBase.Task** is one of the core infrastructure components of
the [LayerBase Architecture Bus](https://github.com/avaw23112/LayerBase). It provides `LBTask`, a Zero-Allocation
asynchronous task model deeply optimized for game engine (e.g., Unity, Godot) heartbeat loops (Pump).

---

## 🎯 Why Develop LBTask?

In modern game development, asynchronous operations (such as waiting for animations, network request callbacks, and
frame-split loading) are everywhere. Using standard C# `Task` or `Task<T>` faces an unavoidable pain point:
**Every `await Task` allocates memory on the managed heap.** In a game main loop running at 60 or 120 FPS, these
high-frequency heap allocations lead to severe Garbage Collection (GC) spikes, causing stutters and frame drops.

`LBTask` was born to solve this:

1. **Zero GC Allocation on Synchronous Paths**: Through built-in pooling and state machine mechanisms, if an async task
   completes synchronously, it will **absolutely not** produce any GC Allocation on the heap.
2. **Dual-Core Drive Mechanism (Self-Driven Delay + Frame Sync)**:
    - **Self-Driven `Delay`**: `LBTask.Delay` features an underlying high-precision scheduler based on a Min-Heap and
      `System.Threading.Timer`. It is entirely self-driven, running on the ThreadPool, and **does not** rely on the main
      loop's `Pump` to advance time. This means even if the main thread hangs, your async timeout logic remains precise.
    - **Frame Sync & Thread Regression**: For `NextFrame()` or operations returning to the main thread, `LBTask` relies
      on the standard `SynchronizationContext`. In Unity, it automatically uses Unity's context; in pure C# servers, you
      can manually call `LayerBaseSynchronizationContext.InstallAsCurrent()` and drive it within your main loop.
3. **Minimal API**: Maintains the "feel" of native Tasks, supporting features like `await LBTask.Delay()`,
   `await LBTask.NextFrame()`, etc.

---

## 📦 How to Use Standalone?

While this package is integrated by default with the main `LayerBase` library, you can **strip it out and use it
independently** as a Zero-GC replacement for native `Task`.

### 1. Basic Async Delay

`LBTask.Delay` requires no external driver; simply `await` it. It delegates to .NET `Task.Delay` / `TimeProvider.System` and does not maintain a global Delay heap, lock, or custom Timer:

```csharp
public async LBTask DoSomethingDelay()
{
    await LBTask.Delay(TimeSpan.FromSeconds(3f));
    Console.WriteLine("Triggered after 3 seconds...");
}
```

### 2. Frame Sync & Context Configuration (For Independent Driving)

If you want to use `LBTask.NextFrame()`, or ensure code after `await` returns to your main thread, you need a
synchronization context.

* **In Unity / Godot**: The engine has already configured the native context for you; just use it directly.
* **In Pure C# Environments**: You can use the built-in `LayerBaseSynchronizationContext`:

```csharp
// 1. Install the context at game/server startup
var ctx = LayerBaseSynchronizationContext.InstallAsCurrent();

// 2. Write frame-split logic
public async LBTask FrameLogic()
{
    Console.WriteLine("Frame 1");
    await LBTask.NextFrame();
    Console.WriteLine("Frame 2 (Returned to main thread)");
}

// 3. Drive it in your main loop (e.g., while(true) or Update)
public void GameLoop()
{
    // Call Update to consume NextFrame and Post-ed callbacks
    ctx.Update(); 
}
```

### Async Tasks with Return Values (`LBTask<T>`)

In addition to void tasks, `LBTask` also supports generic return values. It uses `LBTaskCompletionSource<T>` internally
to implement zero-allocation cache recycling:

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

## ⚙️ Core API Overview

* **`LBTask.CompletedTask`**: Returns a completed LBTask (Zero-Allocation).
* **`LBTask.Delay(TimeSpan)`**: Provides delay operations based on engine time.
* **`LBTask.Delay(int milliseconds)`**: Millisecond-based delay.
* **`LBTask.NextFrame()`**: Waits until the next engine-driven frame.
* **`LBTaskCompletionSource<T>`**: An async state source for manual lifecycle control, recommended for wrapping external
  cross-thread callbacks.

---

## 🔗 About LayerBase

`LayerBase.Task` is part of the LayerBase high-performance architecture ecosystem. Used alongside the core bus (150M TPS
distribution) and Source Generators (Zero-Reflection DI), it unlocks full industrial-grade capabilities.

Project Homepage: [LayerBase GitHub Repository](https://github.com/avaw23112/LayerBase)
