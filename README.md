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

## 📖 最佳实践手册：构建秩序的堡垒

性能只是底座，工程的长期可维护性才是大型项目的生命线。LayerBase 强烈推荐使用 **`Layer -> Service -> Manager`** 的三层递进架构。

为什么要这么设计？在传统的 ECS 或纯 EventBus 架构中，系统往往是扁平的。当你的项目膨胀到百万行代码、成百上千个模块时，扁平架构会导致模块之间的依赖关系像一团乱麻，新人接手根本无从下手。

LayerBase 的三层约束，本质上是在用**物理空间的隔离**来对抗代码的熵增：

*   🌍 **Layer（宏观层级）**：**最顶层的时序与边界组织者。**
    *   **角色**：它不写具体的业务，而是代表了系统的处理优先级和物理界限（如：`RenderLayer`、`PhysicsLayer`、`CoreLogicLayer`、`InteractionLayer`）。
    *   **好处**：通过定义 Layer 的上下层关系，你可以极其明确地控制事件的流向（Bubble 向上，Drop 向下）。这就保证了无论底层业务怎么乱，整体系统运转的“绝对时序”是永远确定的。
*   🏢 **Service（业务服务）**：**中层的功能聚合器。**
    *   **角色**：它负责将同属一个大功能的细碎模块聚合在一起（例如：`PlayerService` 负责将 `PlayerInput`、`PlayerMove`、`PlayerAnimation` 圈在一起）。
    *   **好处**：Service 承担了依赖注入（DI）的装配工作。对外，它暴露粗粒度的接口；对内，它隐藏了 Manager 的复杂性。这实现了完美的“高内聚”。
*   ⚙️ **Manager（具体逻辑块）**：**底层的微观承载者。**
    *   **角色**：遵循**单一职责原则 (SRP)**，专注干好一件事（例如：仅处理角色受击）。
    *   **好处**：Manager 不直接引用任何其他 Manager，它们之间的通讯全部交由事件总线（EventBus）完成。这种设计实现了真正的“低耦合”，让你随时可以拔掉一个 Manager 而不导致编译报错。

通过这套架构，**你的代码目录结构，就是你的系统架构图。**

---

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

### Step 4: 全空间维度的事件触发 (Send / Post / Delay)
传统的框架通常只能通过一个全局单例（如 `EventBus.Publish`）来发消息，这会导致事件瞬间弥漫到整个系统，引发未知的蝴蝶效应。

在 LayerBase 中，**不仅是 `LayerHub`，你代码里的大多数对象（`Layer`、`Service`、`Manager`）都能直接感知自身在架构中的坐标，并向外界派发事件。** 这赋予了你极度精细的“火力覆盖”能力：

```csharp
// ⚔️ 【Send 族：同步执行，立刻阻塞当前执行流，适合强依赖时序的逻辑】
this.SendLocal(new DamageEvent());  // 【精准打击】仅在自己所在的同层级内广播
this.SendBubble(new DamageEvent()); // 【向上冒泡】抛给比自己更“上层”的系统（例如：底层逻辑把数据丢给 UI 渲染）
this.SendDrop(new DamageEvent());   // 【向下坠落】抛给比自己更“下层”的系统（例如：UI 接收输入后下发给物理引擎）
this.SendGlobal(new DamageEvent()); // 【全图穿透】无视界限，穿透所有层级

// 📨 【Post 族：异步投递，不阻塞代码，推入脏队列等待下一帧统一处理，适合非紧急状态同步】
this.PostBubble(new DamageEvent());
this.PostGlobal(new DamageEvent()); 

// ⏳ 【Delay 族：定时投递，内置的零分配计时器】
this.DelayDrop(new PlayerDeathEvent(), 3.5f); // 3.5秒后，自动向下层级派发死亡事件
```
**好处**：这套丰富的 API 让“事件”拥有了真正的物理方向（上下左右）。通过明确的传播路径（比如严禁逻辑层向上传递物理数据，只能用 Bubble），你能从根源上斩断那种“循环触发”的面条代码。

### Step 5: 游戏引擎生命周期整合 (Build & Pump)
LayerBase 是纯 C# 的，它不依赖任何特定的游戏引擎。你只需要在你的引擎（Unity 的 `MonoBehaviour`、Godot 的 `Node` 或纯 C# 的主循环）中，接入它的**构建（Build）**与**心跳（Pump）**。

*   **Build（构建拓扑）**：应放在引擎的最早启动期。此时框架会扫描特性、预分配 SOA 内存，并静态审计整个拓扑的死循环风险。
*   **Pump（驱动心跳）**：应放在引擎的帧更新（Update）中。它负责消费所有的 `Post` 异步事件、`Delay` 定时任务以及推动 `LBTask` 状态机。

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
        // 1. 启动期：构建架构大厦
        LayerHub.CreateLayers()
                .Push(new InteractionLayer()) // Index 0: 顶层交互层
                .Push(new CoreLogicLayer())   // Index 1: 核心逻辑层
                .Build();                     // 自动扫描程序集中的 [OwnerLayer] 并极速装配
    }

    void Update()
    {
        // 2. 运行时：驱动系统心跳
        // 放心，如果脏队列是空的，这一行代码的耗时只有区区几个纳秒（~8ns），
        // 绝对不会对你的帧率造成任何负担！
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