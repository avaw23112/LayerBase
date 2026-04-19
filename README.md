# LayerBase: 面向数据的高性能 C# 游戏架构总线

**LayerBase** 是一款专为 Unity、Godot 及纯 C# 服务端打造的架构通讯框架。它融合了严格的分层依赖注入（DI）与极致优化的面向数据（DOD）事件总线，旨在为中大型游戏项目提供规范、稳健且极速的底层支撑。

---

## 🤔 架构的演进：我们在解决什么问题？

作为游戏开发者，在项目的生命周期中，我们几乎都会经历以下几个架构演进阶段：

### 阶段一：单例模式的失控 (The Singleton Era)
在项目初期，为了快速实现功能，我们往往倾向于使用单例：
```csharp
GameManager.Instance.UpdateHealth(-10);
UIManager.Instance.RefreshHpBar();
AudioManager.Instance.PlayDamageSound();
```
*思考：在短期内，几个 Manager 的互相调用尚能接受。但如果是一款大型游戏，当系统里存在成百上千个 Manager 时呢？*
很快，代码会演变成牵一发而动全身的网状结构。各个系统紧密耦合，“谁调用了谁”变得难以追踪，重构或剥离模块变得几乎不可能。

### 阶段二：事件总线的迷思 (The EventBus Era)
为了实现模块间的解耦，业界普遍的做法是引入 `Action`、`UniRx` 或是各种泛型 `EventBus`：
```csharp
EventBus.Publish(new DamageEvent { Amount = 10 });
```
表面上看，发送方和接收方确实解耦了。但这往往会引入两个更隐蔽的工程灾难：

1. **时序与流向的失控**：一个事件抛出，犹如泥牛入海。你无法确定是 UI 先刷新了状态，还是成就系统先记录了数据。当业务逻辑强依赖执行顺序时，缺乏层级管理的事件机制反而成了 Bug 的温床。
2. **底层性能的暗礁**：市面上绝大多数事件总线，其底层实现都离不开 `Dictionary<Type, List<Delegate>>`。在面对成千上万个实体（Entity）的高频交互时，频繁的字典哈希计算、委托调用的封箱拆箱，以及最致命的——**CPU 缓存未命中（Cache Miss）**，会将系统的吞吐量死死限制在百万级别。

*反思：难道我们就只能在“高耦合的单例”和“不可控的事件总线”之间妥协吗？我们能否既要可控的时序和清晰的架构，又要不打折扣的底层性能？*

### 阶段三：秩序与极速的重构 (LayerBase 的诞生)

为了打破这一僵局，LayerBase 提出了它的解决方案。

一方面，在**宏观架构**上，我们放弃了随地抛发事件的自由，引入了强约束的 `Layer -> Service -> Manager` 三层递进模型。事件的流向被严格限制在清晰的空间维度（Global / Bubble / Drop / Local）内，结合显式的依赖注入（DI），确保逻辑时序绝对可控。

另一方面，在**底层执行**上，为了不让高层架构的抽象拖累运行时性能，我们汲取了纯 ECS（Entity Component System）框架的核心思想，**用彻底的面向数据设计（Data-Oriented Design, DOD）重写了事件总线**。

---

## 📊 直观剖析：底层机制的质变

传统的 EventBus 在内存中是典型的 **AOS (Array of Structures)** 布局。当你派发一个事件时，CPU 需要在堆内存中进行多次非连续跳转：

```text
❌ 传统 EventBus 分发路径 (Cache Miss 频发)
EventBus 
  └─> [Type 哈希计算] 
        └─> [Dictionary Bucket 查找] 
              └─> [List 内存跳转] 
                    └─> [Handler 对象 (包含上下文与状态)] 
                          └─> 虚函数 Invoke 
```

而在 LayerBase 中，我们在启动期（Build）通过 C# 源生成器，将同类事件的所有处理器“拆解、脱水”，转化为纯粹的 **SOA (Structure of Arrays)** 布局：

```text
✅ LayerBase 零分支分发引擎 (完美 Cache 亲和性)
EventBucket<T>
 ├── Delegate[] SyncHandlers  [ ptr | ptr | ptr | ptr ] -> 纯净的连续函数指针，CPU 极速顺序预取
 ├── Delegate[] AsyncHandlers [ ptr | ptr | ptr | ptr ] -> 物理隔离，彻底消灭 if(isAsync) 的分支判断
 └── Circuit[]  FaultCircuits [ 0 | 0 | 1 | 0 ] -> 仅在抛出异常时才访问，绝不污染正常的热路径
```

通过这一转变，配合**硬件级位图跳跃 (Bitmask Skipping)** 和基于 `Unsafe` 的指针越界消除，LayerBase 清空了热路径上的 `if/else` 分支与数组边界检查。这使得它达到了与顶级 ECS 框架同级别的 L1/L2 缓存命中率。

---

## ⚡ 标准基准测试 (BenchmarkDotNet)

我们使用 `BenchmarkDotNet` 进行了基准压测。测试环境：`.NET 8.0`, `X64 RyuJIT`, `Intel Core i7-12650H`。

| 场景描述                                        | 任务量 (事件派发次数) | 平均耗时 (Mean) | 等效处理量 (次/秒) | GC 内存分配 (Allocated) |
|-------------------------------------------------|----------------------:|----------------:|----------------------:|------------------------:|
| **轻度负载 (模拟真实业务)** <br> 10 层架构，单层订阅   | **1,000,000** 次      | **6.38 ms**     | **~156,700,000**      | **0 B** (热路径无分配)  |
| **极限高压 (全链路测试)** <br> 10 层架构，层层都订阅 | **1,000,000** 次      | **16.81 ms**    | **~59,400,000**       | **0 B** (热路径无分配)  |
| **1ms 挑战 (常见3层架构)** <br> 3 层架构全订阅       | **10,000** 次         | **91.41 μs**    | **~109,300,000**      | **0 B** (热路径无分配)  |

> 💡 **数据解读**：在包含 10 个物理层级的真实业务架构下，发送一百万次事件仅需 6 毫秒。即便在每一层都强制挂载逻辑的极端高压环境下，系统依然维持了惊人的吞吐量。对于性能预算极其吃紧的高频动作游戏或服务器网关而言，LayerBase 的架构调度损耗已无限趋近于 0。

---

## 🛡️ 工业级基建：让项目跑得更稳

跑得快是基础，跑得稳才是活下去的关键。LayerBase 提供了完善的工程保障：

- **自愈熔断机制**：任何 Handler 抛出未捕获的异常，系统会精准、物理地熔断该节点。局部故障绝不阻塞同层其他业务，引擎会在下一帧通过“两段式零分配重建”平滑剔除失效节点，实现系统自愈。
- **零开销异步生态 (`LBTask`)**：现代游戏逻辑大量依赖异步。LayerBase 自带专为游戏帧循环（Pump）优化的 `LBTask`。在同步完成的路径下，实现 **0 GC 分配**，让异步逻辑与同步流无缝融合。
- **死循环静态防卫**：在调用 `Build()` 构建拓扑时，底层算法会静态审计整个游戏的事件流向图。若发现死循环风险（例如 A 触发 B，B 同步触发 A），控制台会直接抛出环路路径，拒绝黑盒运行。

---

## 📦 安装指南

1. **源码引入**：将仓库中的 `LayerBase` 和 `LayerBase.Task` 项目目录直接添加到您的解决方案中并建立引用。
2. **配置源生成器 (Source Generator)**：
   框架重度依赖源生成器以消除反射开销，请务必确保配置了 `LayerBase.Generator` 分析器：
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
3. **环境要求**：完美支持 `.NET Standard 2.1`（对 Unity/Godot 极度友好），建议在 `.NET 8.0/9.0` 环境运行以完全解锁底层 `Unsafe` 加速特性。

---

## 📖 最佳实践手册

LayerBase 推荐使用 `Layer -> Service -> Manager` 的三层架构。结合特性的自动装配，让您的代码结构清晰、静态可溯源。

### Step 1: 定义您的事件 (Event Structs)
为了杜绝运行时 GC 垃圾，所有的事件必须声明为 `struct`：

```csharp
public struct DamageEvent
{
    public int TargetId;
    public float Amount;
}
public struct PlayerDeathEvent { }
```

### Step 2: 编写具体的业务逻辑 (Manager)
Manager 专注于单一的垂直业务。继承 `ILayerContext` 即可感知自身所属的层级，并解锁所有 `Send` 与 `Post` 派发能力。
推荐使用 `[Subscribe]` 和 `[SubscribeAsync]` 特性，配合源生成器在编译期生成绑定代码。

```csharp
using LayerBase.DI;
using LayerBase.Core.Event;
using LayerBase.Async;

// 必须标记 partial，供源生成器植入代码
public partial class DamageManager : ILayerContext
{
    // 【同步处理】
    [Subscribe]
    private EventHandledState OnTakeDamage(in DamageEvent e)
    {
        Console.WriteLine($"实体 {e.TargetId} 受到 {e.Amount} 伤害");
        
        if (e.Amount > 100)
        {
            // 向下坠落，将事件安全地传递给更底层的逻辑层
            this.SendDrop(new PlayerDeathEvent()); 
        }
        
        // Continue: 允许同层或后续层级的其他 Manager 继续监听该事件
        // Handled: 立即截断该事件的传播
        return EventHandledState.Continue;
    }

    // 【异步处理】：完美融合分帧计时
    [SubscribeAsync]
    private async LBTask OnPlayerDeath(PlayerDeathEvent e)
    {
        await LBTask.Delay(TimeSpan.FromSeconds(3f)); // 零 GC 延迟
        Console.WriteLine("玩家复活...");
    }
}
```

### Step 3: 组织业务模块 (Service)
Service 负责将相关的 Manager 组合起来，并挂载到指定的 Layer 中。
通过 `[OwnerLayer]` 特性，您可以将一个服务静态绑定到特定的 Layer 上。这种约束让接手代码的同事能一眼看清模块的归属。

```csharp
using LayerBase.DI;

// 声明该服务归属于 GameLogicLayer 层级
[OwnerLayer(typeof(GameLogicLayer))]
public class CombatService : IService 
{
    // 配置依赖注入，将 Manager 注册进该层级
    public void ConfigureServices(IServiceCollection services) 
    { 
        // 注册的顺序，即为该层级内事件响应的优先级顺序
        services.AddSingleton<DamageManager, DamageManager>();
    }
}
```

### Step 4: 定义层级容器 (Layer) & 初始化拓扑 (LayerHub)
Layer 是最宏观的物理或逻辑屏障。在游戏入口处，初始化您的架构拓扑。

```csharp
using LayerBase.Layers;
using LayerBase.LayerHub;

// 定义层级
public class UILayer : Layer { }
public class GameLogicLayer : Layer { }

// 在游戏 Awake / _Ready 中构建
public void InitGame()
{
    LayerHub.CreateLayers()
            .Push(new UILayer())          // Index 0: 顶层 (最先收到 Bubble, 最后收到 Drop)
            .Push(new GameLogicLayer())   // Index 1: 底层
            .Build();                     // Build() 会扫描所有标有 [OwnerLayer] 的服务并自动组装
}
```

### Step 5: 触发事件与驱动循环 (Send/Post/Pump)

**派发事件：**
```csharp
// 【同步执行】全局广播，立即阻塞当前线程并按序穿透所有层级
LayerHub.Send(new DamageEvent { TargetId = 99, Amount = 50f });

// 【异步投递】将事件推入脏队列，等待下一帧的 Pump 处理，不阻塞当前执行流
LayerHub.Post(new DamageEvent { TargetId = 1, Amount = 10f });
```

**驱动主循环：**
如果您使用了 `Post`、`LBTask` 或延迟任务，请在游戏的主循环中驱动 Pump。在没有事件挂起时，此调用的耗时仅为几个纳秒。
```csharp
void Update(float deltaTime)
{
    LayerHub.Pump(deltaTime);
}
```

---

## 🛠 进阶特性：应对复杂场景

### 1. 流式过滤与拦截 (Fluent API)
除了特性自动绑定，LayerBase 提供了流畅的链式 API。您可以在 `Initialize` 接口中手动调用。
其核心优势在于：拦截判定发生在路由的**最早期闭包内**。不符合条件的事件会被直接拒之门外，避免唤醒庞大的业务逻辑块。

```csharp
public partial class PlayerManager : ILayerContext
{
    private int _myEntityId = 10;

    public void Initialize()
    {
        // 优雅的流式拦截
        this.OnEvent<DamageEvent>()
            .Where((in DamageEvent e) => e.TargetId == _myEntityId) // 编译期拦截
            .Handle((in DamageEvent e) => 
            {
                // 处理受击逻辑...
                return EventHandledState.Handled;
            });
    }
}
```

### 2. 后台并行处理 (Parallel Handlers)
如果遇到极度消耗 CPU 且**无需修改主线程对象状态**的纯计算逻辑（如复杂的寻路下发、日志落盘），并行订阅是最佳选择。事件会被投递至无锁队列，交由 ThreadPool 异步吞吐，确保主帧丝滑。

```csharp
[SubscribeParallel]
private EventHandledState OnHeavyComputeTask(in ComputeEvent e)
{
    // 在多线程环境中调度执行
    return EventHandledState.Continue;
}
```

### 3. 拓扑可视化审计 (Topology Audit)
开启 Debug 模式后，您可以随时调用 `GetTopologySummary()`，在控制台打印出一张结构严密的文本拓扑图。整个系统“谁派发了什么事件，谁又监听了什么事件”一览无余，让架构如同白盒般透明。