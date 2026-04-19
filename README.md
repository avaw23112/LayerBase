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

## ⚡ 标准基准测试 (BenchmarkDotNet)

测试环境：`.NET 8.0`, `X64 RyuJIT`, `Intel Core i7-12650H`。

| 场景描述                                        | 任务量 (事件派发次数) | 平均耗时 (Mean) | 等效处理量 (次/秒) | GC 内存分配 (Allocated) |
|-------------------------------------------------|----------------------:|----------------:|----------------------:|------------------------:|
| **轻度负载 (模拟真实业务)** <br> 10 层架构，单层订阅   | **1,000,000** 次      | **6.38 ms**     | **~156,700,000**      | **0 B** (热路径无分配)  |
| **极限高压 (全链路轰炸)** <br> 10 层架构，层层都订阅 | **1,000,000** 次      | **16.81 ms**    | **~59,400,000**       | **0 B** (热路径无分配)  |
| **1ms 挑战 (常见3层架构)** <br> 3 层架构全订阅       | **10,000** 次         | **91.41 μs**    | **~109,300,000**      | **0 B** (热路径无分配)  |

> 💡 **数据解读**：在包含 10 个物理层级的真实业务架构下，发送一百万次事件仅需 6 毫秒。即便在每一层都强行挂载逻辑的极端高压环境下，系统依然维持了约 6000 万的超高吞吐。对于性能预算极其吃紧的游戏主循环而言，LayerBase 的架构调度损耗已无限趋近于 0。

---

## 🛡️ 工业级基建保障

除了追求极致的运行效率，LayerBase 在工程稳健性上也提供了全套设施：

- **自愈熔断机制**：当事件 Handler 抛出未捕获异常时，系统会精准定位并物理熔断该节点，局部故障绝不阻塞同层其他业务。在下一帧，引擎通过“两段式零分配重建（Two-Pass Zero-Allocation Rebuild）”平滑剔除失效节点，实现系统自愈。
- **零分配异步生态 (`LBTask`)**：现代游戏开发高度依赖异步操作。框架内置了专为游戏帧循环调优的 `LBTask` 结构体任务。在同步完成路径下可实现 **0 GC 堆分配**，让异步逻辑免除内存抖动困扰。
- **静态拓扑审计**：在调用 `Build()` 构建层级时，底层的着色图算法（Three-Color Algorithm）会静态扫描整个事件网络。若发现**同步死循环**风险，控制台将直接打印环路警告，拒绝黑盒运行。

---

## 📦 安装指南

1. **源码引入**：将仓库中的 `LayerBase` 和 `LayerBase.Task` 项目目录直接添加到您的解决方案中并建立引用。
2. **配置源生成器 (Source Generator)**：
   框架依赖源生成器以实现零反射的特性自动绑定，请确保在主项目中引入了分析器：
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
3. **环境要求**：支持 `.NET Standard 2.1`（完美兼容 Unity/Godot），建议在 `.NET 8.0/9.0` 环境下运行以解锁完整的 `Unsafe` 硬件加速。

---

## 📖 最佳实践手册

LayerBase 强烈推荐使用 `Layer -> Service -> Manager` 的三层递进架构。这种结构能让代码保持高内聚、低耦合的健康形态，即使项目膨胀到百万行代码，依然清晰可溯源。

*   **Layer（层级）**：最顶层的宏观组织者，代表系统的处理优先级界限。负责将一整类性质相同的内容聚集起来（例如：`RenderLayer`、`PhysicsLayer`、`CoreLogicLayer`、`InteractionLayer`）。
*   **Service（服务）**：中层的业务组织者，负责将同属一个大功能的模块聚合在一起（例如：`PlayerService` 负责组织 `PlayerInput`、`PlayerMove`、`PlayerAnimation` 等模块）。它本身不写太多具体逻辑，而是负责依赖注入（DI）和模块调度。
*   **Manager（管理器）**：底层的具体承载者。遵循单一职责原则（SRP），专注于实现一个具体的微观功能（例如：处理角色受击）。它通过事件总线与外界通讯。

### Step 1: 定义您的事件 (Event Structs)
为避免产生运行时垃圾，框架强制要求所有的事件对象必须声明为 `struct`：

```csharp
public struct DamageEvent
{
    public int TargetId;
    public float Amount;
}
public struct PlayerDeathEvent { }
```

### Step 2: 编写具体的业务逻辑 (Manager)
Manager 专注于单一业务的实现。继承 `ILayerContext` 即可自动感知自身层级，并获得强大的事件处理能力。推荐使用 `[Subscribe]` 等特性，编译器将自动生成无反射的绑定逻辑。

```csharp
using LayerBase.DI;
using LayerBase.Core.Event;
using LayerBase.Async;

// 必须标记为 partial
public partial class DamageManager : ILayerContext
{
    // 【同步处理】：使用 [Subscribe] 特性绑定
    [Subscribe]
    private EventHandledState OnTakeDamage(in DamageEvent e)
    {
        Console.WriteLine($"实体 {e.TargetId} 受到 {e.Amount} 伤害");
        
        if (e.Amount > 100)
        {
            // 向下坠落：将事件安全传递给更底层的系统（如底层物理或核心逻辑层）
            this.SendDrop(new PlayerDeathEvent()); 
        }
        
        // 返回 Continue: 允许同层或后续层级的其他 Manager 接收此事件
        // 返回 Handled: 立即截断该事件在当前及后续层级的传播
        return EventHandledState.Continue;
    }

    // 【异步处理】：完美支持等待与游戏分帧
    [SubscribeAsync]
    private async LBTask OnPlayerDeath(PlayerDeathEvent e)
    {
        await LBTask.Delay(TimeSpan.FromSeconds(3f)); // 享受 0 GC 的非阻塞延迟
        Console.WriteLine("玩家复活...");
    }
}
```

### Step 3: 组织业务模块 (Service)
Service 扮演装配者的角色，将相关的 Manager 注册至所属层级。
使用 `[OwnerLayer]` 特性，能够将服务强约束到特定的物理层级上，使项目的结构一目了然。

```csharp
using LayerBase.DI;

// 静态约束：该服务必须运行在 GameLogicLayer 层级
[OwnerLayer(typeof(GameLogicLayer))]
public class CombatService : IService 
{
    public void ConfigureServices(IServiceCollection services) 
    { 
        // 提示：在此处注册 Manager 的先后顺序，决定了该层级内事件响应的优先级
        services.AddSingleton<DamageManager, DamageManager>();
    }
}
```

### Step 4: 触发事件 (全能的 Event API)
不仅是 `LayerHub`，你的 `Layer`、`Service` 甚至是底层的 `Manager`，都随时可以通过扩展方法调用极其丰富的事件 API 来与外界通讯：

```csharp
// 【Send 族：同步执行，立刻阻塞当前执行流】
this.SendGlobal(new DamageEvent()); // 穿透全局所有层级
this.SendLocal(new DamageEvent());  // 仅在自己所在的同层级内广播
this.SendBubble(new DamageEvent()); // 向上冒泡（发给比自己 Index 更小的顶层，如逻辑抛给 UI）
this.SendDrop(new DamageEvent());   // 向下坠落（发给比自己 Index 更大的底层，如 UI 下发给逻辑）

// 【Post 族：异步投递，不阻塞代码，推入脏队列等待 Pump 处理】
this.PostGlobal(new DamageEvent()); 
this.PostBubble(new DamageEvent());

// 【Delay 族：定时投递，支持延迟 N 秒后派发】
this.DelayDrop(new PlayerDeathEvent(), 3.5f); // 3.5秒后向下层级派发死亡事件
```

### Step 5: 游戏引擎整合 (Build & Pump)
将 LayerBase 的生命周期接入您所使用的引擎（如 Unity 的 `MonoBehaviour` 或 Godot 的 `Node`）。
所有的初始化操作（`Build`）应放在引擎的启动期，所有的事件心跳（`Pump`）应放在引擎的帧更新中。

```csharp
using UnityEngine;
using LayerBase.Layers;
using LayerBase.LayerHub;

public class GameRoot : MonoBehaviour
{
    // 定义层级标识
    public class InteractionLayer : Layer { }
    public class CoreLogicLayer : Layer { }

    void Awake()
    {
        // 1. 初始化架构拓扑
        LayerHub.CreateLayers()
                .Push(new InteractionLayer()) // Index 0: 顶层交互层
                .Push(new CoreLogicLayer())   // Index 1: 核心逻辑层
                .Build();                     // 自动扫描 [OwnerLayer] 并装配
    }

    void Update()
    {
        // 2. 驱动主循环
        // 处理所有 Post 的异步事件、Delay 定时任务以及 LBTask 状态机
        // 如果脏队列为空，此调用的耗时仅为几个纳秒！
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

### 3. 拓扑结构可视化
开启 Debug 模式后，调用 `GetTopologySummary()` 即可在控制台输出一张清晰的文本结构图，展示整个系统内各个 Layer 挂载了哪些 Manager，以及它们具体订阅/派发了什么事件。这在大型项目中是排查系统耦合度不可或缺的工具。