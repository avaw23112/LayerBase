# 🚀 LayerBase: 工业级高性能游戏架构总线

**LayerBase** 是一款专为高性能 C# 游戏开发（如 Unity、Godot、纯 C# 服务端）打造的“分层事件架构与通讯总线”框架。

它彻底打破了传统 OOP 事件总线（Event Bus）的性能瓶颈，采用激进的**面向数据（Data-Oriented Design, DOD）**和**SOA（Structure of Arrays）**底层设计。极简、高效、安全，能够为您的大型项目提供稳如泰山的事件流转和状态管理，其实测 TPS（每秒吞吐量）已达到惊人的**1.5亿次以上**。

## 🎯 我们解决了什么问题？

在大型复杂游戏（特别是高频动作、MMO 或大型单机）开发中，随着代码量的剧增，传统的事件机制往往会引发“到处起飞”的灾难。LayerBase 为此而生，致力于解决三大核心痛点：

1. **统一开发流程 (Unified Workflow)**
   - 强制约束的 `Layer -> Service -> Manager` 三层递进架构，搭配基于特性（Attribute）的自动依赖注入。彻底杜绝了“意大利面条式”的随地订阅与派发，让百人团队的协同开发拥有了统一、清晰且可溯源的范式。
2. **全套工业级基建 (Complete Infrastructure)**
   - LayerBase 并非仅仅是一个 `Dictionary<Type, Delegate>`。它是一整套完整的通讯生态：内建的零分配异步任务系统 (`LBTask`)、多线程并行队列 (`HandleParallel`)、死循环防卫静态审计、以及自动化拓扑可视化分析。为您省去了数十项底层框架轮子的开发成本。
3. **绝对的性能管控 (Performance Control)**
   - 将事件路由的开销从“不可控的运行时损耗”降维打击至“物理极限的寄存器与缓存交互”。让性能预算极其吃紧的 60FPS/120FPS 游戏，再也不用为底层的架构开销买单。

---

## ⚡ 标准基准测试 (BenchmarkDotNet)

我们使用权威的 `BenchmarkDotNet` 进行了严苛的基准压测。测试环境：`.NET 8.0`, `X64 RyuJIT`, `Intel Core i7-12650H`。

| 场景描述                                        | 任务量 (事件派发次数) | 平均耗时 (Mean) | 等效处理量 (次/秒) | GC 内存分配 (Allocated) |
|-------------------------------------------------|----------------------:|----------------:|----------------------:|------------------------:|
| **轻度负载 (模拟真实业务)** <br> 10 层架构，单层订阅   | **1,000,000** 次      | **6.38 ms**     | **~156,700,000**      | **0 B** (热路径无分配)  |
| **极限高压 (全链路轰炸)** <br> 10 层架构，层层都订阅 | **1,000,000** 次      | **16.81 ms**    | **~59,400,000**       | **0 B** (热路径无分配)  |
| **1ms 挑战 (常见3层架构)** <br> 3 层架构全订阅       | **10,000** 次         | **91.41 μs**    | **~109,300,000**      | **0 B** (热路径无分配)  |

> 💡 **数据解读**：在真实的 10 层业务架构下，发送一百万次事件仅需 6 毫秒，TPS 突破 1.5 亿。即使在每一层都强行挂载逻辑的极端高压环境下，依然维持了近 6000 万的超高吞吐。对于每帧只有寥寥数毫秒预算的游戏而言，LayerBase 的架构损耗已无限趋近于 0。

---

## 🏎️ 我们为什么快？ (核心底层黑科技大起底)

LayerBase 性能登峰造极的秘诀，在于我们极其贪婪地压榨了 CPU 的每一条指令和每一次缓存加载。为了突破性能天花板，我们实施了以下 5 项最具毁灭性的底层优化：

1. **硬件级位图跳跃 (Bitmask Skipping)**
   - 跨层级分发时，我们不遍历字典或链表。利用现代 CPU 硬件指令（如 `BitOperations.TrailingZeroCount`），**只需 1 个时钟周期的运算**，即可精准“瞬移”跨过所有空闲层级，彻底消灭跨层分发的跳跃损耗。
2. **纯粹的 SOA 数组布局 (Structure of Arrays)**
   - 传统框架的事件封装会导致严重的内存碎片。我们在构建时将同类事件的所有处理器“拆解脱水”，生成**高度连续的 `Delegate[]` 原生数组**。在热路径中，CPU L1/L2 缓存实现了最完美的顺序预取（Prefetching）。
3. **零分支执行引擎 (Branchless Execution)**
   - 彻底物理隔离同步（Sync）与异步（Async）处理器数组。核心循环内**不包含任何 `if(isAsync)` 的类型判断**；同时利用**位运算状态合并（Bitwise Aggregation）**技术压缩返回值检查，将 CPU 分支预测失败的代价降至最低。
4. **指针级越界消除 (Unsafe Offset)**
   - 在支持的运行时下，直接使用 `MemoryMarshal` 获取数组头部原生指针，结合 `Unsafe.Add` 进行指针偏移，**彻底拔除 JIT 编译器在循环内部的数组边界检查（BCE）指令**。
5. **两段式零分配重建 (Two-Pass Zero-Allocation)**
   - 当节点发生异常需要熔断并自愈时，系统采用两段式扫描预计算大小，直接在目标数组上完成装配。全程**无任何临时集合（如 `List<T>`）产生，确保 0 GC 内存分配**，拒绝一切性能抖动。

---

## 🤔 我们为什么选择 LayerBase？ (架构对比与目标用户)

作为一个技术架构师，要让一个框架在项目中站稳脚跟，必须清醒地认识到其在生态位中的坐标。

### 1. ⚔️ 横向对比：降维打击的统治力
在 C# 游戏开发（Unity/Godot）领域，事件通讯方案大致分为三代，LayerBase 的表现如下：
*   **对比第一/二代框架（原生 `Action` / `UniRx` / 泛型 `EventBus`）**：传统事件总线高度依赖 `Dictionary<Type, List<Delegate>>`，在分发时伴随大量封箱拆箱、虚函数调用。它们的极限 TPS 通常在 50万 到 200万 之间。LayerBase 通过 **SOA 连续数组 + 零分支引擎**，直接将 TPS 抬升到了 **1.5亿**。这是近乎 **50~100 倍的降维打击**。
*   **对比第三代框架（纯 ECS 架构如 `Arch` / `Entitas`）**：ECS 的核心优势在于基于 SOA 内存布局的极速 Query。LayerBase 作为非 ECS 框架，在“事件路由”这一特定任务上，通过底层数据重构，达到了**与顶级 C++ / C# ECS 框架完全同级别的指令吞吐率和 L1 缓存命中率**。

### 2. 🛡️ 能力护城河：秩序与自由的平衡
除了巅峰的性能，LayerBase 构建了极其稳固的工业级基建：
*   **物理空间隔离**：`Local/Global/Bubble/Drop` 四维传播模型，完美契合游戏的渲染树或逻辑层级。底层 UI 的点击不会莫名其妙地污染到顶层物理计算。
*   **工业级自愈熔断**：任何 Handler 抛出未捕获异常，系统会在千分之一毫秒内对其进行**精准定位并熔断**。故障绝不阻塞同层其他业务。在下一帧，引擎通过无锁的“脏标记延迟重建”平滑剔除失效节点，实现系统自愈。
*   **零开销异步生态 (`LBTask`)**：现代游戏开发离不开异步。LayerBase 自带的 `LBTask` 实现了“同步路径零分配”，配合源生成器，让开发者写异步逻辑就像写同步一样爽，且无惧 GC 尖峰。

### 3. ⚖️ 坦诚的局限性与门槛 (Trade-offs)
世界上没有银弹。为了达到 1.5 亿的 TPS，LayerBase 做出了以下妥协，这也是您在选型时需要知晓的：
1.  **事件强制为 `struct`**：为了防止 GC 和利用 SOA，事件在传递时是值拷贝的。这意味着如果您在事件中传递非常大的结构体，拷贝成本反而会抵消路由优势。
2.  **更高的心智负担**：新手习惯了随时随地 `EventBus.Subscribe()`。LayerBase 强制要求理解 `Layer -> Service -> Manager` 的注册时序和传播方向，这对于做一个简单的 Flappy Bird 来说显得“太重了”。
3.  **动态挂载开销**：极速分发依赖于扁平数组（SOA）。如果在游戏运行时**极其频繁**地动态添加/移除单个 Handler，会导致底层不断触发两段式重建。LayerBase 更适合**静态拓扑**（场景初始化时 Build 完毕，运行时狂奔）。

### 🎯 结论：谁适合使用 LayerBase？
**LayerBase 不是一个用来写“原型玩具”的轻量级轮子，它是一把为“3A 级性能要求或超大规模复杂逻辑”打造的重型狙击枪。**
如果您正在开发一款**包含成千上万个实体交互的 MMO、每帧需要处理海量碰撞与状态同步的高频动作游戏、或是追求极致响应吞吐量的帧同步网关**——此时，市面上大多数基于字典和虚函数的架构都会成为 CPU 瓶颈，而 **LayerBase** 将成为您唯一且最佳的选择。

---

## 📦 怎么安装？

1. **源码引入**：将仓库中的 `LayerBase` 和 `LayerBase.Task` 项目目录直接添加到您的解决方案中并建立引用。
2. **配置源生成器 (Source Generator)**：
   框架重度依赖源生成器以消除所有的反射开销，请务必确保配置了 `LayerBase.Generator` 分析器：
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
3. **环境要求**：完美支持 `.NET Standard 2.1`（对 Unity/Godot 极度友好），建议在 `.NET 8.0/9.0` 环境运行以完全解锁底层 `Unsafe` 加速特性。

---

## 📖 怎么用？ (最佳实践手册)

LayerBase 推荐使用 `Layer -> Service -> Manager` 的三层架构，结合特性的自动装配，让您的代码优雅而强大。

### Step 1: 定义您的事件 (Event Structs)
所有的事件必须声明为 `struct`：

```csharp
public struct DamageEvent
{
    public int TargetId;
    public float Amount;
}
public struct PlayerDeathEvent { }
```

### Step 2: 编写具体的业务逻辑 (Manager)
Manager 专注于单一业务逻辑。继承 `ILayerContext` 即可自动感知自己所属的层级，并解锁所有 `Send` 与 `Post` 派发能力。
推荐使用 `[Subscribe]` 和 `[SubscribeAsync]` 特性，让 Source Generator 自动帮您生成事件绑定代码，**零反射开销**。

```csharp
using LayerBase.DI;
using LayerBase.Core.Event;
using LayerBase.Async;

// 必须标记 partial，供源生成器植入绑定逻辑
public partial class DamageManager : ILayerContext
{
    // 【同步处理】：使用 [Subscribe] 特性
    [Subscribe]
    private EventHandledState OnTakeDamage(in DamageEvent e)
    {
        Console.WriteLine($"实体 {e.TargetId} 受到 {e.Amount} 伤害");
        
        if (e.Amount > 100)
        {
            // 向下坠落，传递给更底层的物理层/逻辑层
            this.SendDrop(new PlayerDeathEvent()); 
        }
        
        // Continue: 允许同层或后续层级的其他人继续监听该事件
        // Handled: 立即截断事件传播
        return EventHandledState.Continue;
    }

    // 【异步处理】：使用 [SubscribeAsync] 特性，完美融合分帧计时
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
通过 `[OwnerLayer]` 特性，您可以将一个服务**硬编码绑定**到特定的 Layer 上，极大地增强了项目的静态可溯源性。

```csharp
using LayerBase.DI;

// 声明该服务将永远被挂载到 GameLogicLayer 层级
[OwnerLayer(typeof(GameLogicLayer))]
public class CombatService : IService 
{
    // 配置依赖注入，将 Manager 注册到所属 Layer 中
    public void ConfigureServices(IServiceCollection services) 
    { 
        // 注意：在这里注册的顺序，即为该层级内事件响应的【优先级顺序】！
        services.AddSingleton<DamageManager, DamageManager>();
    }
}
```

### Step 4: 定义层级容器 (Layer) & 初始化拓扑 (LayerHub)
Layer 是最宏观的拦截容器，代表了系统的处理优先级。在游戏入口处初始化它们。

```csharp
using LayerBase.Layers;
using LayerBase.LayerHub;

// 定义层级
public class UILayer : Layer { }
public class GameLogicLayer : Layer { }

// 在游戏 Awake / _Ready 中构建拓扑
public void InitGame()
{
    LayerHub.CreateLayers()
            .Push(new UILayer())          // Index 0: 最顶层 (最先收到 Bubble, 最后收到 Drop)
            .Push(new GameLogicLayer())   // Index 1: 底层
            .Build();                     // Build() 会自动扫描所有标有 [OwnerLayer] 的服务并自动组装！
}
```

### Step 5: 触发事件与驱动循环 (Send/Post/Pump)

**事件派发：**
```csharp
// 【同步执行】全局广播，立即阻塞当前线程并穿透所有层级
LayerHub.Send(new DamageEvent { TargetId = 99, Amount = 50f });

// 【异步投递】将事件压入脏队列，不阻塞当前代码，等待帧泵 (Pump) 处理
LayerHub.Post(new DamageEvent { TargetId = 1, Amount = 10f });
```

**主循环驱动：**
如果您使用了 `Post` 投递、`LBTask` 或者延迟任务，请务必在主循环中调用 Pump。框架内置了“脏队列追踪”技术，在没有事件挂起时，此调用的耗时仅为**几个纳秒**。
```csharp
void Update(float deltaTime)
{
    LayerHub.Pump(deltaTime);
}
```

---

## 🛠 高级特性

### 1. 流式过滤与拦截 (Fluent API)
除了特性自动绑定，LayerBase 还为您提供了如 LINQ 般丝滑的链式订阅 API。您可以在 Service 或 Manager 中直接调用。
这对于动态条件拦截极其有效，能够在路由的**最早期（闭包内部）**过滤事件，拒绝无用逻辑被唤醒。

```csharp
public partial class PlayerManager : ILayerContext
{
    private int _myEntityId = 10;

    public void Initialize()
    {
        // 🌊 优雅的 Fluent API
        this.OnEvent<DamageEvent>()
            .Where((in DamageEvent e) => e.TargetId == _myEntityId) // 编译期注入条件，不符合即秒拒
            .Handle((in DamageEvent e) => 
            {
                // 处理受击逻辑...
                return EventHandledState.Handled;
            });
    }
}
```

### 2. 后台并行处理 (Parallel Handlers)
如果您有极度消耗 CPU 且**无需修改主线程对象状态**的纯计算逻辑（如复杂的数学寻路下发、日志落盘），可以使用并行订阅。事件会被推入无锁队列，交由 ThreadPool 在后台异步吞吐，绝不卡主帧：

```csharp
// 支持特性绑定
[SubscribeParallel]
private EventHandledState OnHeavyComputeTask(in ComputeEvent e)
{
    // 此逻辑将在多线程环境中调度执行
    return EventHandledState.Continue;
}

// 也支持流式绑定
this.OnEvent<ComputeEvent>().HandleParallel(...);
```

### 3. 拓扑审计与死循环防御 (Topology Audit)
事件系统最怕逻辑回环（例如 A 派发了 B，B 又同步派发了 A）。
LayerBase 绝不容忍“黑盒运行”。在 `Build()` 被调用的那一刻，系统底层会启动有向图着色算法（Three-Color Algorithm），**静态审计整个游戏的事件流向**。
- 如果发现**同步死循环风险**或**无订阅者的死信**，系统会在控制台给出清晰的环路路径并抛出异常。
- 开启 Debug 模式后，您可以随时调用 `GetTopologySummary()` 打印出结构清晰的文本拓扑图，让整个系统“谁派发了什么，谁监听了什么”一目了然。

---

*“在苛求性能的领域，一切不必要的抽象都是罪恶。LayerBase 为您扫清障碍。”*
