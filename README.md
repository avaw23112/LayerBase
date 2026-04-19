[English](#english) | [中文](#中文)

<a id="中文"></a>
# 🚀 LayerBase: 面向数据的高性能 C# 游戏架构总线

**LayerBase** 是一款专为 Unity、Godot 及纯 C# 服务端打造的高性能事件架构与通讯总线框架。

它打破了传统面向对象（OOP）事件总线的性能瓶颈，在底层采用了**面向数据设计（Data-Oriented Design, DOD）**与**SOA（Structure of Arrays）**内存布局。在保证业务代码极简、解耦的同时，为中大型项目提供规范的事件流转控制，实测单核 TPS 达到 **1.5 亿次/秒** 的物理级吞吐量。

---

## 🤔 架构演进：我们为什么需要 LayerBase？

在游戏项目的生命周期中，业务复杂度的增长往往会推动通讯架构的演进。回顾常见的技术选型，我们可以清晰地看到痛点所在：

### 1. 单例模式的耦合困境
在项目初期，直接调用是最直观的方式（例如 `GameManager.Instance.UpdateHealth()`）。但在中大型项目中，当系统里存在成百上千个 Manager 时，这种方式会促使代码演变为极度复杂的网状引用。模块间的紧密耦合让重构和独立测试变得举步维艰。

### 2. 传统事件总线（EventBus）的时序与性能瓶颈
为了实现模块解耦，业界普遍引入了 `Action` 委托、`UniRx` 或是泛型 `EventBus`。这确实切断了硬引用，但同时也引入了两个更为隐蔽的工程问题：

*   **隐式的时序陷阱**：在缺乏统一架构指导的情况下，开发者通常会在各个组件的生命周期（如 `Awake`、`Start`）中分散注册事件。这种**无序的注册行为**导致事件响应的先后顺序成为黑盒。当一个事件抛出时，你无法确保数据结算与 UI 表现的确定性顺序，这极易引发偶发性 Bug。
*   **底层的性能暗礁**：传统事件总线的底层普遍依赖 `Dictionary<Type, List<Delegate>>`。在面临海量实体（Entity）的高频交互时，字典的哈希计算、委托链的遍历，以及最关键的——**缺乏内存连续性导致的 CPU 缓存未命中（Cache Miss）**，通常会将系统的处理上限限制在百万级 TPS。

### 3. 破局：秩序与极速的重构
LayerBase 的设计哲学是：**用强约束的框架收编混乱的注册，用底层的面向数据重构打破性能的枷锁。**

1.  **在宏观架构上**：摒弃随地订阅的模式，引入 `Layer -> Service -> Manager` 三层递进的架构。通过依赖注入（DI）和明确的拓扑层级，让事件的流向和处理顺序重新回归绝对的确定性。
2.  **在底层执行上**：汲取 ECS 框架的核心思想，在底层采用纯粹的 SOA 数组布局进行事件路由。使得这一架构不仅规范了代码，更在性能上达到了与顶级 C++/C# ECS 框架同级别的缓存命中率。

---

## ⚡ 标准基准测试 (BenchmarkDotNet)

### 1. 原始测试报表 (Original Benchmark Report)

测试环境：`BenchmarkDotNet v0.15.8`, `Windows 11`, `.NET 8.0 (X64 RyuJIT)`, `Intel Core i7-12650H`.

| Type | Method | Mean | Error | StdDev | Allocated |
|--- |--- |---:|---:|---:|---:|
| **Classic_1ms_Bench** | **'经典 1ms 挑战 (3层全订阅) - 1万次'** | **61.32 us** | **1.223 us** | **2.415 us** | **-** |
| **Extreme_Empty_64_Bench** | **'极限空负载 (64层/0订阅) - 100万次'** | **1,516.91 us** | **30.175 us** | **64.955 us** | **-** |
| **MultiLayer_Full_Bench** | **'多层高压 (10层/全订阅) - 100万次'** | **9,170.90 us** | **180.520 us** | **306.536 us** | **-** |
| **MultiLayer_Low_Bench** | **'多层低压 (10层/仅尾层) - 100万次'** | **4,598.08 us** | **91.886 us** | **197.794 us** | **-** |
| **MultiLayer_Random_Bench** | **'多层随机负载 (10层/5层订阅) - 100万次'** | **6,222.94 us** | **123.069 us** | **272.714 us** | **-** |
| **SingleLayer_High_Bench** | **'单层高压 (1层/10订阅) - 100万次'** | **9,053.39 us** | **174.913 us** | **460.791 us** | **-** |
| **SingleLayer_Low_Bench** | **'单层低压 (1层/1订阅) - 100万次'** | **4,453.57 us** | **87.153 us** | **169.985 us** | **-** |
| **Typical_Heavy_180_Bench** | **'中重度负载 (180订阅) - 1万次'** | **894.46 us** | **17.772 us** | **44.260 us** | **-** |

### 2. 性能大图景 (Performance Analysis Map)

我们将原始耗时换算为每秒吞吐量（TPS），以更直观地展示框架的吞吐红利：

| 核心维度 | 关键指标 | 实测表现 | 换算 TPS (每秒吞吐) | 工程价值 |
| :--- | :--- | :--- | :--- | :--- |
| **单点爆发力** | 单次分发极限 | **4.45 ns** | **🚀 2.24 亿次/秒** | 榨干了托管环境分发的物理极限 |
| **复杂逻辑吞吐** | 180 订阅负载 | **89.4 ns** | **🚀 1118 万次/秒** | 支撑极复杂业务下的实时响应 |
| **层级穿透力** | 64层深路由 | **1.51 ns** | **🚀 6.59 亿次/秒** | 证明分层位图过滤近乎零损耗 |
| **实时性保障** | 1万次 1ms 挑战 | **61.32 us** | **🚀 1.63 亿次/秒** | 复杂传递对逻辑层完全透明 |

---

## 📊 直观剖析：我们为什么这么快？

为了突破传统架构的性能天花板，LayerBase 在底层实施了全方位的物理级优化：

### 1. SOA 面向数据布局 (Structure of Arrays)
传统的 EventBus 在内存中是典型的 **AOS (Array of Structures)** 布局。派发事件时，CPU 必须在堆内存中进行多次非连续跳转：

```text
❌ 传统 EventBus 分发路径 (面临严重的 Cache Miss)
EventBus 
  └─> [哈希计算定位 Bucket] 
        └─> [读取 List 内存块] 
              └─> [跳转至 Handler 对象内存 (包含上下文/委托)] 
                    └─> 虚函数 Invoke 
```

而在 LayerBase 中，系统在构建期（Build）通过源生成器，将同类事件的所有处理器“拆解并脱水”，转化为连续的原生数组（**SOA 布局**）：

```text
✅ LayerBase 零分支分发引擎 (完美的 Cache 亲和性)
EventBucket<T>
 ├── Delegate[] SyncHandlers  [ ptr | ptr | ptr | ptr ] -> 纯净的连续函数指针，CPU 极速顺序预取
 ├── Delegate[] AsyncHandlers [ ptr | ptr | ptr | ptr ] -> 物理隔离同步与异步，消灭分支判断
 └── Circuit[]  FaultCircuits [ 0 | 0 | 1 | 0 ] -> 仅在抛出异常时才访问，绝不污染热路径
```
由于热路径中只剩下紧凑的委托指针，CPU L1/L2 缓存可以实现近乎完美的顺序预取（Prefetching）。

### 2. 硬件级位图跳跃 (Bitmask Skipping)
在跨层级分发（如全局广播）时，LayerBase 不遍历任何字典。每个层级的活跃状态被映射进一个 `ulong` 整数中。利用现代 CPU 指令（`BitOperations.TrailingZeroCount`），**仅需 1 个时钟周期的位运算**，即可精准计算出下一个存在订阅者的层级，将层级间的跳转开销降至最低。

### 3. 无分支与越界消除 (Branchless & Unsafe Offsets)
*   **位运算状态合并**：在核心循环中，将多个 Handler 的返回状态通过按位或（`|`）合并，大幅压缩了分支预测指令（Branch Prediction）。
*   **指针偏移**：在支持的运行时下，底层直接获取数组的原生指针并通过 `Unsafe.Add` 步进，彻底消除了 JIT 在循环内的数组边界检查（BCE）。

---

## 🛡️ 工业级基建保障

除了追求极致的运行效率，LayerBase 在工程稳健性上也提供了全套设施：

- **自愈熔断机制**：当事件 Handler 抛出未捕获异常时，系统会精准定位并物理熔断该节点，局部故障绝不阻塞同层其他业务。在下一帧，引擎通过“两段式零分配重建（Two-Pass Zero-Allocation Rebuild）”平滑剔除失效节点，实现系统自愈。
- **零分配异步生态 (`LBTask`)**：现代游戏开发高度依赖异步操作。框架内置了专为游戏帧循环调优的 `LBTask` 结构体任务。在同步完成路径下可实现 **0 GC 堆分配**，让异步逻辑免除内存抖动困扰。
- **静态拓扑审计**：在调用 `Build()` 构建层级时，底层的着色图算法（Three-Color Algorithm）会静态扫描整个事件网络。若发现**同步死循环**风险，控制台将直接打印环路警告，拒绝黑盒运行。

---

## 📦 安装指南

1. **NuGet 快速安装 (推荐)**：
   您可以通过 NuGet 包管理器直接安装 LayerBase：
   ```bash
   dotnet add package LayerBase --version 1.3.2
   ```
2. **源码引入**：将仓库中的 `LayerBase` 和 `LayerBase.Task` 项目目录直接添加到您的解决方案中并建立引用。
3. **配置源生成器 (Source Generator)**：
   框架依赖源生成器以实现零反射的特性自动绑定，请确保在主项目中引入了分析器：
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
4. **环境要求**：支持 `.NET Standard 2.1`（完美兼容 Unity/Godot），建议在 `.NET 8.0/9.0` 环境下运行以解锁完整的 `Unsafe` 硬件加速。

---

## 📖 最佳实践手册：构建清晰的架构

在大型项目中，扁平架构容易导致模块依赖复杂化。LayerBase 的三层结构旨在通过空间隔离来管理这种复杂性：

*   🌍 **Layer（宏观层级）**：**处理优先级与物理界限。**
    *   **职责**：代表系统的不同层面（如 `RenderLayer`、`PhysicsLayer`、`CoreLogicLayer`）。它不包含具体业务逻辑，主要用于定义事件流向（Bubble 向上，Drop 向下）的边界，确保整体系统时序的确定性。
*   🏢 **Service（业务服务）**：**功能聚合与调度。**
    *   **职责**：将相关联的功能模块聚合在一起（例如 `PlayerService` 聚合输入、移动、动画等）。它负责依赖注入（DI）配置，对外暴露粗粒度接口，对内管理 Manager，实现高内聚。
*   ⚙️ **Manager（具体逻辑块）**：**具体业务的承载者。**
    *   **职责**：遵循单一职责原则 (SRP)，实现具体的微观功能（如处理伤害计算）。Manager 之间尽量避免直接引用，而是通过事件总线进行通讯，实现低耦合。

通过这种结构，项目的代码目录结构能够清晰地反映其系统架构。

---

### Step 1: 定义事件 (Event Structs)
为避免在事件频繁触发时产生 GC 压力，框架要求所有的事件对象必须声明为 `struct`：

```csharp
public struct DamageEvent
{
    public int TargetId;
    public float Amount;
}
public struct PlayerDeathEvent { }
```

### Step 2: 编写业务逻辑 (Manager)
Manager 继承自 `ILayerContext`，能够感知自身所处的层级，并具备事件发送与接收能力。推荐使用特性（Attribute）进行绑定，编译器会自动生成关联代码以避免反射开销。

```csharp
using LayerBase.DI;
using LayerBase.Core.Event;
using LayerBase.Async;

// 类需标记为 partial 配合源生成器
public partial class DamageManager : ILayerContext
{
    // 【同步事件处理】：使用 [Subscribe] 特性
    [Subscribe]
    private EventHandledState OnTakeDamage(in DamageEvent e)
    {
        // 业务逻辑处理
        
        if (e.Amount > 100)
        {
            // 向下坠落：将事件传递给更底层的系统
            this.SendDrop(new PlayerDeathEvent()); 
        }
        
        // 返回 Continue: 允许其他同事件的 Manager 继续处理
        // 返回 Handled: 截断该事件的后续传播
        return EventHandledState.Continue;
    }

    // 【异步事件处理】：使用 [SubscribeAsync] 特性
    [SubscribeAsync]
    private async LBTask OnPlayerDeath(PlayerDeathEvent e)
    {
        // 支持使用 LBTask.Delay 进行无 GC 延迟
        await LBTask.Delay(TimeSpan.FromSeconds(3f)); 
    }
}
```

### Step 3: 组织业务模块 (Service)
Service 负责将相关的 Manager 注册到 DI 容器中。
通过 `[OwnerLayer]` 特性，可以将 Service 静态绑定到指定的 Layer 层级。

```csharp
using LayerBase.DI;

// 绑定至 GameLogicLayer 层级
[OwnerLayer(typeof(GameLogicLayer))]
public class CombatService : IService 
{
    public void ConfigureServices(IServiceCollection services) 
    { 
        // 注册 Manager。此处注册的先后顺序即决定了同层级内事件响应的优先级。
        services.AddSingleton<DamageManager, DamageManager>();
    }
}
```

### Step 4: 空间维度的事件触发
LayerBase 提供了精确控制传播方向的 API。在 `Layer`、`Service` 或 `Manager` 内部，您可以直接调用扩展方法来派发事件：

```csharp
// 【Send 族：同步执行，当前执行流会等待分发完成】
this.SendLocal(new DamageEvent());  // 【同层】仅在当前层级广播
this.SendBubble(new DamageEvent()); // 【冒泡】向索引更小的上层抛出（如逻辑层发往UI层）
this.SendDrop(new DamageEvent());   // 【坠落】向索引更大的下层抛出（如UI层发往逻辑层）
this.SendGlobal(new DamageEvent()); // 【全局】穿透所有层级广播

// 【Post 族：异步投递，将事件压入队列，由下一帧的 Pump 处理】
this.PostBubble(new DamageEvent());
this.PostGlobal(new DamageEvent()); 

// 【Delay 族：定时投递】
this.DelayDrop(new PlayerDeathEvent(), 3.5f); // 在指定秒数后向下层级派发
```
这种设计有助于避免无向广播可能导致的循环触发和不必要的性能消耗。

### Step 5: 引擎生命周期整合
将 LayerBase 接入到具体的游戏引擎（如 Unity 的 `MonoBehaviour`）时，需处理两个关键生命周期：构建（Build）与心跳驱动（Pump）。

*   **Build**：在游戏初始化阶段调用，完成特性的扫描、SOA 数组分配及静态死循环审计。
*   **Pump**：在每帧更新阶段调用，用于处理队列中的异步事件和时间延迟任务。

```csharp
using UnityEngine;
using LayerBase.Layers;
using LayerBase.LayerHub;

public class GameRoot : MonoBehaviour
{
    // 定义层级
    public class InteractionLayer : Layer { }
    public class CoreLogicLayer : Layer { }

    void Awake()
    {
        // 1. 初始化期：构建拓扑
        LayerHub.CreateLayers()
                .Push(new InteractionLayer()) // 索引 0: 上层交互
                .Push(new CoreLogicLayer())   // 索引 1: 下层逻辑
                .SetDebug()                   // 开启Debug模式可以从GetTopologySummary()获得整个系统的构建图。对性能影响不大。
                .Build();                     // 自动扫描 [OwnerLayer] 并装配

        //注册全局消息通道，用于捕获框架的异常
        LayerHub.OnLayerEventInfo +=
            info =>
            {
                GD.PrintErr($"[{info.LayerIndex}][{info.EventName}][{info.Type}]: {info.Source}{info.Message}");
            };
    }

    void Update()
    {
        // 2. 运行期：驱动事件泵
        // 在空闲状态下，Pump 的调用开销极小
        LayerHub.Pump(Time.deltaTime);
    }
}
```

---

## 🛠 进阶特性指南

### 1. 流式过滤与拦截 (Fluent API)
对于需要动态控制订阅条件的场景，LayerBase 提供了优雅的链式 API。它最大的优势在于：拦截条件会在路由的最早期（包装委托内部）执行，不符合条件的事件会被直接短路，避免了不必要的函数调用。

```csharp
public partial class PlayerManager : ILayerContext
{
    private int _myEntityId = 10;

    public void Initialize()
    {
        // 链式调用：订阅 -> 过滤 -> 处理
        this.OnEvent<DamageEvent>()
            .Where((in DamageEvent e) => e.TargetId == _myEntityId) 
            .Handle((in DamageEvent e) => 
            {
                // 处理受击...
                return EventHandledState.Handled;
            });
    }
}
```

### 2. 后台并行处理 (Parallel Handlers)
当面临高 CPU 消耗且**不依赖/不修改主线程状态**的纯计算逻辑（如寻路数据打包、耗时日志序列化）时，可使用并行订阅。事件将进入无锁队列并由 ThreadPool 在后台异步消化，保障主线程的帧率稳定。

```csharp
// 通过特性快速绑定并行方法
[SubscribeParallel]
private EventHandledState OnHeavyComputeTask(in ComputeEvent e)
{
    // 该方法在多线程环境中被安全调度
    return EventHandledState.Continue;
}
```

### 3. 拓扑结构可视化 (Topology Audit)
开启 Debug 模式后，调用 `GetTopologySummary()` 即可在控制台输出一张清晰的文本结构图，展示整个系统内各个 Layer 挂载了哪些 Manager，以及它们具体订阅/派发了什么事件。这在大型项目中是排查系统耦合度不可或缺的工具。

### 4. 切片式独立处理器 (Standalone EventHandler)
有时候，您可能有一段非常独立的逻辑，它只处理某一个特定事件，将其塞入一个庞大的 Manager 显得过于臃肿。LayerBase 支持“切片式”的独立处理器。
只需实现 `IEventHandler<T>` 或 `IEventHandlerAsync<T>` 接口，并挂载 `[OwnerLayer]` 特性，源生成器就会在编译期自动将其注册到对应的层级中：

```csharp
// 这是一个独立的、切片式的事件处理器
[OwnerLayer(typeof(GameLogicLayer))] 
public class PlayerDamageHandler : IEventHandler<DamageEvent>
{
    public EventHandledState Deal(in DamageEvent e)
    {
        // 专注处理伤害逻辑...
        return EventHandledState.Continue;
    }
}
```

### 5. 事件元数据与全局异常观察 (Event MetaData)
对于某些核心事件（如网络同步包或核心状态流转），如果在分发过程中有任何 Handler 抛出了异常，我们通常希望能在全局第一时间捕获，以进行统一的日志打点或崩溃上报。

LayerBase 提供了一套**无侵入式、零反射**的元数据（MetaData）注册方案：
您只需定义一个继承自 `EventMetaData<T>` 的类。源生成器会在编译期自动将其与您的事件绑定（**注：要求事件 `struct` 必须声明为 `partial`**）。

```csharp
// 1. 事件必须声明为 partial struct
public partial struct CoreSyncEvent 
{ 
    public byte[] Data; 
}

// 2. 定义该事件的元数据配置
public class CoreSyncEventMetaData : EventMetaData<CoreSyncEvent>
{
    // [可选]：定义事件所属的分类树，方便进行拓扑分类或模块检索
    public override EventCategoryToken Category => EventCatalogue.Path("Network", "Sync").GetToken();

    // 🏆 全局异常拦截点
    // 任何 Handler 在处理 CoreSyncEvent 抛出异常时，都会自动路由到这里！
    public override void OnEventExpectation(CoreSyncEvent e, Exception exception)
    {
        // 在这里进行统一的容错处理、日志打点或崩溃上报
        Console.WriteLine($"[严重错误] 同步事件处理失败: {exception.Message}");
    }
}
```

---
---

<a id="english"></a>
# 🚀 LayerBase: Data-Oriented High-Performance C# Game Architecture Bus

**LayerBase** is a high-performance event architecture and communication bus framework tailored for Unity, Godot, and pure C# servers.

It shatters the performance bottlenecks of traditional Object-Oriented Programming (OOP) event buses by adopting **Data-Oriented Design (DOD)** and **SOA (Structure of Arrays)** memory layout at its core. While keeping your business code minimal and decoupled, it provides standardized event flow control for medium-to-large projects, achieving an impressive physical throughput of **over 150 million TPS (Transactions Per Second)** on a single core.

---

## 🤔 Architecture Evolution: Why Do We Need LayerBase?

Throughout the lifecycle of a game project, the growth in business complexity inevitably drives the evolution of the communication architecture. Looking back at common technology choices, the pain points become clear:

### 1. The Singleton Coupling Dilemma
In the early stages of a project, direct invocation is the most intuitive approach (e.g., `GameManager.Instance.UpdateHealth()`). However, in medium-to-large projects, when hundreds or thousands of Managers exist, this approach degrades into an extremely complex web of references. The tight coupling between modules makes refactoring and isolated testing nearly impossible.

### 2. The Timing and Performance Bottlenecks of Traditional EventBuses
To decouple modules, the industry widely adopted `Action` delegates, `UniRx`, or various generic `EventBus` implementations. While this cuts off hard references, it introduces two hidden engineering disasters:

*   **Implicit Timing Traps**: Without a unified architectural guideline, developers usually register events scattered across the lifecycles of various components (like `Awake` or `Start`). This **unordered registration behavior** turns the execution order of event responses into a black box. When an event is fired, you cannot guarantee the deterministic order of data settlement versus UI updates, making it a hotbed for sporadic bugs.
*   **Underlying Performance Reefs**: The core implementation of traditional event buses generally relies on `Dictionary<Type, List<Delegate>>`. When faced with high-frequency interactions among massive numbers of entities, frequent dictionary hashing, delegate chain iteration, and crucially—**CPU Cache Misses caused by the lack of memory contiguity**—often restrict the system's processing ceiling to just a few million TPS.

### 3. The Breakthrough: Reconstructing Order and Speed
LayerBase's design philosophy is: **Tame chaotic registrations with a strongly constrained framework, and shatter performance shackles with data-oriented reconstruction.**

1.  **On Macro Architecture**: Abandon the free-for-all subscription pattern and introduce a strongly constrained `Layer -> Service -> Manager` three-tier progressive architecture. Through Dependency Injection (DI) and explicit topology layers, the flow and processing order of events return to absolute determinism.
2.  **On Micro Execution**: Draw inspiration from ECS frameworks. Adopt pure SOA array layouts for event routing at the lowest level. This ensures that the architecture not only standardizes the code but also achieves cache hit rates on par with top-tier C++/C# ECS frameworks.

---

## 📊 Visual Breakdown: Why Are We So Fast?

To break through the performance ceiling of traditional architectures, LayerBase implements comprehensive physical-level optimizations at the bottom layer:

### 1. SOA Data-Oriented Layout (Structure of Arrays)
A traditional EventBus uses a typical **AOS (Array of Structures)** layout in memory. When dispatching an event, the CPU must perform multiple expensive, non-contiguous memory jumps:

```text
❌ Traditional EventBus Dispatch Path (Severe Cache Misses)
EventBus 
  └─> [Hash calculation to locate Bucket] 
        └─> [Read List memory block] 
              └─> [Jump to Handler Object memory (Contains Context/Delegate)] 
                    └─> Virtual function Invoke 
```

In contrast, LayerBase uses source generators during the build phase to "dismantle and dehydrate" all handlers for the same event type, transforming them into contiguous native arrays (**SOA Layout**):

```text
✅ LayerBase Branchless Dispatch Engine (Perfect Cache Affinity)
EventBucket<T>
 ├── Delegate[] SyncHandlers  [ ptr | ptr | ptr | ptr ] -> Pure contiguous function pointers, CPU ultra-fast sequential prefetch
 ├── Delegate[] AsyncHandlers [ ptr | ptr | ptr | ptr ] -> Physically isolated sync & async, eliminating branch checks
 └── Circuit[]  FaultCircuits [ 0 | 0 | 1 | 0 ] -> Accessed ONLY when exceptions are thrown, never polluting the hot path
```
Since only compact delegate pointers remain in the hot path, the CPU's L1/L2 cache achieves near-perfect sequential prefetching.

### 2. Hardware-Level Bitmask Skipping
When dispatching across layers (e.g., global broadcast), LayerBase does not iterate through any dictionaries. The active state of each layer is mapped into a single `ulong` integer. Utilizing modern CPU instructions (`BitOperations.TrailingZeroCount`), **a single clock cycle of bitwise operation** can precisely calculate the next layer containing a subscriber, minimizing inter-layer jump overhead.

### 3. Branchless and Unsafe Bounds Check Elimination
*   **Bitwise State Aggregation**: In the core loop, the return states of multiple Handlers are merged using bitwise OR (`|`), drastically compressing branch prediction instructions.
*   **Pointer Offsets**: In supported runtimes, the underlying engine directly acquires the native pointer of the array and steps through it via `Unsafe.Add`, completely eliminating JIT array bounds check (BCE) instructions inside the loop.

---

## ⚡ Standard Benchmarks (BenchmarkDotNet)

Testing Environment: `BenchmarkDotNet v0.15.8`, `Windows 11`, `.NET 8.0 (X64 RyuJIT)`, `Intel Core i7-12650H`.

| Method | Mean | Error | StdDev | Gen0 | Gen1 | Gen2 | Allocated |
|------------------------------- |-------------:|-----------:|-------------:|--------:|--------:|-------:|----------:|
| 'Single Layer Low Load (1 layer, 1 sub) - 1M times' | 8,197.72 us | 231.042 us | 681.233 us | - | - | - | 8.51 KB |
| 'Single Layer High Load (1 layer, 10 subs) - 1M times' | 19,898.70 us | 601.152 us | 1,763.076 us | - | - | - | 12.15 KB |
| 'Multi-layer Low Load (10 layers, tail sub only) - 1M times' | 7,462.16 us | 208.456 us | 604.768 us | - | - | - | 59.4 KB |
| 'Multi-layer Full Load (10 layers, sub on every layer) - 1M times'| 19,114.17 us | 536.572 us | 1,565.206 us | - | - | - | 67.75 KB |
| 'Multi-layer Random Load (10 layers, 5 random subs) - 1M times'| 13,634.22 us | 350.394 us | 1,033.144 us | - | - | - | 63.36 KB |
| 'Extreme Empty Load (64 layers, 0 subs) - 1M times' | 1,811.59 us | 36.105 us | 99.445 us | 44.9219 | 19.5313 | 7.8125 | 496.54 KB |
| 'Classic 1ms Challenge (3 tiers, fully subbed) - 10k times' | 97.83 us | 2.244 us | 6.617 us | 1.7090 | 0.1221 | - | 21.28 KB |
| 'Typical Mid-Heavy Load (5 tiers: 1 has 100 subs, 4 have 20) - 10k times' | 2,807.12 us | 55.601 us | 139.493 us | 7.8125 | - | - | 105.72 KB |

> 💡 **Data Interpretation**:
> *   **Allocated (Memory Allocation)**: All `Allocated` values in the table are **one-time memory overhead** generated during `GlobalSetup` (build phase) to initialize layer containers and pre-allocate underlying SOA arrays. In the **runtime hot path (Run)** of event dispatching, all scenarios achieved **0 GC Memory Allocation** (`-` denotes no GC triggered).
> *   **Throughput Limits**: Under the most common "10 Layers Low Load (head and tail subs only)" scenario, 1 million dispatches took merely ~7.4ms, equaling an astonishing throughput of **134 million times/sec**. This proves that the architectural routing overhead across 10 physical layers is practically zero.
> *   **High Load Resilience**: In a heavy-duty test mimicking medium-to-large projects ("5-tier architecture, single event triggering 180 Handlers"), routing one event and triggering 180 callbacks took about 0.28 microseconds. Dispatching 10,000 times (executing 1.8 million business callbacks) took only 2.8 milliseconds.
> *   **1ms Challenge**: In the mainstream 3-tier (UI-Logic-Data) fully-subscribed game architecture, dispatching 10,000 events consumes a mere 97.8 microseconds.
> *   **Zero-Cost Idling**: Under extreme empty loading with 64 layers, the routing probe time for 1 million events is less than 2ms, implying that a massive number of idle layers will not impose any burden on the main frame rate.

---

## 🛡️ Industrial-Grade Infrastructure Guarantees

Beyond pursuing extreme execution efficiency, LayerBase also provides a complete suite of facilities for engineering robustness:

- **Self-Healing Circuit Breaker**: When an event Handler throws an unhandled exception, the system instantly locates and physically trips the breaker for that specific node. Local failures will never block other businesses in the same layer. In the next frame, the engine smoothly purges the invalid node via a "Two-Pass Zero-Allocation Rebuild", achieving system self-healing.
- **Zero-Allocation Asynchronous Ecosystem (`LBTask`)**: Modern game development relies heavily on async operations. The framework comes with `LBTask`, a struct-based task model specifically tuned for the game loop. It achieves **0 GC Heap Allocation** on synchronously completed paths, freeing your async logic from memory jitter nightmares.
- **Static Topology Audit**: During `Build()` to construct the layers, the underlying Three-Color Algorithm statically scans the entire event network. If a **synchronous infinite loop** risk is detected, a loop warning is printed directly to the console, refusing black-box execution.

---

## 📦 Installation Guide

1. **Quick Install via NuGet (Recommended)**:
   You can easily install LayerBase through the NuGet Package Manager:
   ```bash
   dotnet add package LayerBase --version 1.3.2
   ```
2. **Source Code Integration**: Add the `LayerBase` and `LayerBase.Task` project directories from the repository directly to your solution and reference them.
3. **Configure Source Generator**:
   The framework relies on Source Generators to achieve zero-reflection attribute binding. Ensure the analyzer is referenced in your main project:
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
4. **Environment Requirements**: Supports `.NET Standard 2.1` (perfectly compatible with Unity/Godot). It's recommended to run in a `.NET 8.0/9.0` environment to unlock full hardware acceleration via `Unsafe`.

---

## 📖 Best Practices Manual: Building a Clear Architecture

In large projects, flat architectures easily lead to convoluted module dependencies. LayerBase's three-tier structure manages this complexity through spatial isolation:

*   🌍 **Layer (Macro Level)**: **Handles processing priorities and physical boundaries.**
    *   **Role**: Represents different tiers of the system (e.g., `RenderLayer`, `PhysicsLayer`, `CoreLogicLayer`). It contains no specific business logic but defines the boundaries for event flow (Bubble up, Drop down), ensuring determinism in overall system timing.
*   🏢 **Service (Business Service)**: **Function aggregation and scheduling.**
    *   **Role**: Groups related functional modules (e.g., `PlayerService` grouping input, movement, animation). It handles Dependency Injection (DI) configurations, exposes coarse-grained interfaces outwardly, and manages Managers inwardly to achieve high cohesion.
*   ⚙️ **Manager (Specific Logic Block)**: **The carrier of concrete business.**
    *   **Role**: Adheres to the Single Responsibility Principle (SRP) to implement a specific micro-feature (like handling damage calculations). Managers generally avoid referencing each other directly, communicating entirely through the event bus to achieve low coupling.

Through this structure, the project's directory layout transparently reflects its system architecture.

---

### Step 1: Define Events (Event Structs)
To avoid GC pressure when events are triggered frequently, the framework requires all event objects to be declared as `struct`:

```csharp
public struct DamageEvent
{
    public int TargetId;
    public float Amount;
}
public struct PlayerDeathEvent { }
```

### Step 2: Write Business Logic (Manager)
Managers inherit from `ILayerContext`, allowing them to perceive their layer and providing powerful event sending and receiving capabilities. It is highly recommended to use attributes for binding; the compiler will automatically generate the code to avoid reflection overhead.

```csharp
using LayerBase.DI;
using LayerBase.Core.Event;
using LayerBase.Async;

// The class must be marked as partial to work with the source generator
public partial class DamageManager : ILayerContext
{
    // [Synchronous Event Handling]: using the [Subscribe] attribute
    [Subscribe]
    private EventHandledState OnTakeDamage(in DamageEvent e)
    {
        // Business logic handling...
        
        if (e.Amount > 100)
        {
            // Drop down: Pass the event to lower-level systems
            this.SendDrop(new PlayerDeathEvent()); 
        }
        
        // Return Continue: allow other Managers in the same or subsequent layers to process this event
        // Return Handled: truncate further propagation of this event
        return EventHandledState.Continue;
    }

    // [Asynchronous Event Handling]: using the [SubscribeAsync] attribute
    [SubscribeAsync]
    private async LBTask OnPlayerDeath(PlayerDeathEvent e)
    {
        // Supports GC-free delays using LBTask.Delay
        await LBTask.Delay(TimeSpan.FromSeconds(3f)); 
    }
}
```

### Step 3: Organize Business Modules (Service)
Services are responsible for registering related Managers into the DI container.
By using the `[OwnerLayer]` attribute, a Service can be statically bound to a specific Layer.

```csharp
using LayerBase.DI;

// Bound to the GameLogicLayer
[OwnerLayer(typeof(GameLogicLayer))]
public class CombatService : IService 
{
    public void ConfigureServices(IServiceCollection services) 
    { 
        // Register Managers. The order of registration dictates the priority of event responses within this layer.
        services.AddSingleton<DamageManager, DamageManager>();
    }
}
```

### Step 4: Spatial Event Triggering
LayerBase provides APIs for precise propagation direction control. Within a `Layer`, `Service`, or `Manager`, you can call extension methods directly to dispatch events:

```csharp
// ⚔️ [Send Family: Synchronous execution, the current thread blocks until dispatch finishes]
this.SendLocal(new DamageEvent());  // [Local] Broadcast only within the current layer
this.SendBubble(new DamageEvent()); // [Bubble] Throw upward to a smaller index (e.g., Logic sending to UI)
this.SendDrop(new DamageEvent());   // [Drop] Throw downward to a larger index (e.g., UI sending to Logic)
this.SendGlobal(new DamageEvent()); // [Global] Penetrate and broadcast across all layers

// 📨 [Post Family: Asynchronous delivery, enqueues the event to be processed by the next Pump]
this.PostBubble(new DamageEvent());
this.PostGlobal(new DamageEvent()); 

// ⏳ [Delay Family: Timed delivery]
this.DelayDrop(new PlayerDeathEvent(), 3.5f); // Dispatch downward after specified seconds
```
This design helps prevent the circular triggers and unnecessary performance overhead caused by undirected global broadcasting.

### Step 5: Engine Lifecycle Integration
When integrating LayerBase into a specific game engine (e.g., Unity's `MonoBehaviour`), two key lifecycles must be handled: Build and Pump.

*   **Build**: Called during the game's initialization phase to scan attributes, allocate SOA arrays, and statically audit infinite loops.
*   **Pump**: Called in the per-frame update phase to process queued asynchronous events and delayed tasks.

```csharp
using UnityEngine;
using LayerBase.Layers;
using LayerBase.LayerHub;

public class GameRoot : MonoBehaviour
{
    // Define Layers
    public class InteractionLayer : Layer { }
    public class CoreLogicLayer : Layer { }

    void Awake()
    {
        // 1. Initialization phase: Construct topology
        LayerHub.CreateLayers()
                .Push(new InteractionLayer()) // Index 0: Upper Interaction Layer
                .Push(new CoreLogicLayer())   // Index 1: Lower Logic Layer
                .SetDebug()                   // Enable Debug mode to obtain the build graph via GetTopologySummary(). Minimal performance impact.
                .Build();                     // Automatically scan [OwnerLayer] and assemble

        // Register the global message channel to capture framework exceptions
        LayerHub.OnLayerEventInfo +=
            info =>
            {
                GD.PrintErr($"[{info.LayerIndex}][{info.EventName}][{info.Type}]: {info.Source}{info.Message}");
            };
    }

    void Update()
    {
        // 2. Runtime phase: Drive the event pump
        // Under idle states, the cost of calling Pump is negligible
        LayerHub.Pump(Time.deltaTime);
    }
}
```

---

## 🛠 Advanced Features Guide

### 1. Fluent Filtering and Interception (Fluent API)
For scenarios requiring dynamic subscription conditions, LayerBase offers an elegant chainable API. Its greatest advantage is that interception conditions are evaluated at the earliest possible stage in routing (inside the wrapper delegate). Unqualified events are short-circuited immediately, preventing the awakening of massive business logic blocks.

```csharp
public partial class PlayerManager : ILayerContext
{
    private int _myEntityId = 10;

    public void Initialize()
    {
        // Chainable calls: Subscribe -> Filter -> Handle
        this.OnEvent<DamageEvent>()
            .Where((in DamageEvent e) => e.TargetId == _myEntityId) 
            .Handle((in DamageEvent e) => 
            {
                // Handle damage...
                return EventHandledState.Handled;
            });
    }
}
```

### 2. Background Parallel Processing (Parallel Handlers)
When dealing with logic that consumes high CPU and **does not rely on or modify the main thread state** (such as pathfinding data packaging or heavy log serialization), parallel subscriptions can be utilized. Events will enter a lock-free queue and be processed asynchronously by the ThreadPool in the background, ensuring main thread frame rate stability.

```csharp
// Quickly bind parallel methods via attribute
[SubscribeParallel]
private EventHandledState OnHeavyComputeTask(in ComputeEvent e)
{
    // This method is safely scheduled in a multi-threaded environment
    return EventHandledState.Continue;
}
```

### 3. Topology Audit
By enabling Debug mode, you can call `GetTopologySummary()` to output a clear text structural graph in the console, showcasing which Managers are mounted on which Layers across the system, as well as what specific events they subscribe to or dispatch. This is an indispensable tool for debugging system coupling in large projects.

### 4. Slice-based Standalone Processors (Standalone EventHandler)
Sometimes you might have highly independent logic dedicated to a single event, making it too bloated to cram into a massive Manager. LayerBase supports "slice-based" standalone processors.
Simply implement the `IEventHandler<T>` or `IEventHandlerAsync<T>` interface and attach the `[OwnerLayer]` attribute. The source generator will automatically register it to the corresponding layer at compile time:

```csharp
// A standalone, slice-based event handler
[OwnerLayer(typeof(GameLogicLayer))] 
public class PlayerDamageHandler : IEventHandler<DamageEvent>
{
    public EventHandledState Deal(in DamageEvent e)
    {
        // Focus solely on damage logic...
        return EventHandledState.Continue;
    }
}
```

### 5. Event MetaData and Global Exception Observation
For critical events (like network sync packets or core state transitions), if any Handler throws an exception during dispatch, we generally want to catch it globally and immediately for unified logging or crash reporting.

LayerBase provides a **non-intrusive, zero-reflection** MetaData registration solution:
You simply define a class inheriting from `EventMetaData<T>`. The source generator will automatically bind it to your event at compile time (**Note: the event `struct` must be declared as `partial`**).

```csharp
// 1. The event must be declared as a partial struct
public partial struct CoreSyncEvent 
{ 
    public byte[] Data; 
}

// 2. Define the metadata configuration for the event
public class CoreSyncEventMetaData : EventMetaData<CoreSyncEvent>
{
    // [Optional]: Define a category tree for the event, useful for topological categorization or module retrieval
    public override EventCategoryToken Category => EventCatalogue.Path("Network", "Sync").GetToken();

    // 🏆 Global Exception Interception Point
    // Any unhandled exception thrown by a Handler processing CoreSyncEvent will be automatically routed here!
    public override void OnEventExpectation(CoreSyncEvent e, Exception exception)
    {
        // Handle fault tolerance, logging, or crash reporting here
        Console.WriteLine($"[Critical Error] Sync event processing failed: {exception.Message}");
    }
}
```